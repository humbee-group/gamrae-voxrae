// Assets/Scripts/Voxel/Domain/Block/LogBlock.cs

namespace Voxel.Domain.Blocks
{
    public sealed class LogBlock : Block
    {
        public override RenderType RenderType => RenderType.Opaque;

        public override byte EncodeState(StateProps props)
        {
            var ax = props.axis.HasValue ? props.axis.Value : Axis.Y;
            return (byte)((int)ax & 0b11);
        }

        public override StateProps DecodeState(byte state)
        {
            return new StateProps { axis = (Axis)(state & 0b11) };
        }
    }
}