using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopDetector
{
    public sealed class FloatRingBuffer
    {
        private readonly float[] buffer;
        private int writePos;
        public int Count { get; private set; }

        public FloatRingBuffer(int capacity)
        {
            buffer = new float[capacity];
        }

        public void Push(float value)
        {
            buffer[writePos] = value;
            writePos = (writePos + 1) % buffer.Length;
            Count = Math.Min(Count + 1, buffer.Length);
        }

        public float this[int indexFromEnd]
        {
            get
            {
                int idx = writePos - 1 - indexFromEnd;
                if (idx < 0) idx += buffer.Length;
                return buffer[idx];
            }
        }
    }

}
