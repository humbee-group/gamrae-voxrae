// Assets/Scripts/Voxel/Runtime/WorldRuntime.cs
// Ne jamais supprimer les commentaires

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.World;
using Voxel.Domain.WorldRuntime;
using Voxel.Domain.Registry;

namespace Voxel.Runtime
{
    [RequireComponent(typeof(SectionRenderRegistry))]
    public sealed class WorldRuntime : MonoBehaviour
    {
        [Header("World")]
        public string saveRoot = "World";
        public int sectionsY = 16;

        [Header("Render hook")]
        public SectionRenderRegistry renderRegistry;

        private readonly Dictionary<SectionPos, LevelChunkSection> _sections = new();

        private void Awake()
        {
            if (!renderRegistry) renderRegistry = GetComponent<SectionRenderRegistry>();
        }

        public bool TryGetSection(SectionPos sp, out LevelChunkSection sec) => _sections.TryGetValue(sp, out sec);

        public LevelChunkSection GetOrCreateSection(SectionPos sp)
        {
            if (_sections.TryGetValue(sp, out var s)) return s;
            s = new LevelChunkSection();
            _sections[sp] = s;
            return s;
        }

        public bool SetBlockAndStateAndMark(int wx,int wy,int wz, ushort id, byte st)
        {
            var bp = new Voxel.Domain.World.BlockPos(wx,wy,wz);
            var sp = Level.ToSectionPos(bp);
            var lp = Level.ToLocalInSection(bp);
            var sec = GetOrCreateSection(sp);
            var changed = sec.SetLocal(lp.x, lp.y, lp.z, id, st);
            if (changed) renderRegistry?.MarkDirty(sp);
            return changed;
        }

        public bool TryGetBlock(int wx,int wy,int wz, out ushort id, out byte state)
        {
            var bp = new Voxel.Domain.World.BlockPos(wx,wy,wz);
            var sp = Level.ToSectionPos(bp);
            if (!_sections.TryGetValue(sp, out var sec)) { id = 0; state = 0; return false; }
            var lp = Level.ToLocalInSection(bp);
            var t = sec.Get(lp.x, lp.y, lp.z);
            id = t.id; state = t.state; return true;
        }

        public bool IsSolidAt(int wx,int wy,int wz)
        {
            if (!TryGetBlock(wx,wy,wz, out var id, out var st)) return false;
            if (id == 0) return false;
            var b = BlockRegistry.Get(id);
            return b != null && (b.IsOccluding(st) || b.IsOpaque(st));
        }

        // 6) GC sections: retire la section du monde si non dirty
        public bool TryDespawnSection(SectionPos sp)
        {
            if (!_sections.TryGetValue(sp, out var sec)) return true;
            if (sec.Dirty) return false;
            _sections.Remove(sp);
            return true;
        }

        public IEnumerable<(SectionPos sp, LevelChunkSection sec)> Sections()
            => System.Linq.Enumerable.Select(_sections, kv => (kv.Key, kv.Value));
    }
}