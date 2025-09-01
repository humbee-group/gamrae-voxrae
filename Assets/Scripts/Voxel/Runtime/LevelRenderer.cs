// Assets/Scripts/Voxel/Runtime/LevelRenderer.cs
// Ne jamais supprimer les commentaires

using System.Collections.Generic;
using UnityEngine;
using Voxel.Meshing;

namespace Voxel.Runtime
{
    public sealed class LevelRenderer : MonoBehaviour
    {
        public MonoBehaviour uvProviderBehaviour; // IUVProvider
        private IUVProvider uvProvider;

        [Header("Materials (0=Opaque,1=Cutout,2=Translucent)")]
        public Material opaqueMaterial;
        public Material cutoutMaterial;
        public Material translucentMaterial;

        [Header("Budgets")]
        [Range(1, 128)] public int meshBuildBudgetPerFrame = 16;
        [Range(1, 128)] public int meshAssignBudgetPerFrame = 16;
        [Range(1, 128)] public int colliderAssignBudgetPerFrame = 8;

        private readonly Dictionary<(int sx,int sy,int sz), Target> targets = new();
        private readonly Queue<(int sx,int sy,int sz)> dirtyQ = new();
        private readonly HashSet<(int sx,int sy,int sz)> dirtySet = new();
        private readonly Queue<Result> builtQ = new();
        private readonly Queue<Result> colliderQ = new();

        private void Awake()
        {
            uvProvider = uvProviderBehaviour as IUVProvider;
        }

        public void RegisterSectionTarget(int sx, int sy, int sz, MeshFilter filter, MeshCollider collider, ISectionReader reader)
        {
            targets[(sx, sy, sz)] = new Target { filter = filter, collider = collider, reader = reader };
            var mr = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (mr != null)
            {
                mr.sharedMaterials = new[] { opaqueMaterial, cutoutMaterial, translucentMaterial };
            }
        }

        public void MarkSectionDirty(int sx, int sy, int sz)
        {
            var key = (sx, sy, sz);
            if (dirtySet.Add(key)) dirtyQ.Enqueue(key);
        }

        public void MarkAllRegisteredDirty()
        {
            foreach (var key in targets.Keys)
                if (dirtySet.Add(key)) dirtyQ.Enqueue(key);
        }

        private void Update()
        {
            int builds = 0;
            while (builds < meshBuildBudgetPerFrame && dirtyQ.Count > 0)
            {
                var key = dirtyQ.Dequeue();
                dirtySet.Remove(key);
                if (!targets.TryGetValue(key, out var t) || t.reader == null || uvProvider == null) continue;
                var mesh = SectionMesher.BuildMesh(t.reader, uvProvider);
                builtQ.Enqueue(new Result { key = key, mesh = mesh });
                builds++;
            }

            int assigns = 0;
            while (assigns < meshAssignBudgetPerFrame && builtQ.Count > 0)
            {
                var r = builtQ.Dequeue();
                if (targets.TryGetValue(r.key, out var t) && t.filter != null)
                {
                    var old = t.filter.sharedMesh;
                    t.filter.sharedMesh = r.mesh;
                    if (old != null) Destroy(old);
                    colliderQ.Enqueue(r);
                    assigns++;
                }
                else Destroy(r.mesh);
            }

            int col = 0;
            while (col < colliderAssignBudgetPerFrame && colliderQ.Count > 0)
            {
                var r = colliderQ.Dequeue();
                if (targets.TryGetValue(r.key, out var t) && t.collider != null)
                {
                    var old = t.collider.sharedMesh;
                    t.collider.sharedMesh = r.mesh;
                    if (old != null && old != t.filter.sharedMesh) Destroy(old);
                }
                col++;
            }
        }

        private void OnDisable()
        {
            while (builtQ.Count > 0) Destroy(builtQ.Dequeue().mesh);
            colliderQ.Clear(); dirtyQ.Clear(); dirtySet.Clear();
        }

        private struct Target { public MeshFilter filter; public MeshCollider collider; public ISectionReader reader; }
        private struct Result { public (int sx,int sy,int sz) key; public Mesh mesh; }
    }
}