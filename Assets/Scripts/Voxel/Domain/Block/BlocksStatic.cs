// Assets/Scripts/Voxel/Domain/Block/BlocksStatic.cs
// Simplifié: ponts utilitaires sur le registre statique

using Voxel.Domain.Registry;

namespace Voxel.Domain.Blocks
{
    public static class Blocks
    {
        public static bool IsOpaque(ushort id, byte state)    => BlockRegistry.Get(id).IsOpaque(state);
        public static bool IsOccluding(ushort id, byte state) => BlockRegistry.Get(id).IsOccluding(state);
    }
}