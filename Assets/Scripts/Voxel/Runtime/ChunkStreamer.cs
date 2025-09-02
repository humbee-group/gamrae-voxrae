// Assets/Scripts/Voxel/Runtime/ChunkStreamer.cs
// Corrige l'usage de StreamWorker.Result (pas de sky/block) et recharge sky/block côté main thread si fromDisk.

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.World;
using Voxel.IO;
using Voxel.Runtime.Streaming;
using Voxel.Domain.Registry;
using Voxel.Client.Renderer.Chunk;
using Voxel.Lighting;

namespace Voxel.Runtime
{
    [RequireComponent(typeof(WorldRuntime))]
    public sealed class ChunkStreamer : MonoBehaviour
    {
        [Header("Streaming")]
        public Transform center;
        [Range(1,64)] public int viewRadius = 12;
        public int enqueueBudgetPerFrame = 64;
        public int applyBudgetPerFrame = 24;

        [Header("Despawn (GC)")]
        public int despawnMargin = 2;
        public int maxDespawnPerFrame = 48;

        [Header("Terrain")]
        public int seed = 1337;
        public float noiseScale = 0.0125f;
        public int baseHeight = 8;
        public int amplitude = 24;

        [Header("Render")]
        public ChunkRenderDispatcher dispatcher;

        private WorldRuntime world;
        private StreamWorker worker;
        private ISectionSource generator;
        private LightEngine lightEngine;

        private readonly HashSet<SectionPos> wanted   = new();
        private readonly HashSet<SectionPos> enqueued = new();
        private readonly HashSet<SectionPos> active   = new();

        private readonly List<(SectionPos sp, int ring, int d2)> toEnqueue = new();

        private void Awake()
        {
            world = GetComponent<WorldRuntime>();
            if (!dispatcher) dispatcher = FindAnyObjectByType<ChunkRenderDispatcher>();
            lightEngine = FindAnyObjectByType<LightEngine>();

            worker = new StreamWorker(JobHandler);
            worker.Start();

            BlockRegister.Init();
            var stoneId = Voxel.Domain.Registry.BlockRegistry.Get("stone").Id;
            var dirtId  = Voxel.Domain.Registry.BlockRegistry.Get("dirt").Id;
            var grassId = Voxel.Domain.Registry.BlockRegistry.Get("grass").Id;

            generator = new Generation.NoiseTerrainGenerator(
                seed, noiseScale, baseHeight, amplitude,
                stoneId, dirtId, grassId,
                dirtThickness: 3, bedrock: true);
        }

        private void Start()
        {
            Vector3 c = center ? center.position : Vector3.zero;
            int csx = Mathf.FloorToInt(c.x / 16f);
            int csz = Mathf.FloorToInt(c.z / 16f);

            for (int sy = 0; sy < world.sectionsY; sy++)
            {
                var sp = new SectionPos(csx, sy, csz);
                if (active.Contains(sp)) continue;

                var path = LevelStorage.SectionPath(world.saveRoot, sp.x, sp.y, sp.z);
                if (LevelStorage.TryLoadSection(path, out var ids, out var st, out var sky, out var blk))
                {
                    var sec = world.GetOrCreateSection(sp);
                    for (int i = 0; i < 4096; i++) { sec.ids[i] = ids[i]; sec.st[i] = st[i]; sec.sky[i] = sky[i]; sec.block[i] = blk[i]; }
                    sec.ClearDirty();
                }
                else
                {
                    var gen = generator.LoadOrGenerate(sp.x, sp.y, sp.z);
                    var sec = world.GetOrCreateSection(sp);
                    for (int i = 0; i < 4096; i++) { sec.ids[i] = gen.ids[i]; sec.st[i] = gen.states[i]; }
                    sec.ClearDirty();
                    lightEngine?.OnSectionLoaded(sp);
                }

                dispatcher.RegisterOrUpdateSection(sp);
                dispatcher.MarkSectionDirty(sp);
                active.Add(sp);
            }
            
        }

        private void OnDestroy() => worker.Stop();

        private void Update()
        {
            BuildWantedDisk();
            EnqueueByRings(enqueueBudgetPerFrame);
            DrainResults(applyBudgetPerFrame);
            DespawnOutsideDisk();
        }

