// Assets/Scripts/Voxel/Runtime/WorldRuntime.cs
// Monde runtime + lumière: hooks vers LightEngine

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.World;
using Voxel.Client.Renderer.Chunk;
using Voxel.Lighting;
using Voxel.IO;

namespace Voxel.Runtime
{
    public sealed class WorldRuntime : MonoBehaviour
    {
        [Header("World")]
        public int sectionsY = 16;
        public string saveRoot;

        [Header("Renderer")]
        public ChunkRenderDispatcher renderDispatcher;

        [Header("Lighting")]
        public LightEngine lightEngine;

        private readonly Dictionary<SectionPos, LevelChunkSection> sections = new();

        private void Awake()
        {
            if (string.IsNullOrEmpty(saveRoot))
                saveRoot = System.IO.Path.Combine(Application.persistentDataPath, "World");
            if (!renderDispatcher)
                renderDispatcher = FindAnyObjectByType<ChunkRenderDispatcher>();
            if (!lightEngine)
                lightEngine = FindAnyObjectByType<LightEngine>();
        }

        public bool TryGetSection(SectionPos sp, out LevelChunkSection sec)
            => sections.TryGetValue(sp, out sec);

        public LevelChunkSection GetOrCreateSection(SectionPos sp)
        {
            if (!sections.TryGetValue(sp, out var sec))
            {
                sec = new LevelChunkSection();
                sections.Add(sp, sec);
                renderDispatcher?.RegisterOrUpdateSection(sp);
            }
            return sec;
        }

        public IEnumerable<(SectionPos pos, LevelChunkSection sec)> Sections()
        {
            foreach (var kv in sections) yield return (kv.Key, kv.Value);
        }

        public void RemoveSection(SectionPos sp)
        {
            if (sections.Remove(sp))
                renderDispatcher?.RemoveSection(sp);
        }

        // ===== Bloc monde =====
        public (ushort id, byte state) GetBlock(int wx, int wy, int wz)
        {
            var sp = WorldToSection(wx, wy, wz, out int lx, out int ly, out int lz);
            return sections.TryGetValue(sp, out var sec)
                ? sec.Get(lx, ly, lz)
                : ((ushort)0, (byte)0);
        }

        public bool SetBlockAndStateAndMark(int wx, int wy, int wz, ushort id, byte state)
            => SetBlock(wx, wy, wz, id, state);

        public bool SetBlock(int wx, int wy, int wz, ushort id, byte state)
        {
            var sp = WorldToSection(wx, wy, wz, out int lx, out int ly, out int lz);
            var sec = GetOrCreateSection(sp);

            var old = sec.Get(lx,ly,lz);
            bool dirty = sec.SetLocal(lx, ly, lz, id, state);
            if (!dirty) return false;

            // Lumière: notifier
            lightEngine?.OnBlockChanged(wx,wy,wz, old.id, old.state, id, state);

            // Remesh local
            renderDispatcher?.MarkSectionDirty(sp);
            // Voisins si bordure
            if (lx == 0)  renderDispatcher?.MarkSectionDirty(new SectionPos(sp.x-1,sp.y,sp.z));
            if (lx == 15) renderDispatcher?.MarkSectionDirty(new SectionPos(sp.x+1,sp.y,sp.z));
            if (ly == 0)  renderDispatcher?.MarkSectionDirty(new SectionPos(sp.x,sp.y-1,sp.z));
            if (ly == 15) renderDispatcher?.MarkSectionDirty(new SectionPos(sp.x,sp.y+1,sp.z));
            if (lz == 0)  renderDispatcher?.MarkSectionDirty(new SectionPos(sp.x,sp.y,sp.z-1));
            if (lz == 15) renderDispatcher?.MarkSectionDirty(new SectionPos(sp.x,sp.y,sp.z+1));

            return true;
        }

        public bool IsSolidAt(int wx, int wy, int wz)
        {
            var (id, st) = GetBlock(wx, wy, wz);
            if (id == 0) return false;
            var b = Voxel.Domain.Registry.BlockRegistry.Get(id);
            if (b.RenderType != Voxel.Domain.Blocks.RenderType.Opaque) return false;
            return b.IsOccluding(st);
        }

        private SectionPos WorldToSection(int wx, int wy, int wz, out int lx, out int ly, out int lz)
        {
            int sx = FloorDiv(wx,16), sy = FloorDiv(wy,16), sz = FloorDiv(wz,16);
            lx = Mod(wx,16); ly=Mod(wy,16); lz=Mod(wz,16);
            return new SectionPos(sx, sy, sz);
        }
        private static int FloorDiv(int a,int b)=> (a>=0)? a/b : ((a-(b-1))/b);
        private static int Mod(int a,int b){ int m=a%b; return m<0? m+b:m; }
    }
}