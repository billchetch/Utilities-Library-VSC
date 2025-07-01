
using System.IO.Ports;
using Chetch.Utilities;

namespace Chetch.Utilities;


/***
INSTRUCTIOS FOR USE (OS dependent)

LINUX (29/06/2025)
Process I used to setup Raspberry Pi 5:
1. Ensure that the remote device is paired and trusted:

> bluetoothctl
pair REMOTE MAC ADDRESSS
trust REMOTE MAC ADDRESSS

2. Add Serial Port capability to the bluetooth service by addding the following to /etc/systemd/system/dbus-org.bluez.service

ExecStart=/usr/libexec/bluetooth/bluetoothd -C
ExecStartPost=/usr/bin/sdptool add SP

3. Do the usual things to restart the service (daemon-reload etc.) or just reboot
4. Do the necesssary steps to ensure that a device node /dev/rfcomm0 appears when a remote bluetooth device connects
5. use "sudo rfcomm watch /dev/rfcomm0" or "sudo rfcomm watch hci0" to watch for connections (alternatively automate this with a service file e.g. rfcomm.service and start watching with that)
6. Now /dev/rfcomm0 can be used as your device path value for constructing a BluetoothSerialConnecton object.


MAC OS

WINDOWS
Todo

***/

public class BluetoothSerialConnection : SerialPortConnection
{
    #region Constants
    #endregion

    #region Fields
    String devicePath; //dev path or description (Windows)
    #endregion

    #region Constructors
    public BluetoothSerialConnection(String devicePath, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        : base(baudRate, parity, dataBits, stopBits)
    {

        if (OperatingSystem.IsLinux())
        {
            //check here if the rfcomm process is running to watch what we are doing
            var result = CMD.Exec("bash", "-c \"ps aux | grep rfcomm\"", Environment.NewLine);
            if (String.IsNullOrEmpty(result))
            {
                throw new Exception("Cannot use BluetoothSerial as no connections will be detected since rfcomm watch is not running");
            }
            String[] processes = result.Split(Environment.NewLine);
            String[] searchFor = new string[] { "rfcomm watch " + devicePath, "rfcomm watch hci0" };
            bool procFound = false;
            foreach (var proc in processes)
            {
                foreach (var name in searchFor)
                {
                    if (proc.Contains(name))
                    {
                        procFound = true;
                        break;
                    }
                }
            }
            if (!procFound)
            {
                throw new Exception("Cannot use BluetoothSerial as no connections will be detected since rfcomm watch is not running");
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            //Device path must exists in Mac OS and is not dependent on things being watched on rot
            if (!File.Exists(devicePath))
            {
                throw new Exception(String.Format("Cannot find device path {0}", devicePath));
            }
        }

        this.devicePath = devicePath;
    }
    #endregion

    #region Methods
    protected override string GetPortName()
    {
        if (OperatingSystem.IsWindows())
        {
            var devices = SerialPortConnection.GetUSBDevices(devicePath);

            foreach (var f in devices)
            {
                SerialPortConnection.USBDeviceInfo di = SerialPortConnection.GetUSBDeviceInfo(f);
            }
            throw new Exception(String.Format("Cannot find device based on search term {0}", devicePath));
        }
        else if (OperatingSystem.IsLinux())
        {
            return devicePath;
        }
        else if (OperatingSystem.IsMacOS())
        {
            return devicePath;
        }
        else
        {
            throw new NotSupportedException(String.Format("Operating system is not supported"));
        }
    }

    protected override bool PortExists()
    {
        if (OperatingSystem.IsLinux())
        {
            if (!File.Exists(PortName))
            {
                return false;
            }
            else
            {
                String result = CMD.Exec("rfcomm", "-a");
                if (String.IsNullOrEmpty(result))
                {
                    return false;
                }
                else
                {
                    String connected = "connected";
                    return result.Contains(connected);
                }
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            return File.Exists(devicePath);
        }
        else
        {
            return base.PortExists();
        }
    }
    #endregion
}

