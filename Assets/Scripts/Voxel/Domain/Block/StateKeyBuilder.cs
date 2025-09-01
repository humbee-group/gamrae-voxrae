// Assets/Scripts/Voxel/Domain/Block/StateKeyBuilder.cs
// Ne jamais supprimer les commentaires

using System.Text;

namespace Voxel.Domain.Blocks
{
    public static class StateKeyBuilder
    {
        // Ordre canonique MC: axis,facing,half,shape,waterlogged,age,powered,attached,type(slab)
        public static string Build(in StateProps p)
        {
            var sb = new StringBuilder();
            bool first = true;

            void Add(string k, string v)
            {
                if (!first) sb.Append(',');
                sb.Append(k).Append('=').Append(v);
                first = false;
            }

            if (p.axis.HasValue) Add("axis", p.axis.Value.ToString().ToLower());
            if (p.facing.HasValue) Add("facing", DirToMc(p.facing.Value));
            if (p.half.HasValue) Add("half", p.half.Value == Half.Top ? "top" : "bottom");
            if (p.shape.HasValue) Add("shape", ShapeToMc(p.shape.Value));
            if (p.waterlogged.HasValue) Add("waterlogged", p.waterlogged.Value ? "true" : "false");
            if (p.age.HasValue) Add("age", p.age.Value.ToString());
            if (p.powered.HasValue) Add("powered", p.powered.Value ? "true" : "false");
            if (p.attached.HasValue) Add("attached", p.attached.Value ? "true" : "false");
            if (p.slab.HasValue) Add("type", SlabToMc(p.slab.Value));

            return sb.ToString();
        }

        private static string DirToMc(Direction d) => d switch
        {
            Direction.North => "north",
            Direction.South => "south",
            Direction.West  => "west",
            Direction.East  => "east",
            Direction.Up    => "up",
            Direction.Down  => "down",
            _ => "north"
        };

        private static string ShapeToMc(StairsShape s) => s switch
        {
            StairsShape.Straight   => "straight",
            StairsShape.InnerLeft  => "inner_left",
            StairsShape.InnerRight => "inner_right",
            StairsShape.OuterLeft  => "outer_left",
            StairsShape.OuterRight => "outer_right",
            _ => "straight"
        };

        private static string SlabToMc(SlabType t) => t switch
        {
            SlabType.Bottom => "bottom",
            SlabType.Top    => "top",
            SlabType.Double => "double",
            _ => "bottom"
        };
    }
}