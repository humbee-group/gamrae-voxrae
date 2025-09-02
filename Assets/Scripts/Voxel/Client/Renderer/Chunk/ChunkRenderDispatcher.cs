// Assets/Scripts/Voxel/Client/Renderer/Chunk/ChunkRenderDispatcher.cs
// Fournit LightNeighborhood (résout coins/arrêtes via modulo + world.TryGetSection) et appelle le mesher avec lumière.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Voxel.Domain.World;
using Voxel.Meshing;

namespace Voxel.Client.Renderer.Chunk
{
    public enum TessellatorMode { NonGreedy, Greedy }

    [RequireComponent(typeof(Voxel.Runtime.WorldRuntime))]
    public sealed class ChunkRenderDispatcher : MonoBehaviour
    {
        [Header("Materials (0=Opaque,1=Cutout,2=Translucent)")]
        public Material opaqueMaterial;
        public Material cutoutMaterial;
        public Material translucentMaterial;

        [Header("Providers")]
        public MonoBehaviour uvProviderBehaviour; // IUVProvider
        private IUVProvider uvProvider;

        [Header("Budgets")]
        [Range(1,256)] public int meshBuildBudgetPerFrame = 32;
        [Range(1,256)] public int meshAssignBudgetPerFrame = 32;
        [Range(0,256)] public int colliderAssignBudgetPerFrame = 12;

        [Header("Anneaux / LOD")]
        public Transform center;
        [Range(1,64)] public int viewRadius = 12;
        [Range(0,8)]  public int colliderMaxRing = 3;
        [Range(0,8)]  public int shadowMaxRing = 2;

        [Header("Culling")]
        public bool frustumCulling = true;

        [Header("Meshing")]
        public TessellatorMode tessellator = TessellatorMode.NonGreedy;

        private Camera cam;
        private Plane[] planes = new Plane[6];

        private readonly Dictionary<SectionPos, RenderSection> sections = new();
        private readonly Queue<SectionPos> dirtyQ = new();
        private readonly HashSet<SectionPos> dirtySet = new();

        private readonly Queue<(SectionPos key, Mesh mesh)> builtQ = new();
        private readonly Queue<(SectionPos key, Mesh mesh)> colliderQ = new();

        private Voxel.Runtime.WorldRuntime world;

        private void Awake()
        {
            world = GetComponent<Voxel.Runtime.WorldRuntime>();
            uvProvider = uvProviderBehaviour as IUVProvider;
            cam = Camera.main;
        }

        public void RegisterOrUpdateSection(SectionPos sp)
        {
            if (!sections.ContainsKey(sp))
            {
                var rs = RenderSection.Create(transform, sp);
                sections.Add(sp, rs);
                rs.mr.sharedMaterials = new[] { opaqueMaterial, cutoutMaterial, translucentMaterial };
                rs.mr.receiveShadows = true;
                rs.mr.shadowCastingMode = ShadowCastingMode.On;
                rs.mr.enabled = true;
            }
        }

        public void RemoveSection(SectionPos sp)
        {
            if (sections.TryGetValue(sp, out var rs))
            {
                RenderSection.Recycle(rs);
                sections.Remove(sp);
            }
        }

        public void MarkSectionDirty(SectionPos sp)
        {
            if (dirtySet.Add(sp)) dirtyQ.Enqueue(sp);
        }

        public void MarkAllRegisteredDirty()
        {
            foreach (var sp in sections.Keys)
                if (dirtySet.Add(sp)) dirtyQ.Enqueue(sp);
        }

        private void LateUpdate()
        {
            if (center == null && cam != null) center = cam.transform;
            if (frustumCulling && cam != null) planes = GeometryUtility.CalculateFrustumPlanes(cam);

            UpdateRingsAndCulling();

            int builds = 0;
            while (builds < meshBuildBudgetPerFrame && dirtyQ.Count > 0)
            {
                var sp = dirtyQ.Dequeue();
                dirtySet.Remove(sp);
                if (!sections.TryGetValue(sp, out var rs)) continue;
                if (!world.TryGetSection(sp, out _)) continue;
                if (uvProvider == null) break;

                var nb  = new Neighborhood(world, sp);
                var lp  = new LightNeighborhood(world, sp);   // lumière voxel

                Mesh mesh = ChunkTessellator.BuildMesh(nb, uvProvider, lp);
                builtQ.Enqueue((sp, mesh));
                builds++;
            }

            int assigns = 0;
            while (assigns < meshAssignBudgetPerFrame && builtQ.Count > 0)
            {
                var item = builtQ.Dequeue();
                if (!sections.TryGetValue(item.key, out var rs))
                { Destroy(item.mesh); continue; }

                var old = rs.mf.sharedMesh;
                rs.mf.sharedMesh = item.mesh;
                if (old) Destroy(old);

                if (rs.ring <= colliderMaxRing) colliderQ.Enqueue(item);
                assigns++;
            }

            int cols = 0;
            while (cols < colliderAssignBudgetPerFrame && colliderQ.Count > 0)
            {
                var item = colliderQ.Dequeue();
                if (!sections.TryGetValue(item.key, out var rs)) continue;
                var old = rs.mc.sharedMesh;
                rs.mc.sharedMesh = item.mesh;
                if (old && old != rs.mf.sharedMesh) Destroy(old);
                cols++;
            }
        }

