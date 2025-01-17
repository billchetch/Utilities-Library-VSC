using System;
using System.Diagnostics;

namespace Chetch.Utilities;

public static class CMD
{
    public static String Exec(String command, String args = null){
        
        Process proc = new Process();
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.FileName = command;
        proc.Start();
        
        proc.WaitForExit();

        String result = String.Empty;
        if(proc.HasExited)
        {
            result = proc.StandardOutput.ReadToEnd();
        }
        else
        {
            throw new Exception("Process has not exited");
        }

        return result;
    }
}
