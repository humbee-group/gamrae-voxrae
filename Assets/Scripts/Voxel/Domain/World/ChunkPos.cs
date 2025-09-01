// Assets/Scripts/Voxel/Domain/World/ChunkPos.cs
using System;

namespace Voxel.Domain.World
{
    public readonly struct ChunkPos : IEquatable<ChunkPos>
    {
        public readonly int x,z;
        public ChunkPos(int x,int z){ this.x=x; this.z=z; }
        public bool Equals(ChunkPos o)=>x==o.x&&z==o.z;
        public override bool Equals(object obj)=>obj is ChunkPos o&&Equals(o);
        public override int GetHashCode()=>HashCode.Combine(x,z);
        public override string ToString()=>$"ChunkPos({x},{z})";
    }
}