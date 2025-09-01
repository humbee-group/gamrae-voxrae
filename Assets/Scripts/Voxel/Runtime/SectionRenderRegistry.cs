// Assets/Scripts/Voxel/Runtime/SectionRenderRegistry.cs
// Ne jamais supprimer les commentaires

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.World;
using Voxel.Meshing;

namespace Voxel.Runtime
{
    [RequireComponent(typeof(WorldRuntime))]
    public sealed class SectionRenderRegistry : MonoBehaviour
    {
        [Header("Renderer")]
        public LevelRenderer renderDispatcher;

        private WorldRuntime world;
        private readonly Dictionary<SectionPos, (MeshFilter mf, MeshCollider mc)> _targets = new();

        private void Awake()
        {
            world = GetComponent<WorldRuntime>();
            if (!renderDispatcher) renderDispatcher = FindAnyObjectByType<LevelRenderer>();
        }

        public void EnsureSectionTarget(SectionPos sp)
        {
            if (_targets.ContainsKey(sp)) return;

            var go = new GameObject($"Section_{sp.x}_{sp.y}_{sp.z}");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(sp.x*16, sp.y*16, sp.z*16);

            var mf = go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var mc = go.AddComponent<MeshCollider>();

            renderDispatcher.RegisterSectionTarget(sp.x, sp.y, sp.z, mf, mc, new SectionReaderProxy(world, sp));
            _targets[sp] = (mf, mc);
        }

        public void RemoveSectionTarget(SectionPos sp)
        {
            if (!_targets.TryGetValue(sp, out var t)) return;
            if (t.mf) Destroy(t.mf.gameObject);
            _targets.Remove(sp);
        }

        public void MarkDirty(SectionPos sp)
        {
            // S’assure que la cible existe puis marque dirty
            EnsureSectionTarget(sp);
            renderDispatcher.MarkSectionDirty(sp.x, sp.y, sp.z);
        }

        private sealed class SectionReaderProxy : ISectionReader
        {
            private readonly WorldRuntime world; private readonly SectionPos sp;
            public SectionReaderProxy(WorldRuntime w, SectionPos p){ world=w; sp=p; }
            public (ushort id, byte state) Get(int lx,int ly,int lz)
                => world.TryGetSection(sp, out var s) ? s.Get(lx,ly,lz) : ((ushort)0,(byte)0);
            public bool InBounds(int lx,int ly,int lz) => lx>=0&&lx<16 && ly>=0&&ly<16 && lz>=0&&lz<16;
        }
    }
}