        private void UpdateRingsAndCulling()
        {
            if (sections.Count == 0) return;
            Vector3 c = center ? center.position : Vector3.zero;
            int csx = Mathf.FloorToInt(c.x / 16f);
            int csz = Mathf.FloorToInt(c.z / 16f);

            foreach (var kv in sections)
            {
                var sp = kv.Key;
                var rs = kv.Value;
                int dx = Mathf.Abs(sp.x - csx);
                int dz = Mathf.Abs(sp.z - csz);
                rs.ring = Mathf.Max(dx, dz);

                bool inView = rs.ring <= viewRadius;
                if (frustumCulling && cam != null && inView)
                    inView = GeometryUtility.TestPlanesAABB(planes, rs.worldBounds);

                rs.mr.enabled = inView;
                if (!inView) continue;

                rs.mr.receiveShadows = rs.ring <= shadowMaxRing;
                rs.mr.shadowCastingMode = rs.ring <= shadowMaxRing ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }

        private readonly struct Neighborhood : Voxel.Client.Renderer.Chunk.ISectionNeighborhood
        {
            private readonly Voxel.Runtime.WorldRuntime world;
            private readonly SectionPos self;

            public Neighborhood(Voxel.Runtime.WorldRuntime w, SectionPos sp) { world = w; self = sp; }

            public (ushort id, byte state) GetWithNeighbors(int lx, int ly, int lz)
            {
                int sx = self.x, sy = self.y, sz = self.z;
                int x = lx, y = ly, z = lz;

                if (lx < 0) { sx--; x = 16 + lx; } else if (lx > 15) { sx++; x = lx - 16; }
                if (ly < 0) { sy--; y = 16 + ly; } else if (ly > 15) { sy++; y = ly - 16; }
                if (lz < 0) { sz--; z = 16 + lz; } else if (lz > 15) { sz++; z = lz - 16; }

                if (world.TryGetSection(new SectionPos(sx, sy, sz), out var s))
                    return s.Get(x, y, z);
                return ((ushort)0, (byte)0);
            }

            public bool Exists(int lx, int ly, int lz) => lx>=0 && lx<16 && ly>=0 && ly<16 && lz>=0 && lz<16;
        }

        // === lumière voxel robuste (gère coins/arrêtes via modulo + world.TryGetSection) ===
        private readonly struct LightNeighborhood : Voxel.Meshing.ILightProvider
        {
            private readonly Voxel.Runtime.WorldRuntime world;
            private readonly SectionPos baseSp;

            public LightNeighborhood(Voxel.Runtime.WorldRuntime w, SectionPos sp){ world=w; baseSp=sp; }

            public byte GetSky(int lx, int ly, int lz)
            {
                if (!Resolve(ref lx, ref ly, ref lz, out var sec)) return 0;
                int idx = ((ly*16)+lz)*16 + lx;
                return (uint)idx < 4096u ? sec.sky[idx] : (byte)0;
            }

            public byte GetBlk(int lx, int ly, int lz)
            {
                if (!Resolve(ref lx, ref ly, ref lz, out var sec)) return 0;
                int idx = ((ly*16)+lz)*16 + lx;
                return (uint)idx < 4096u ? sec.block[idx] : (byte)0;
            }

            private bool Resolve(ref int x, ref int y, ref int z, out LevelChunkSection sec)
            {
                int sx=baseSp.x, sy=baseSp.y, sz=baseSp.z;

                if (x < 0)   { int d=(x-15)/16; sx+=d; x-=16*d; }
                else if (x > 15){ int d=x/16;   sx+=d; x-=16*d; }

                if (y < 0)   { int d=(y-15)/16; sy+=d; y-=16*d; }
                else if (y > 15){ int d=y/16;   sy+=d; y-=16*d; }

                if (z < 0)   { int d=(z-15)/16; sz+=d; z-=16*d; }
                else if (z > 15){ int d=z/16;   sz+=d; z-=16*d; }

                return world.TryGetSection(new SectionPos(sx,sy,sz), out sec);
            }
        }
    }
}