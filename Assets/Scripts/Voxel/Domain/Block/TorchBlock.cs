// Assets/Scripts/Voxel/Domain/Block/TorchBlock.cs
// Bloc lumière simple (cube), émission 14, non-opaque, non-occludant.

namespace Voxel.Domain.Blocks
{
    public sealed class TorchBlock : Block
    {
        // Visuellement: Cutout pour accepter l’alpha si la texture a un canal A
        public override RenderType RenderType => RenderType.Cutout;

        public override bool IsOpaque(byte state)    => false;
        public override bool IsOccluding(byte state) => false;

        // Émet 14 (comme MC)
        public override byte LightEmission(byte state) => 14;

        // Aucune atténuation (laisse passer la lumière)
        public override byte LightAttenuation(byte state) => 0;

        // Pas d’états pour cette v1
        public override byte EncodeState(StateProps props) => 0;
        public override StateProps DecodeState(byte state) => default;
    }
}