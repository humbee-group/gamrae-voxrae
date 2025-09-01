// Assets/Scripts/Voxel/domain/block/BlockState.cs
// Encodage 1 byte compatible avec ta base: ordre canonique des props
using System;
using Voxel.Domain.Blocks;

namespace Voxel.Domain.Blocks
{
    public readonly struct BlockState : IEquatable<BlockState>
    {
        public readonly ushort Id;   // identifiant bloc (palette globale)
        public readonly byte State;  // 8 bits d'état spécifiques au block

        public BlockState(ushort id, byte state) { Id = id; State = state; }

        public bool Equals(BlockState other) => Id == other.Id && State == other.State;
        public override bool Equals(object obj) => obj is BlockState o && Equals(o);
        public override int GetHashCode() => (Id << 8) ^ State;
        public override string ToString() => $"BlockState(id={Id},st=0x{State:X2})";
    }
}