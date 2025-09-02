// Assets/Scripts/Voxel/Runtime/SectionRenderRegistry.cs
// Shim de compat: redirige MarkDirty/Ensure/Remove vers ChunkRenderDispatcher.

using UnityEngine;
using Voxel.Domain.World;
using Voxel.Client.Renderer.Chunk;

namespace Voxel.Runtime
{
    [RequireComponent(typeof(WorldRuntime))]
    public sealed class SectionRenderRegistry : MonoBehaviour
    {
        public ChunkRenderDispatcher dispatcher;

        private void Awake()
        {
            if (!dispatcher) dispatcher = FindAnyObjectByType<ChunkRenderDispatcher>();
        }

        public void EnsureSectionTarget(SectionPos sp)
        {
            if (dispatcher) dispatcher.RegisterOrUpdateSection(sp);
        }

        public void RemoveSectionTarget(SectionPos sp)
        {
            if (dispatcher) dispatcher.RemoveSection(sp);
        }

        public void MarkDirty(SectionPos sp)
        {
            if (dispatcher) dispatcher.MarkSectionDirty(sp);
        }
    }
}