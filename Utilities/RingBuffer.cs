using System;
using System.Collections;

namespace Chetch.Utilities;

public class RingBuffer<T> : IEnumerable<T>
{
    private readonly T[] buffer;
    private int head; // Index of the oldest element
    private int tail; // Index where the next element will be added
    private int count; // Current number of elements in the buffer

    public int Capacity { get; }
    public int Count => count;

    public bool Reverse { get; set; } = false;

    public bool IsFull => count == Capacity;

    public bool IsEmpty => count == 0;

    public RingBuffer(int capacity, bool reverse = false)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }
        Capacity = capacity;
        buffer = new T[capacity];
        head = 0;
        tail = 0;
        count = 0;
        Reverse = reverse;
    }

    // Adds an item to the buffer. If full, overwrites the oldest element.
    public void Add(T item)
    {
        buffer[tail] = item;
        tail = (tail + 1) % Capacity;

        if (count == Capacity)
        {
            head = (head + 1) % Capacity; // Oldest element was overwritten, so advance head
        }
        else
        {
            count++;
        }
    }

    // Retrieves and removes the oldest item from the buffer.
    public T Remove()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        T item = buffer[head];
        buffer[head] = default(T); // Clear the reference (optional, for garbage collection)
        head = (head + 1) % Capacity;
        count--;
        return item;
    }

    // Peeks at the oldest item without removing it.
    public T Peek()
    {
        if (count == 0)
        {
            throw new InvalidOperationException("Buffer is empty.");
        }
        return buffer[head];
    }

    // Clears all elements from the buffer.
    public void Clear()
    {
        Array.Clear(buffer, 0, Capacity);
        head = 0;
        tail = 0;
        count = 0;
    }

    // Implements IEnumerable for easy iteration.
    public IEnumerator<T> GetEnumerator()
    {
        if (count == 0)
        {
            yield break;
        }

        if (Reverse)
        {
            int current = tail == 0 ? (Capacity - 1) : tail - 1;
            for (int i = 0; i < count; i++)
            {
                yield return buffer[current];
                current = current == 0 ? (Capacity - 1) : current - 1;
            }
        }
        else
        {
            int current = head;
            for (int i = 0; i < count; i++)
            {
                yield return buffer[current];
                current = (current + 1) % Capacity;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
