// Assets/Scripts/Voxel/Domain/Registry/BlockRegister.cs
// Déclaration centralisée des blocs (comme Minecraft)

using Voxel.Domain.Blocks;

namespace Voxel.Domain.Registry
{
public static class BlockRegister
{
    static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        BlockRegistry.Register("minecraft:stone", new OpaqueCubeBlock());
        BlockRegistry.Register("minecraft:oak_log", new LogBlock());
        BlockRegistry.Register("minecraft:oak_slab", new SlabBlock());
        BlockRegistry.Register("minecraft:oak_stairs", new StairsBlock());
    }
}
}