        private void BuildWantedDisk()
        {
            wanted.Clear();
            Vector3 c = center ? center.position : Vector3.zero;
            int csx = Mathf.FloorToInt(c.x / 16f);
            int csz = Mathf.FloorToInt(c.z / 16f);
            int r = Mathf.Max(1, viewRadius); int r2=r*r;

            for (int dz=-r; dz<=r; dz++)
            for (int dx=-r; dx<=r; dx++)
            {
                int d2 = dx*dx + dz*dz;
                if (d2>r2) continue;
                int sx=csx+dx, sz=csz+dz;
                for (int sy=0; sy<world.sectionsY; sy++) wanted.Add(new SectionPos(sx,sy,sz));
            }
        }

        private void EnqueueByRings(int budget)
        {
            if (budget<=0) return;
            toEnqueue.Clear();
            Vector3 c = center ? center.position : Vector3.zero;
            int csx = Mathf.FloorToInt(c.x / 16f);
            int csz = Mathf.FloorToInt(c.z / 16f);

            foreach (var sp in wanted)
            {
                if (enqueued.Contains(sp) || active.Contains(sp)) continue;
                int dx = Mathf.Abs(sp.x - csx), dz = Mathf.Abs(sp.z - csz);
                int ring = Mathf.Max(dx,dz);
                int ddx = sp.x - csx, ddz = sp.z - csz;
                int d2 = ddx*ddx + ddz*ddz;
                toEnqueue.Add((sp, ring, d2));
            }

            toEnqueue.Sort((a,b)=> a.ring!=b.ring ? a.ring.CompareTo(b.ring) : a.d2.CompareTo(b.d2));

            int count=0;
            for (int i=0;i<toEnqueue.Count && count<budget;i++)
            {
                var sp = toEnqueue[i].sp;
                var path = LevelStorage.SectionPath(world.saveRoot, sp.x, sp.y, sp.z);
                worker.Enqueue(new StreamWorker.Job { sx=sp.x, sy=sp.y, sz=sp.z, path=path });
                enqueued.Add(sp); count++;
            }
        }

        private void DrainResults(int budget)
        {
            int count=0;
            while (count<budget && worker.TryDequeueResult(out var r))
            {
                var sp = new SectionPos(r.sx,r.sy,r.sz);
                if (!wanted.Contains(sp)) { enqueued.Remove(sp); count++; continue; }

                var sec = world.GetOrCreateSection(sp);
                for (int i=0;i<4096;i++){ sec.ids[i]=r.ids[i]; sec.st[i]=r.states[i]; }
                sec.ClearDirty();

                // Si la section provient du disque, recharge sky/block ici et applique
                if (r.fromDisk)
                {
                    var path = LevelStorage.SectionPath(world.saveRoot, sp.x, sp.y, sp.z);
                    if (LevelStorage.TryLoadSection(path, out _, out _, out var sky, out var blk))
                    {
                        for (int i=0;i<4096;i++){ sec.sky[i]=sky[i]; sec.block[i]=blk[i]; }
                    }
                }
                else
                {
                    // Générée → calculer la lumière
                    lightEngine?.OnSectionLoaded(sp);
                }

                dispatcher.RegisterOrUpdateSection(sp);
                dispatcher.MarkSectionDirty(sp);

                active.Add(sp);
                enqueued.Remove(sp);
                count++;
            }
        }

        private void DespawnOutsideDisk()
        {
            if (center==null || active.Count==0) return;
            Vector3 c = center.position; int csx=Mathf.FloorToInt(c.x/16f), csz=Mathf.FloorToInt(c.z/16f);
            int keep=viewRadius+Mathf.Max(0, despawnMargin); int keep2=keep*keep;

            int removed=0;
            var snapshot = new List<SectionPos>(active);
            foreach (var sp in snapshot)
            {
                if (removed>=maxDespawnPerFrame) break;
                int ddx=sp.x-csx, ddz=sp.z-csz, d2=ddx*ddx+ddz*ddz;
                if (d2<=keep2) continue;

                if (world.TryGetSection(sp, out var sec) && sec.Dirty) continue;

                dispatcher.RemoveSection(sp);
                active.Remove(sp); enqueued.Remove(sp);
                removed++;
            }
        }

        private StreamWorker.Result JobHandler(StreamWorker.Job j)
        {
            // Result ne porte que ids/states + fromDisk
            if (LevelReaderRaw.TryReadSection(j.path, out var ids, out var st))
                return new StreamWorker.Result { sx=j.sx, sy=j.sy, sz=j.sz, ids=ids, states=st, fromDisk=true };

            var gen = generator.LoadOrGenerate(j.sx, j.sy, j.sz);
            return new StreamWorker.Result { sx=j.sx, sy=j.sy, sz=j.sz, ids=gen.ids, states=gen.states, fromDisk=false };
        }
    }
}