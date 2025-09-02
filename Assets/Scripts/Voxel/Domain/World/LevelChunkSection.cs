// Assets/Scripts/Voxel/Domain/World/LevelChunkSection.cs
// Stockage 16³ + lecteur + buffers de lumière

using Voxel.Meshing;

namespace Voxel.Domain.World
{
    public sealed class LevelChunkSection : ISectionReader
    {
        public const int Size = 16;
        public readonly ushort[] ids = new ushort[Size*Size*Size];
        public readonly byte[]   st  = new byte  [Size*Size*Size];

        // Lumière (0..15)
        public readonly byte[] sky  = new byte[Size*Size*Size];
        public readonly byte[] block= new byte[Size*Size*Size];

        public bool Dirty { get; private set; }

        public (ushort id, byte state) Get(int lx,int ly,int lz)
        {
            int i = ((ly*Size)+lz)*Size+lx;
            return (ids[i], st[i]);
        }
        public bool InBounds(int lx,int ly,int lz)
            => lx>=0&&lx<Size && ly>=0&&ly<Size && lz>=0&&lz<Size;

        public bool SetLocal(int lx,int ly,int lz, ushort id, byte state)
        {
            int i=((ly*Size)+lz)*Size+lx;
            if (ids[i]==id && st[i]==state) return false;
            ids[i]=id; st[i]=state; Dirty=true; return true;
        }
        public void ClearDirty()=>Dirty=false;

        public int Index(int lx,int ly,int lz) => ((ly*Size)+lz)*Size+lx;
        public byte GetSky(int lx,int ly,int lz)   => sky[Index(lx,ly,lz)];
        public byte GetBlk(int lx,int ly,int lz)   => block[Index(lx,ly,lz)];
        public void SetSky(int lx,int ly,int lz, byte v) => sky[Index(lx,ly,lz)] = v;
        public void SetBlk(int lx,int ly,int lz, byte v) => block[Index(lx,ly,lz)] = v;
    }
}