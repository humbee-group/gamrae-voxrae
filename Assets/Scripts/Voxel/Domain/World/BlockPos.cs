// Assets/Scripts/Voxel/Domain/World/BlockPos.cs
using System;
using UnityEngine;

namespace Voxel.Domain.World
{
    [Serializable]
    public readonly struct BlockPos : IEquatable<BlockPos>
    {
        public readonly int x, y, z;
        public BlockPos(int x, int y, int z) { this.x=x; this.y=y; this.z=z; }
        public bool Equals(BlockPos o) => x==o.x && y==o.y && z==o.z;
        public override bool Equals(object obj) => obj is BlockPos o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(x,y,z);
        public override string ToString() => $"BlockPos({x},{y},{z})";
    }
}