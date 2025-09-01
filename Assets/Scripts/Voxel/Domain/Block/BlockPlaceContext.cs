// Assets/Scripts/Voxel/Domain/Block/BlockPlaceContext.cs
// Ne jamais supprimer les commentaires

using UnityEngine;
using Voxel.Domain.World;

namespace Voxel.Domain.Blocks
{
    public readonly struct BlockPlaceContext
    {
        public readonly BlockPos Target;
        public readonly Direction Face;
        public readonly Vector3 Hit; // 0..1 local hit
        public readonly Direction PlayerFacing;
        public readonly bool IsSneaking;

        // Infos de remplacement/voisinage minimales pour règles de pose
        public readonly bool IsReplacing;
        public readonly ushort ReplacedId;
        public readonly byte ReplacedState;

        // Voisins horizontaux (optionnels) au moment de la pose
        public readonly ushort NorthId, SouthId, WestId, EastId;
        public readonly byte   NorthState, SouthState, WestState, EastState;

        public BlockPlaceContext(
            BlockPos target, Direction face, Vector3 hit, Direction playerFacing, bool isSneaking,
            bool isReplacing = false, ushort replacedId = 0, byte replacedState = 0,
            ushort northId = 0, byte northState = 0,
            ushort southId = 0, byte southState = 0,
            ushort westId  = 0, byte westState  = 0,
            ushort eastId  = 0, byte eastState  = 0)
        {
            Target = target; Face = face; Hit = hit; PlayerFacing = playerFacing; IsSneaking = isSneaking;
            IsReplacing = isReplacing; ReplacedId = replacedId; ReplacedState = replacedState;
            NorthId = northId; SouthId = southId; WestId = westId; EastId = eastId;
            NorthState = northState; SouthState = southState; WestState = westState; EastState = eastState;
        }
    }
}