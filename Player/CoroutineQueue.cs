using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteAssetBundleTools
{
    public class CoroutineQueue
    {
        public delegate Coroutine HandleStartCoroutine(IEnumerator coroutine);
        public delegate void HandleProgressUpdate(float progress);
        public event HandleProgressUpdate OnProgressUpdate;
        private readonly uint maxActive;
        private readonly Queue<IEnumerator> queue;
        private HandleStartCoroutine coroutineStarter;
        private uint numActive;
        private int total;
        private int completedCount;
        private float progress;

        public CoroutineQueue(uint max, HandleStartCoroutine starter)
        {
            maxActive = max;
            coroutineStarter = starter;
            queue = new Queue<IEnumerator>();
        }

        public void Run(IEnumerator coroutine)
        {
            if (numActive < maxActive)
            {
                IEnumerator runner = CoroutineRunner(coroutine);
                coroutineStarter(runner);
            }
            else
            {
                queue.Enqueue(coroutine);
            }
        }

        public IEnumerator All(IEnumerator[] coroutines)
        {
            total = coroutines.Length;
            completedCount = 0;
            for (int i = 0; i < total; i++)
            {
                Run(coroutines[i]);
            }

            while (completedCount < total)
            {
                yield return null;
            }
            total = 0;
            completedCount = 0;
        }

        private IEnumerator CoroutineRunner(IEnumerator coroutine)
        {
            numActive++;
            yield return coroutineStarter(coroutine);
            completedCount++;
            if (OnProgressUpdate != null)
            {
                if (total != 0)
                {
                    OnProgressUpdate((float)completedCount / (float)total);
                }
            }
            numActive--;
            if (queue.Count > 0)
            {
                IEnumerator next = queue.Dequeue();
                Run(next);
            }
        }
    }
}

