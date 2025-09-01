// Assets/Scripts/Voxel/Domain/Registry/BlockRegistry.cs
// Registre statique façon Minecraft

using System;
using System.Collections.Generic;
using Voxel.Domain.Blocks;

namespace Voxel.Domain.Registry
{
    public static class BlockRegistry
    {
        private static readonly Dictionary<string, Block> _byName = new(StringComparer.Ordinal);
        private static readonly Dictionary<ushort, Block> _byId   = new();
        private static readonly Dictionary<Block, ushort> _toId   = new();

        private static ushort _nextId = 1; // 0=air

        public static Block AIR { get; } = new AirBlock { Name = "minecraft:air", Id = 0 };

        public static void Register(string name, Block block)
        {
            if (_byName.ContainsKey(name))
                throw new InvalidOperationException($"Block déjà enregistré: {name}");
            var id = _nextId++;
            block.Name = name;
            block.Id = id;
            _byName[name] = block;
            _byId[id] = block;
            _toId[block] = id;
        }

        public static Block Get(string name) => _byName.TryGetValue(name, out var b) ? b : AIR;
        public static Block Get(ushort id) => _byId.TryGetValue(id, out var b) ? b : AIR;
        public static ushort GetId(Block block) => _toId.TryGetValue(block, out var id) ? id : (ushort)0;
    }
}