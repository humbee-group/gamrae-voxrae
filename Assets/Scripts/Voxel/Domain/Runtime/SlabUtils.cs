// Assets/Scripts/Voxel/runtime/SlabUtils.cs
using Voxel.Domain.Blocks;

namespace Voxel.Runtime
{
    public static class SlabUtils
    {
        public static SlabType FromHitY(float localY01) => localY01 >= 0.5f ? SlabType.Top : SlabType.Bottom;

        public static SlabType Merge(SlabType a, SlabType b)
        {
            if (a == SlabType.Double || b == SlabType.Double) return SlabType.Double;
            if (a == SlabType.Top && b == SlabType.Bottom) return SlabType.Double;
            if (a == SlabType.Bottom && b == SlabType.Top) return SlabType.Double;
            return a; // rien à fusionner
        }
    }
}