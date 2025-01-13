using System;

namespace Chetch.Utilities;

public static class CheckSum
{
    public static byte SimpleAddition(byte[] data)
    {
        byte sum = 0;
        unchecked // Let overflow occur without exceptions
        {
            foreach (byte b in data)
            {
                sum += b;
            }
        }
        return sum;
    }
}