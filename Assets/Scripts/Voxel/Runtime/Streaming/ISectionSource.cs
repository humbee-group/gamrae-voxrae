// Assets/Scripts/Voxel/Runtime/Streaming/ISectionSource.cs
namespace Voxel.Runtime.Streaming
{
    public interface ISectionSource
    {
        // Doit fournir ids/states (longueur 4096) pour (sx,sy,sz). fromDisk true si lu depuis disque.
        (ushort[] ids, byte[] states, bool fromDisk) LoadOrGenerate(int sx,int sy,int sz);
    }
}