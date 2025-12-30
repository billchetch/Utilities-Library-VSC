using System;
using System.CodeDom;

namespace Chetch.Utilities;

public class CircularLog<T> : RingBuffer<T>
{
    public ulong EntriesCount { get; internal set; } = 0;

    public CircularLog(int capacity, bool reverse = true) : base(capacity, reverse)
    {}

    override public T Remove()
    {
        throw new NotImplementedException("Cannot remove items from a circular log");
    }

    public override void Add(T item)
    {
        base.Add(item);
        EntriesCount++;
    }

    public override void Clear()
    {
        base.Clear();
        EntriesCount = 0;
    }
}
