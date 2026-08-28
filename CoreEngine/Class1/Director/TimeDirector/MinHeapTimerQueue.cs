using System.Collections.Generic;

namespace CoreEngine.TimeSystem
{
    internal class MinHeapTimerQueue
    {
        private readonly List<TimerTask> _heap = new List<TimerTask>(128);

        public int Count => _heap.Count;

        public void Push(TimerTask task)
        {
            _heap.Add(task);
            SiftUp(_heap.Count - 1);
        }

        public TimerTask Peek()
        {
            return _heap.Count > 0 ? _heap[0] : null;
        }

        public TimerTask Pop()
        {
            if (_heap.Count == 0) return null;

            TimerTask root = _heap[0];
            int lastIndex = _heap.Count - 1;
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);

            if (_heap.Count > 0)
            {
                SiftDown(0);
            }

            return root;
        }

        public void Clear()
        {
            _heap.Clear();
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_heap[index].CompareTo(_heap[parent]) >= 0) break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            int count = _heap.Count;
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < count && _heap[left].CompareTo(_heap[smallest]) < 0)
                    smallest = left;
                if (right < count && _heap[right].CompareTo(_heap[smallest]) < 0)
                    smallest = right;

                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            TimerTask temp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = temp;
        }
    }
}