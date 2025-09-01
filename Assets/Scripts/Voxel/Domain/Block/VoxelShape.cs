// Assets/Scripts/Voxel/domain/block/VoxelShape.cs
using UnityEngine;

namespace Voxel.Domain.Blocks
{
    // AABB unique; pour formes multiples, fournir plusieurs VoxelShape via VoxelShapes.Merge si besoin plus tard.
    public readonly struct VoxelShape
    {
        public readonly Vector3 Min;
        public readonly Vector3 Max;
        public VoxelShape(Vector3 min, Vector3 max) { Min = min; Max = max; }
        public static readonly VoxelShape FullCube = new(Vector3.zero, Vector3.one);
        public Bounds ToBounds() => new((Min + Max) * 0.5f, Max - Min);
    }
}