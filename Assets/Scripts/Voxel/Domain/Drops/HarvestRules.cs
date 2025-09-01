// Assets/Scripts/Voxel/Domain/Drops/HarvestRules.cs
// Ne jamais supprimer les commentaires

namespace Voxel.Domain.Drops
{
    public enum ToolTier { None=0, Wood=1, Stone=2, Iron=3, Diamond=4, Netherite=5 }

    public static class HarvestRules
    {
        public static bool CanHarvest(int requiredTier, ToolTier toolTier)
        {
            return (int)toolTier >= requiredTier;
        }
    }
}