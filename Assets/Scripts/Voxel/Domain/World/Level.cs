// Assets/Scripts/Voxel/Domain/World/Level.cs
// Utils de coordonnées + écriture atomique
using UnityEngine;

namespace Voxel.Domain.WorldRuntime
{
    public static class Level
    {
        public const int ChunkSize = 16;
        public const int SectionHeight = 16;
        public static int SectionsY = 16;

        public static Voxel.Domain.World.ChunkPos ToChunkPos(Voxel.Domain.World.BlockPos p)
            => new(Mathf.FloorToInt(FloorDiv(p.x,ChunkSize)), Mathf.FloorToInt(FloorDiv(p.z,ChunkSize)));

        public static Voxel.Domain.World.SectionPos ToSectionPos(Voxel.Domain.World.BlockPos p)
            => new(Mathf.FloorToInt(FloorDiv(p.x,ChunkSize)), Mathf.FloorToInt(FloorDiv(p.y,SectionHeight)), Mathf.FloorToInt(FloorDiv(p.z,ChunkSize)));

        public static Vector3Int ToLocalInSection(Voxel.Domain.World.BlockPos p)
            => new(Mod(p.x,ChunkSize), Mod(p.y,SectionHeight), Mod(p.z,ChunkSize));

        // setFunc(wx,wy,wz,id,state) -> true si dirty
        public static void SetBlockAndStateAndMark(System.Func<int,int,int,ushort,byte,bool> setFunc, int wx,int wy,int wz, ushort id, byte state)
        {
            if (setFunc != null) _ = setFunc(wx,wy,wz,id,state);
        }

        private static int FloorDiv(int a,int b)=> (a>=0)? a/b : ((a-(b-1))/b);
        private static int Mod(int a,int b){ int m=a%b; return m<0? m+b:m; }
    }
}