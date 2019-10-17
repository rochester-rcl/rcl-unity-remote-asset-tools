using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RemoteAssetBundleTools
{
    public class CoroutineQueue
    {
        public delegate Coroutine HandleStartCoroutine(IEnumerator coroutine);
        public delegate void HandleProgressUpdate(float progress);
        public delegate void HandleCoroutineError(string message);
        public event HandleProgressUpdate OnProgressUpdate;
        public event HandleCoroutineError OnCoroutineError;
        private readonly uint maxActive;
        private readonly Queue<CoroutineCallbackWrapper> queue;
        private HandleStartCoroutine coroutineStarter;
        private uint numActive;
        private int total;
        private int completedCount;
        private float progress;

        private class CoroutineCallbackWrapper
        {
            internal IEnumerator Coroutine { get; set; }
            internal Action<bool> Callback { get; set; }
            internal CoroutineCallbackWrapper(IEnumerator coroutine, Action<bool> callback)
            {
                Coroutine = coroutine;
                Callback = callback;
            }

            internal void ExecCallback(bool status)
            {
                if (Callback != null)
                {
                    Callback(status);
                }
            }
        }

        public CoroutineQueue(uint max, HandleStartCoroutine starter)
        {
            maxActive = max;
            coroutineStarter = starter;
            queue = new Queue<CoroutineCallbackWrapper>();
        }

        public void Run(IEnumerator coroutine, Action<bool> callback = null)
        {
            CoroutineCallbackWrapper wrapper = new CoroutineCallbackWrapper(coroutine, callback);
            RunInternal(wrapper);
        }

        private void RunInternal(CoroutineCallbackWrapper wrapper)
        {
            if (numActive < maxActive)
            {
                IEnumerator runner = CoroutineRunner(wrapper);
                coroutineStarter(runner);
            }
            else
            {
                queue.Enqueue(wrapper);
            }
        }

        public IEnumerator All(IEnumerator[] coroutines)
        {
            total = coroutines.Length;
            completedCount = 0;
            foreach (IEnumerator coroutine in coroutines)
            {
                Run(coroutine);
            }
            while (completedCount < total)
            {
                yield return null;
            }
            total = 0;
            completedCount = 0;
        }

        public void Chain(params IEnumerator[] coroutines)
        {
            for (int i = 0; i < coroutines.Length; i++)
            {
                IEnumerator current = coroutines[i];
                IEnumerator next = (i < coroutines.Length - 1) ? coroutines[i + 1] : null;
                if (next != null)
                {
                    Run(current, (status) => { if (status) Run(next); });
                }
            }
        }

        private IEnumerator CoroutineRunner(CoroutineCallbackWrapper wrapper)
        {
            numActive++;
            IEnumerator coroutine = wrapper.Coroutine;
            while (true)
            {
                object current;
                try
                {
                    if (coroutine.MoveNext() == false)
                    {
                        break;
                    }
                    current = coroutine.Current;
                }
                catch (System.Exception ex)
                {
                    if (OnCoroutineError != null)
                    {
                        OnCoroutineError(ex.Message);
                    }
                    wrapper.ExecCallback(false);
                    yield break;
                }
                yield return current;
            }
            wrapper.ExecCallback(true);
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
                CoroutineCallbackWrapper next = queue.Dequeue();
                RunInternal(next);
            }
        }
    }
}

