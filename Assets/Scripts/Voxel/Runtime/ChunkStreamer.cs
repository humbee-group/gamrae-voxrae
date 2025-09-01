// Assets/Scripts/Voxel/Runtime/ChunkStreamer.cs
// Ne jamais supprimer les commentaires

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.World;
using Voxel.IO;
using Voxel.Runtime.Streaming;
using Voxel.Domain.Registry;

namespace Voxel.Runtime
{
    [RequireComponent(typeof(WorldRuntime))]
    [RequireComponent(typeof(SectionRenderRegistry))]
    public sealed class ChunkStreamer : MonoBehaviour
    {
        [Header("Streaming")]
        public Transform center;
        [Range(1,32)] public int viewRadius = 8;
        public int buildBudgetPerFrame = 16;

        [Header("Despawn (GC)")]
        [Tooltip("Marge autour du viewRadius avant de despawn (en sections).")]
        public int despawnMargin = 2;
        [Tooltip("Limite de despawn par frame.")]
        public int maxDespawnPerFrame = 24;

        [Header("Terrain")]
        public int seed = 1337;
        public float noiseScale = 0.0125f;
        public int baseHeight = 8;
        public int amplitude = 24;

        private WorldRuntime world;
        private SectionRenderRegistry renderReg;
        private StreamWorker worker;
        private ISectionSource generator;

        private readonly HashSet<SectionPos> wanted   = new();
        private readonly HashSet<SectionPos> enqueued = new();
        private readonly HashSet<SectionPos> active   = new(); // sections rendues

        private void Awake()
        {
            world = GetComponent<WorldRuntime>();
            renderReg = GetComponent<SectionRenderRegistry>();
            worker = new StreamWorker(JobHandler);
            worker.Start();

            BlockRegister.Init();
            var stoneId = BlockRegistry.Get("minecraft:stone").Id;
            generator = new Generation.NoiseTerrainGenerator(seed, noiseScale, baseHeight, amplitude, stoneId);
        }

        private void OnDestroy() => worker.Stop();

        private void Update()
        {
            UpdateWantedSet();
            EnqueueMissing();
            DrainResults(buildBudgetPerFrame);
            DespawnFar();
        }

        private void UpdateWantedSet()
        {
            wanted.Clear();
            var c = center ? center.position : Vector3.zero;
            int csx = Mathf.FloorToInt(c.x/16f);
            int csz = Mathf.FloorToInt(c.z/16f);

            int r = viewRadius;
            for (int dz=-r; dz<=r; dz++)
            for (int dx=-r; dx<=r; dx++)
            {
                int sx = csx+dx;
                int sz = csz+dz;
                for (int sy=0; sy<world.sectionsY; sy++)
                    wanted.Add(new SectionPos(sx,sy,sz));
            }
        }

        private void EnqueueMissing()
        {
            foreach (var sp in wanted)
            {
                if (enqueued.Contains(sp)) continue;
                var path = LevelStorage.SectionPath(world.saveRoot, sp.x, sp.y, sp.z);
                worker.Enqueue(new StreamWorker.Job { sx=sp.x, sy=sp.y, sz=sp.z, path=path });
                enqueued.Add(sp);
            }
        }

        private void DrainResults(int budget)
        {
            int count = 0;
            while (count<budget && worker.TryDequeueResult(out var r))
            {
                var sp = new SectionPos(r.sx, r.sy, r.sz);
                var sec = world.GetOrCreateSection(sp);

                for (int i=0;i<4096;i++){ sec.ids[i]=r.ids[i]; sec.st[i]=r.states[i]; }
                sec.ClearDirty();

                renderReg.EnsureSectionTarget(sp);
                renderReg.MarkDirty(sp);

                active.Add(sp);
                count++;
            }
        }

        private void DespawnFar()
        {
            if (center == null || active.Count == 0) return;

            var c = center.position;
            int csx = Mathf.FloorToInt(c.x/16f);
            int csz = Mathf.FloorToInt(c.z/16f);
            int keep = viewRadius + Mathf.Max(0, despawnMargin);

            int removed = 0;
            // snapshot pour éviter modif collection pendant itération
            var toCheck = ListCache.Take(active.Count);
            toCheck.AddRange(active);

            foreach (var sp in toCheck)
            {
                if (removed >= maxDespawnPerFrame) break;

                int dx = Mathf.Abs(sp.x - csx);
                int dz = Mathf.Abs(sp.z - csz);
                bool outside = dx > keep || dz > keep;

                if (!outside) continue;

                // ne despawn pas si la section est dirty (en attente de save)
                if (world.TryGetSection(sp, out var sec) && sec.Dirty) continue;

                renderReg.RemoveSectionTarget(sp);
                active.Remove(sp);
                enqueued.Remove(sp); // autorisera un futur re-enqueue si on revient
                removed++;
            }

            ListCache.Return(toCheck);
        }

        private StreamWorker.Result JobHandler(StreamWorker.Job j)
        {
            if (LevelReaderRaw.TryReadSection(j.path, out var ids, out var st))
                return new StreamWorker.Result { sx=j.sx, sy=j.sy, sz=j.sz, ids=ids, states=st, fromDisk=true };

            var gen = generator.LoadOrGenerate(j.sx,j.sy,j.sz);
            return new StreamWorker.Result { sx=j.sx, sy=j.sy, sz=j.sz, ids=gen.ids, states=gen.states, fromDisk=false };
        }

        // petit cache list pour éviter alloc GC dans DespawnFar
        private static class ListCache
        {
            static readonly Stack<List<SectionPos>> pool = new();
            public static List<SectionPos> Take(int cap) => pool.Count>0 ? pool.Pop() : new List<SectionPos>(cap);
            public static void Return(List<SectionPos> l){ l.Clear(); pool.Push(l); }
        }
    }
}