using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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
            if (max < 2)
            {
                throw new Exception("Max Concurrency Levels of Less Than 2 will Result in a Deadlock.");
            }
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

        private void HandleCoroutineAllProgress(bool status, ref int completedCount, ref int total, ref bool failureState)
        {
            if (status)
            {
                completedCount++;
            }
            else
            {
                failureState = true;
            }
            if (OnProgressUpdate != null)
            {
                if (total != 0)
                {
                    OnProgressUpdate((float)completedCount / (float)total);
                }
            }
        }

        public IEnumerator All(IEnumerator[] coroutines)
        {
            total = coroutines.Length;
            int completedCount = 0;
            bool failureState = false;
            foreach (IEnumerator coroutine in coroutines)
            {
                Run(coroutine, (status) =>
                {
                    HandleCoroutineAllProgress(status, ref completedCount, ref total, ref failureState);
                });
            }
            while (completedCount < total)
            {
                if (failureState)
                {
                    Debug.LogError("Unable to Execute All Coroutines");
                    yield break;
                }
                yield return null;
            }
        }

        public void Chain(params IEnumerator[] coroutines)
        {
            if (coroutines.Length > 0)
            {
                Run(coroutines[0], (status) => { if (status) Chain(coroutines.Skip(1).ToArray());});
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
                    numActive--;
                    yield break;
                }
                yield return current;
            }
            wrapper.ExecCallback(true);
            numActive--;
            if (queue.Count > 0)
            {
                CoroutineCallbackWrapper next = queue.Dequeue();
                RunInternal(next);
            }
        }
    }
}

