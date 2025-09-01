// Assets/Scripts/Voxel/Domain/Runtime/Placement/PlacementAPI.cs
// Supprime la ref au VoxelBlockRegistry

using Voxel.Domain.World;
using Voxel.Domain.WorldRuntime;

namespace Voxel.Runtime.Placement
{
    public static class PlacementAPI
    {
        // setFunc: (wx,wy,wz,id,state) -> bool dirty
        public static void Place(
            Voxel.Domain.Blocks.BlockPlaceContext ctx,
            ushort id,
            byte finalState,
            System.Func<int,int,int,ushort,byte,bool> setFunc)
        {
#if UNITY_EDITOR
            // Ici, pas de validation mask: registre statique ne stocke pas de mask
#endif
            Level.SetBlockAndStateAndMark(setFunc, ctx.Target.x, ctx.Target.y, ctx.Target.z, id, finalState);
        }
    }
}