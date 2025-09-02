// Assets/Scripts/Voxel/Packs/BlockstateResolver.cs
// Résolution robuste : variante sans "model" -> hérite du modèle de la variante par défaut.

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

            var blk = BlockRegistry.Get(id);
            var name = blk.Name;
            int colon = name.IndexOf(':');
            if (colon >= 0) name = name[(colon + 1)..];

            if (!_pack.blockstates.TryGetValue(name, out var bs) || bs.variants == null || bs.variants.Count == 0)
            { mref = default; return false; }

            var props = blk.DecodeState(state);
            var key = Voxel.Domain.Blocks.StateKeyBuilder.Build(props);

            bs.variants.TryGetValue(key ?? "", out var vExact);
            bs.variants.TryGetValue("", out var vDefault);

            var vChosen = vExact ?? vDefault ?? bs.variants.Values.First();

            // Héritage du modèle si manquant
            string model = vChosen.model;
            if (string.IsNullOrEmpty(model))
            {
                if (vDefault != null && !string.IsNullOrEmpty(vDefault.model))
                    model = vDefault.model;
                else
                {
                    var any = bs.variants.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v.model));
                    if (any != null) model = any.model;
                }
            }
            if (string.IsNullOrEmpty(model)) { mref = default; return false; }

            mref = new ModelRef
            {
                model = model,
                rotX = vChosen.x ?? 0,
                rotY = vChosen.y ?? 0,
                uvlock = vChosen.uvlock ?? false
            };
            _cache[(id, state)] = mref;
            return true;
        }
    }
}