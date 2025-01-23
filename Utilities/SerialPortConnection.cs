using System;
using System.Management;
using System.IO.Ports;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Data.SqlTypes;

namespace Chetch.Utilities;

public abstract class SerialPortConnection
{
    #region Constants
    public const int REOPEN_TIMER_INTERVAL = 2000;
    #endregion

    #region Static stuff
    static public String GetPortNameForDevice(String searchFor, String searchKey)
    {
        String portName = String.Empty;

         if(OperatingSystem.IsMacOS())
        {
            String output = CMD.Exec("ioreg",  "-a -r -c IOUSBHostDevice -l");
            
            //parse the output which is expected to be XML
            var xe = XElement.Parse(output);
            
            //Now search through the XML to find the right reg items
            var vals = xe.Descendants("key")
                .Where(x => x.Value == searchKey && ((XElement)x.NextNode).Value.Contains(searchFor))
                .Select(x => x.Parent);
            
            //Search through reg items to find required value
            String returnKey = "IOCalloutDevice";
            var results = new List<String>();
            foreach(var elt in vals)
            {
                var found = elt.Descendants("key").Where(x => x.Value == returnKey).Select(x => ((XElement)x.NextNode).Value);
                if(found.Any())
                {
                    foreach(var pn in found)
                    {
                        if(!results.Contains(pn))
                        {
                            results.Add(pn);
                        }
                    }
                }
            }
            if(results.Count == 1)
            {
                portName = results[0];
            }
            else if(results.Count == 0)
            {
                throw new Exception(String.Format("Cannot find {0} ... please check serial device is attached", searchFor));
            }
            else
            {
                throw new Exception(String.Format("Found {0} results, should only be one!", results.Count));
            }
        }
        else if(OperatingSystem.IsLinux())
        {
            String devDirectoryPath = "/dev/serial/by-id/";
            if(Directory.Exists(devDirectoryPath))
            {
                var files = Directory.GetFiles(devDirectoryPath);
                foreach(var fname in files)
                {
                    if(fname.Contains(searchFor))
                    {
                        FileInfo fi = new FileInfo(fname);
                        if(!String.IsNullOrEmpty(fi.LinkTarget))
                        {
                            portName = Path.GetFullPath(fi.LinkTarget, fi.Directory.FullName);
                        } 
                        else 
                        {
                            portName = fname;
                        }
                        break;
                    }
                }
                if(String.IsNullOrEmpty(portName))
                {
                    throw new Exception(String.Format("Cannot find {0} in {1} .. check serial defvice is attached", searchFor, devDirectoryPath));
                }
            } 
            else 
            {
                throw new Exception(String.Format("Cannot find directory {0} ... check serial device is attached", devDirectoryPath));
            }
        }
        else
        {
            throw new Exception(String.Format("Unrecognised platform: {0} Version: {1}", Environment.OSVersion.Platform, Environment.OSVersion.Version));
        }

        return portName;
    }

    static public String GetPortNameForDevice(int searchFor, String searchKey)
    {
        return GetPortNameForDevice(searchFor.ToString(), searchKey);
    }

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
    #endregion

    #region Enums and Classes
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
                catch 
                {
                    //what to do here??
                }
                connectTimer.Start();
            };
    }
    #endregion

    #region Methods
    abstract protected String getPortName();

    virtual protected void OnDataReceived(byte[] data)
    {
        DataReceived?.Invoke(this, data);
    }

    public void Connect()
    {
        connectTimer.Stop();
        
        //Serial port creation
        if(serialPort == null)
        {
            try{
                PortName = getPortName();
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
            catch (Exception e)
            {
                lastError = e;
            }
        }
        
        //Serial port opening
        if(serialPort != null && !serialPort.IsOpen)
        {
            try
            {
                serialPort.Open();
                connected = true;
                Connected?.Invoke(this, connected);
            }
            catch (Exception e)
            {
                lastError = e;
                //possible loggin or something here
            }
        }

        //restart the ol timer
        connectTimer.Start();
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
