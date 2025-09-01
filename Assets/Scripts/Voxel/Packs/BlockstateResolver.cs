// Assets/Scripts/Voxel/Packs/BlockstateResolver.cs
// Utilise le registre statique

using System.Linq;
using System.Collections.Generic;
using Voxel.Domain.Registry;
using Voxel.Packs.Json;

namespace Voxel.Packs
{
    public sealed class BlockstateResolver
    {
        public struct ModelRef { public string model; public int rotX, rotY; public bool uvlock; }

        private readonly Pack _pack;
        private readonly Dictionary<(ushort, byte), ModelRef> _cache = new();

        public BlockstateResolver(Pack pack) { _pack = pack; }

        public bool TryResolve(ushort id, byte state, out ModelRef mref)
        {
            if (_cache.TryGetValue((id, state), out mref)) return true;

            var block = BlockRegistry.Get(id);
            var fullName = block.Name; // "minecraft:stone"
            var name = fullName.Contains(":") ? fullName.Split(':')[1] : fullName;

            if (!_pack.blockstates.TryGetValue(name, out var bs) || bs.variants == null || bs.variants.Count == 0)
            { mref = default; return false; }

            var props = block.DecodeState(state);
            var stateKey = Voxel.Domain.Blocks.StateKeyBuilder.Build(props);

            BlockstateJson.Variant v;
            if (!string.IsNullOrEmpty(stateKey) && bs.variants.TryGetValue(stateKey, out v))
            { /* exact */ }
            else
            { v = bs.variants.First().Value; }

            mref = new ModelRef
            {
                model = v.model,
                rotX = v.x ?? 0,
                rotY = v.y ?? 0,
                uvlock = v.uvlock ?? false
            };
            _cache[(id, state)] = mref;
            return true;
        }
    }
}