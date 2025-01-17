using System;
using System.Management;
using System.IO.Ports;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace Chetch.Utilities;

public class SerialPortDevice
{
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
                        portName = fname;
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
}
