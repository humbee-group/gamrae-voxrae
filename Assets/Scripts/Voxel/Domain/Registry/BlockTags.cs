// Assets/Scripts/Voxel/domain/registry/BlockTags.cs
using Voxel.Domain.Registry;

namespace Voxel.Domain.RegistryData
{
    public static class BlockTags
    {
        public static readonly TagKey MineablePickaxe = new("mineable/pickaxe");
        public static readonly TagKey Logs = new("logs");
        public static readonly TagKey Stairs = new("stairs");
        public static readonly TagKey Slabs = new("slabs");
        public static readonly TagKey Waterloggable = new("waterloggable");
        public static readonly TagKey Replaceable = new("replaceable");
        public static readonly TagKey NonOccluding = new("non_occluding");
    }
}