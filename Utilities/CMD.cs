using System;
using System.Diagnostics;

namespace Chetch.Utilities;

public static class CMD
{
    public static String Exec(String command, String args = null, String appendToDataReceived = null){
        
        Process proc = new Process();
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.FileName = command;
        String result = String.Empty;
        
        proc.OutputDataReceived += (sender, eargs) =>{
            result += eargs.Data;
            if(appendToDataReceived != null && !String.IsNullOrEmpty(eargs.Data))
            {
                result += appendToDataReceived;
            }
        };
        proc.Start();
        proc.BeginOutputReadLine();
        
        proc.WaitForExit();

        if(proc.HasExited)
        {
             return result;   
        }
        else
        {
            throw new Exception("Process has not exited");
        }
    }
}
