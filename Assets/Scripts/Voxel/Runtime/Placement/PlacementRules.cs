// Assets/Scripts/Voxel/Runtime/Placement/PlacementRules.cs
// Résolution d'état à la pose (façon MC) pour quelques blocs courants
using UnityEngine;
using Voxel.Domain.Blocks;

namespace Voxel.Runtime.Placement
{
    public static class PlacementRules
    {
        public static byte Compute(ushort blockId, BlockPlaceContext ctx)
        {
            var b = Voxel.Domain.Registry.BlockRegistry.Get(blockId);

            // Logs: axis via face
            if (b is ColumnBlock)
            {
                var axis = ctx.Face switch
                {
                    Direction.Up or Direction.Down => Axis.Y,
                    Direction.East or Direction.West => Axis.X,
                    _ => Axis.Z
                };
                return b.EncodeState(new StateProps{ axis=axis });
            }

            // Slabs: half par hit.y, fusion gérée côté monde plus tard si besoin
            if (b is SlabBlock)
            {
                var half = ctx.Hit.y >= 0.5f ? Half.Top : Half.Bottom;
                return b.EncodeState(new StateProps{ half=half, slab = (half==Half.Top? SlabType.Top: SlabType.Bottom) });
            }

            // Stairs: facing depuis joueur, half via hit
            if (b is StairsBlock)
            {
                var facing = ctx.PlayerFacing is Direction.Up or Direction.Down ? Direction.North : ctx.PlayerFacing;
                var half   = ctx.Hit.y >= 0.5f ? Half.Top : Half.Bottom;
                return b.EncodeState(new StateProps{ facing=facing, half=half, shape=StairsShape.Straight });
            }

            // Par défaut
            return b.EncodeState(default);
        }
    }
}