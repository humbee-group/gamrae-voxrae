// Assets/Scripts/Voxel/Domain/Block/ColumnBlock.cs
namespace Voxel.Domain.Blocks
{
    /// Bloc type cube_column (ex: troncs, piliers). Orientation par Axis.
    public sealed class ColumnBlock : Block
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