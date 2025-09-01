// Assets/Scripts/Voxel/IO/StreamWorker.cs
// Ne jamais supprimer les commentaires

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Voxel.IO
{
    /// <summary>
    /// Worker générique pour charger/générer des sections en BG. Envoie des résultats via out-queue.
    /// </summary>
    public sealed class StreamWorker : IDisposable
    {
        public struct Job { public int sx, sy, sz; public string path; }
        public struct Result { public int sx, sy, sz; public ushort[] ids; public byte[] states; public bool fromDisk; }

        private readonly BlockingCollection<Job> _in = new(new ConcurrentQueue<Job>());
        private readonly ConcurrentQueue<Result> _out = new();
        private Thread _thread;
        private volatile bool _running;
        private readonly Func<Job, Result> _handler;

        public StreamWorker(Func<Job, Result> handler) { _handler = handler; }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(Run) { IsBackground = true, Name = "StreamWorker" };
            _thread.Start();
        }

        public void Enqueue(Job j)
        {
            if (!_running) Start();
            _in.Add(j);
        }

        public bool TryDequeueResult(out Result r) => _out.TryDequeue(out r);

        private void Run()
        {
            foreach (var j in _in.GetConsumingEnumerable())
            {
                var r = _handler != null ? _handler(j) : default;
                _out.Enqueue(r);
            }
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _in.CompleteAdding();
            _thread?.Join();
        }

        public void Dispose() => Stop();
    }
}