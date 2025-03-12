using System;

namespace Chetch.Utilities;

public static class ConsoleHelper
{
    static public void PK(String text, params String[] args)
    {
        System.Console.WriteLine(text, args);
        System.Console.ReadKey(true);
    }

    static public void CLR(String text, params String[] args)
    {
        System.Console.Clear();
        System.Console.Write(text, args);
    }

    static public void LF(int n = 1)
    {
        for(int i = 0; i < n; i++)
        {
            Console.WriteLine("");
        }
    }
}
