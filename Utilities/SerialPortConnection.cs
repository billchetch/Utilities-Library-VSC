using System;
using System.Management;
using System.IO.Ports;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Data.SqlTypes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace Chetch.Utilities;

public abstract class SerialPortConnection
{
    #region Constants
    public const int REOPEN_TIMER_INTERVAL = 2000;
    #endregion

    #region Static stuff
    static public bool PortExists(String portName)
    {
        var portNames = SerialPort.GetPortNames();
        foreach(var pn in portNames){
            if(pn.Equals(portName)){
                return true;
            }
        }
        return false;
    }

    static public String[] GetUSBDevices(String pathSpec)
    {
        var dirName = Path.GetDirectoryName(pathSpec);
        var fName = Path.GetFileName(pathSpec);
        var files = Directory.GetFiles(dirName, fName);
        return files;
    } 

    static public USBDeviceInfo GetUSBDeviceInfo(String portName)
    {
        if(OperatingSystem.IsMacOS())
        {
            var xml = CMD.Exec("system_profiler", "-xml SPUSBDataType");
            //parse the output which is expected to be XML
            var xe = XElement.Parse(xml);
            
            //Now search through the XML to find the right reg items
            var devices = xe.Descendants("key")
                .Where(x => x.Value == "location_id")
                .Select(x => (XElement)x.Parent).ToList();

            foreach(var dev in devices)
            {
                var devInfo = dev.Elements().Where(x => x.Name == "key").Select(x => x).ToDictionary(x => x.Value, x => ((XElement)x.NextNode).Value);
                var locationSuffix = devInfo["location_id"].Replace("0", "").Replace("x", "").Replace(" / ", "").Substring(0, 2) + "0";
                if(portName.Contains(locationSuffix))
                {
                    return new USBDeviceInfo(portName, devInfo["product_id"], devInfo["vendor_id"]);
                }
            }
            
            throw new Exception(String.Format("Could not find device info for usb serial device @ {0}", portName));
        }
        else if(OperatingSystem.IsLinux())
        {
            var output = CMD.Exec("udevadm", "info -q property --property=DEVNAME,ID_USB_MODEL_ID,ID_USB_VENDOR_ID --no-pager --value " + portName, Environment.NewLine);
            
            if(String.IsNullOrEmpty(output))
            {
                throw new Exception(String.Format("Could not find device info for usb serial device @ {0}", portName));
            }
            var lines = output.Split(Environment.NewLine);
            if(lines.Length < 3)
            {
                throw new Exception(String.Format("Could not retrieve sufficient info for usb serial device @ {0}", portName));
            }
            
            return new USBDeviceInfo(lines[0], lines[1], lines[2]);
        }
        else
        {
            throw new Exception(String.Format("Unrecognised platform: {0} Version: {1}", Environment.OSVersion.Platform, Environment.OSVersion.Version));
        }
    }

    #endregion

    #region Enums and Classes
    public class USBDeviceInfo
    {
        public String PortName;
        public int ProductID;
        public int VendorID;

        public USBDeviceInfo(String portName, int productID, int vendorID)
        {
            PortName = portName;
            ProductID = productID;
            VendorID = vendorID;
        }

        public USBDeviceInfo(String portName, String productID, string vendorID) : 
                this(portName, System.Convert.ToInt32(productID, 16), System.Convert.ToInt32(vendorID, 16))
        {}
        public bool IsValidProduct(int productID, int vendorID)
        {
            return productID == ProductID && vendorID == VendorID;
        }
    }
    #endregion

    #region Fields
    int baudRate;
    Parity parity;
    int dataBits;
    StopBits stopBits;

    SerialPort serialPort;
    
    Exception lastError;

    System.Timers.Timer connectTimer = new System.Timers.Timer();

    bool connected; //we save state for triggering events
    #endregion

    #region Properties
    public String PortName { get; internal set; } = String.Empty;
    
    public bool IsConnected => serialPort != null && serialPort.IsOpen && SerialPortConnection.PortExists(PortName);
    #endregion

    #region Events
    public event EventHandler<byte[]> DataReceived;

    public event EventHandler<bool> Connected;
    #endregion

    #region Constructors
    public SerialPortConnection(int baudRate, Parity parity, int dataBits, StopBits stopBits)
    {
        this.baudRate = baudRate;
        this.parity = parity;
        this.dataBits = dataBits;
        this.stopBits = stopBits;

        connectTimer.AutoReset = false;
        connectTimer.Interval = REOPEN_TIMER_INTERVAL;
        connectTimer.Elapsed += (sender, eargs) => {
                connectTimer.Stop();

                try
                {
                    if(!IsConnected)
                    {
                        if(connected){
                            connected = false;
                            Connected?.Invoke(this, connected);
                        }
                        serialPort?.Dispose();
                        serialPort = null;
                        Connect();
                    }
                }
                catch (Exception e)
                {
                    lastError = e;
                }
                connectTimer.Start();
            };
    }
    #endregion

    #region Methods
    abstract protected String GetPortName();

    virtual protected void OnDataReceived(byte[] data)
    {
        DataReceived?.Invoke(this, data);
    }
    
    public void Connect()
    {
        connectTimer.Stop();
        
        try{
            //Serial port creation
            if(serialPort == null)
            {
                PortName = GetPortName();
                if(PortExists(PortName))
                {   
                    serialPort = new SerialPort(PortName, baudRate, parity, dataBits, stopBits);
                    serialPort.DataReceived += (ArrayShapeEncoder, eargs) => {
                            int dataLength = serialPort.BytesToRead;
                            byte[] data = new byte[dataLength];
                            int nbrDataRead = serialPort.Read(data, 0, dataLength);

                            if (nbrDataRead == 0)
                                return;
                    
                            OnDataReceived(data);
                        };    
                }
            }
            
            //Serial port opening
            if(serialPort != null && !serialPort.IsOpen)
            {
                serialPort.Open();
                connected = true;
                Connected.Invoke(this, connected);
            }
        }
        catch(Exception e)
        {
            throw new Exception(String.Format("Failed to connect: {0}", e.Message));
        }
        finally
        {
            //restart the ol timer
            connectTimer.Start();
        }
    }

    public void SendData(byte[] data)
    {
        if(data.Length > 0)
        {
            serialPort.Write(data, 0, data.Length);
        }
    }
    
    public void Disconnect()
    {
        if(connected){
            connected = false;
            Connected?.Invoke(this, connected);
        }
        if(serialPort != null)
        {
            if(serialPort.IsOpen)
                serialPort.Close();

            serialPort.Dispose();
            serialPort = null;
        }
        connectTimer.Stop();
    }
    #endregion
}
