using System;
using System.CodeDom;

namespace Chetch.Utilities;

public class CircularLog<T> : RingBuffer<T>
{
    public ulong WritesCount { get; internal set; } = 0;

    public CircularLog(int capacity, bool reverse = true) : base(capacity, reverse)
    {}

    override public T Remove()
    {
        throw new NotImplementedException("Cannot remove items from a circular log");
    }

    public override void Add(T item)
    {
        base.Add(item);
        WritesCount++;
    }

    public override void Clear()
    {
        base.Clear();
        WritesCount = 0;
    }
}
