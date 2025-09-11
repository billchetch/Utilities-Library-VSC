using System;
using Chetch.Utilities;

namespace Utilities.Tests;

[TestClass]
public sealed class GeneralTests
{
    [TestMethod]
    public void Scrapbook()
    {
        int k = 0;
        int c = (k - 1) % 100;
        Console.WriteLine("C={0}",c);
    }
    
    [TestMethod]
    public void RingBufferTest()
    {
        int capacity = 100;

        RingBuffer<int> buffer = new RingBuffer<int>(capacity, true);

        for (int i = 0; i < buffer.Capacity / 2; i++)
        {
            buffer.Add(i);
        }

        foreach (var n in buffer)
        {
            Console.WriteLine("n = {0}", n);
        }
    }
}
