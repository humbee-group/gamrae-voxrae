// Assets/Scripts/Voxel/domain/block/VoxelShapes.cs
using UnityEngine;

namespace Voxel.Domain.Blocks
{
    public static class VoxelShapes
    {
        public static VoxelShape SlabBottom => new(new Vector3(0, 0, 0), new Vector3(1, 0.5f, 1));
        public static VoxelShape SlabTop => new(new Vector3(0, 0.5f, 0), new Vector3(1, 1f, 1));

        // Escalier simplifié: collision pleine; occlusion partielle au meshing (à venir via IsOccluding).
        public static VoxelShape FullCube => VoxelShape.FullCube;
    }
}