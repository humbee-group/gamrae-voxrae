// Assets/Scripts/Voxel/Domain/Block/GrassBlock.cs
namespace Voxel.Domain.Blocks
{
    /// Bloc opaque type grass_block (top/bottom/side gérés par le modèle JSON).
    public sealed class GrassBlock : Block
    {
        public override RenderType RenderType => RenderType.Opaque;
        public override byte EncodeState(StateProps props) => 0;
        public override StateProps DecodeState(byte state) => default;
    }
}