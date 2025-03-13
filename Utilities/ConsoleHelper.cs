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

    static public void CLRL(int row = -1, int col = 0)
    {
        if(row < 0)row = Console.CursorTop;
        Console.SetCursorPosition(col, row);
        Console.Write(new string(' ', Console.WindowWidth - col)); 
        Console.SetCursorPosition(col, row);
    }

    static public void LF(int n = 1)
    {
        for(int i = 0; i < n; i++)
        {
            Console.WriteLine("");
        }
    }
}
