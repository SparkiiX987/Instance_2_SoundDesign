using System.Collections.Generic;

namespace Utils.Extensions
{
    public class LimitedQueue<T>
    {
        private readonly Queue<T> queue = new();
        private readonly int maxSize;

        public LimitedQueue(int _maxSize)
        {
            this.maxSize = _maxSize;
        }

        public void Enqueue(T _item)
        {
            if (queue.Count >= maxSize)
            {
                queue.Dequeue();
            }
            queue.Enqueue(_item);
        }

        public T[] ToArray() => queue.ToArray();
    }
}