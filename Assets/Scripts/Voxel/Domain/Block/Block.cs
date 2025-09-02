// Assets/Scripts/Voxel/Domain/Block/Block.cs
// Base bloc (non ScriptableObject) + AirBlock

namespace Voxel.Domain.Blocks
{
    public abstract class Block
    {
        public ushort Id { get; internal set; }
        public string Name { get; internal set; }

        public abstract RenderType RenderType { get; }

        public virtual bool IsOpaque(byte state)    => true;
        public virtual bool IsOccluding(byte state) => true;

        /// Niveau de lumière émise (0..15). Ex: torche=14. Par défaut 0.
        public virtual byte LightEmission(byte state) => 0;

        /// Atténuation de lumière à travers ce bloc (0 ou 1 pour MC-like). Par défaut 1 si opaque, sinon 0.
        public virtual byte LightAttenuation(byte state) => IsOpaque(state) ? (byte)1 : (byte)0;

        public abstract byte EncodeState(StateProps props);
        public abstract StateProps DecodeState(byte state);
    }

    public sealed class AirBlock : Block
    {
        public override RenderType RenderType => RenderType.Cutout;
        public override bool IsOpaque(byte state)    => false;
        public override bool IsOccluding(byte state) => false;
        public override byte EncodeState(StateProps props) => 0;
        public override StateProps DecodeState(byte state) => default;
    }
}