// Assets/Scripts/Voxel/Domain/Block/StairsBlock.cs

namespace Voxel.Domain.Blocks
{
    public sealed class StairsBlock : Block
    {
        // Bits: [2:0]=facing, [3]=half, [6:4]=shape
        public override RenderType RenderType => RenderType.Cutout;
        public override bool IsOpaque(byte state)    => false;
        public override bool IsOccluding(byte state) => false;

        public override byte EncodeState(StateProps p)
        {
            byte st = 0;
            if (p.facing.HasValue) st |= (byte)((int)p.facing.Value & 0b111);
            if (p.half == Half.Top) st |= 0b1000;
            if (p.shape.HasValue) st |= (byte)(((int)p.shape.Value & 0b111) << 4);
            return st;
        }

        public override StateProps DecodeState(byte state)
        {
            var facing = (Direction)(state & 0b111);
            var half = ((state & 0b1000) != 0) ? Half.Top : Half.Bottom;
            var shape = (StairsShape)((state >> 4) & 0b111);
            return new StateProps { facing = facing, half = half, shape = shape };
        }
    }
}