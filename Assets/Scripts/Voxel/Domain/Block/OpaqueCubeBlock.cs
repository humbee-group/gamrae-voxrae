// Assets/Scripts/Voxel/Domain/Block/OpaqueCubeBlock.cs

namespace Voxel.Domain.Blocks
{
    public sealed class OpaqueCubeBlock : Block
    {
        public override RenderType RenderType => RenderType.Opaque;
        public override byte EncodeState(StateProps props) => 0;
        public override StateProps DecodeState(byte state) => default;
    }
}