using System;

namespace Chetch.Utilities;

public static class CheckSum
{
    public static ulong SimpleAddition(byte[] data)
    {
        ulong sum = 0;
        unchecked // Let overflow occur without exceptions
        {
            foreach (byte b in data)
            {
                sum += b;
            }
        }
        return sum;
    }

    public static byte[] SimpleAddition(byte[] data, int checksumSize)
    {
        ulong sum = 0;
        unchecked // Let overflow occur without exceptions
        {
            foreach (byte b in data)
            {
                sum += b;
            }
        }

        return Chetch.Utilities.Convert.ToBytes(sum, checksumSize);
    }
}