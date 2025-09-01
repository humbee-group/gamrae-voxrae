// Assets/Scripts/Voxel/Domain/Block/SlabBlock.cs

namespace Voxel.Domain.Blocks
{
    public sealed class SlabBlock : Block
    {
        public override RenderType RenderType => RenderType.Cutout;

        public override bool IsOpaque(byte state)    => (SlabType)(state & 0b11) == SlabType.Double;
        public override bool IsOccluding(byte state) => (SlabType)(state & 0b11) == SlabType.Double;

        public override byte EncodeState(StateProps props)
        {
            var t = props.slab ?? (props.half == Half.Top ? SlabType.Top : SlabType.Bottom);
            return (byte)((int)t & 0b11);
        }

        public override StateProps DecodeState(byte state)
        {
            var t = (SlabType)(state & 0b11);
            return new StateProps { slab = t, half = t == SlabType.Top ? Half.Top : t == SlabType.Bottom ? Half.Bottom : (Half?)null };
        }
    }
}