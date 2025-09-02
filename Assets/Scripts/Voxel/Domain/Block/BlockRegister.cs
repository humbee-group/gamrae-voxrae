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


            BlockRegistry.Register("stone", new OpaqueCubeBlock());
            BlockRegistry.Register("dirt", new OpaqueCubeBlock());
            BlockRegistry.Register("grass", new GrassBlock());
            BlockRegistry.Register("oak_log", new ColumnBlock());
            BlockRegistry.Register("oak_slab", new SlabBlock());
            BlockRegistry.Register("oak_stairs", new StairsBlock());
            BlockRegistry.Register("torch", new TorchBlock());
    }
}
}