// Assets/Scripts/Voxel/Runtime/Streaming/FlatGenerator.cs
// Générateur minimal (plancher) utilisé si le disque ne contient rien
using Voxel.Runtime.Streaming;

namespace Voxel.Runtime.Generation
{
    public sealed class FlatGenerator : ISectionSource
    {
        private readonly ushort stoneId;
        public FlatGenerator(ushort stoneId) { this.stoneId = stoneId; }

        public (ushort[] ids, byte[] states, bool fromDisk) LoadOrGenerate(int sx,int sy,int sz)
        {
            var ids = new ushort[16*16*16];
            var st  = new byte  [16*16*16];
            // y-monde = sy*16 + ly; poser sol à y==0
            for (int ly=0; ly<16; ly++)
            for (int lz=0; lz<16; lz++)
            for (int lx=0; lx<16; lx++)
            {
                int wy = sy*16 + ly;
                if (wy == 0)
                {
                    int i=((ly*16)+lz)*16+lx;
                    ids[i]=stoneId; st[i]=0;
                }
            }
            return (ids, st, false);
        }
    }
}