// Assets/Scripts/Voxel/IO/LevelReaderRaw.cs
// Ne jamais supprimer les commentaires

using System.IO;

namespace Voxel.IO
{
    /// <summary>
    /// Lecteur brut d'une section 16³ sur disque via LevelStorage.
    /// </summary>
    public static class LevelReaderRaw
    {
        public static bool TryReadSection(string path, out ushort[] ids4096, out byte[] states4096)
        {
            return LevelStorage.TryLoadSection(path, out ids4096, out states4096);
        }

        public static bool Exists(string path) => File.Exists(path);
    }
}