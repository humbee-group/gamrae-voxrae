// Assets/Scripts/Voxel/IO/LevelReaderRaw.cs
// Wrapper simple pour la v4 (ids, states, sky, block). Garde l’API existante.

namespace Voxel.IO
{
    public static class LevelReaderRaw
    {
        public static bool TryReadSection(string path, out ushort[] ids, out byte[] states)
        {
            return LevelStorage.TryLoadSection(path, out ids, out states, out _, out _);
        }

        public static bool TryReadSectionV4(string path, out ushort[] ids, out byte[] states, out byte[] sky, out byte[] block)
        {
            return LevelStorage.TryLoadSection(path, out ids, out states, out sky, out block);
        }
    }
}