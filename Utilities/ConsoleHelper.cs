using System;

namespace Chetch.Utilities;

public static class ConsoleHelper
{
    static public void PK(String text)
    {
        System.Console.WriteLine(text);
        System.Console.ReadKey(true);
    }
}
