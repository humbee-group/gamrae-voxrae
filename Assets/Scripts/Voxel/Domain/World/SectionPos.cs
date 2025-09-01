// Assets/Scripts/Voxel/Domain/World/SectionPos.cs
using System;

namespace Voxel.Domain.World
{
    public readonly struct SectionPos : IEquatable<SectionPos>
    {
        public readonly int x,y,z; // y=index de section (16 voxels)
        public SectionPos(int x,int y,int z){ this.x=x; this.y=y; this.z=z; }
        public bool Equals(SectionPos o)=>x==o.x&&y==o.y&&z==o.z;
        public override bool Equals(object obj)=>obj is SectionPos o&&Equals(o);
        public override int GetHashCode()=>HashCode.Combine(x,y,z);
        public override string ToString()=>$"SectionPos({x},{y},{z})";
    }
}