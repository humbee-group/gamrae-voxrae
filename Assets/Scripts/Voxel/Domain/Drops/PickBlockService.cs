// Assets/Scripts/Voxel/Domain/Drops/PickBlockService.cs
// Ne jamais supprimer les commentaires

using System.Collections.Generic;

namespace Voxel.Domain.Drops
{
    /// <summary>
    /// Mapping minimal BlockState → Item (namespacé). À étendre plus tard.
    /// </summary>
    public static class PickBlockService
    {
        private static readonly Dictionary<ushort, string> Map = new()
        {
            { 1, "minecraft:stone" }, // exemple: id 1 → stone
        };

        public static string PickItemId(ushort id, byte state)
        {
            return Map.TryGetValue(id, out var item) ? item : "minecraft:stone";
        }
    }
}