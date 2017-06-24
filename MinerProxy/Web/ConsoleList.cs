using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy.Web
{
    class ConsoleList
    {

        public ConsoleColor color { get; set; }
        public string message { get; set; }

        public ConsoleList(string text, ConsoleColor color)

        {
            this.message = text;
            this.color = color;
        }

    }

    class SlidingBuffer<T> : IEnumerable<T>
    {
        private readonly Queue<T> _queue;
        private readonly int _maxCount;

        public SlidingBuffer(int maxCount)
        {
            _maxCount = maxCount;
            _queue = new Queue<T>(maxCount);
        }

        public void Add(T item)
        {
            if (_queue.Count == _maxCount)
                _queue.Dequeue();
            _queue.Enqueue(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
