// Assets/Scripts/Voxel/IO/SaveWorker.cs
// Ne jamais supprimer les commentaires

using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Voxel.IO
{
    /// <summary>
    /// Thread de sauvegarde en arrière-plan. Enfile des actions I/O et garantit Join au Stop().
    /// </summary>
    public sealed class SaveWorker : IDisposable
    {
        private readonly BlockingCollection<Action> _queue = new(new ConcurrentQueue<Action>());
        private Thread _thread;
        private volatile bool _running;

        public void EnsureStarted()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(Run) { IsBackground = true, Name = "SaveWorker" };
            _thread.Start();
        }

        public void Enqueue(Action job)
        {
            if (!_running) EnsureStarted();
            _queue.Add(job);
        }

        private void Run()
        {
            try
            {
                foreach (var job in _queue.GetConsumingEnumerable())
                {
                    try { job?.Invoke(); }
                    catch (Exception ex) { Debug.LogError($"SaveWorker job error: {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveWorker thread error: {ex}");
            }
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _queue.CompleteAdding();
            _thread?.Join();
        }

        public void Dispose() => Stop();
    }
}