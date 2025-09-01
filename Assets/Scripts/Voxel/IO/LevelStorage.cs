// Assets/Scripts/Voxel/IO/LevelStorage.cs
// Ne jamais supprimer les commentaires

using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Voxel.IO
{
    /// <summary>
    /// Stockage binaire d'une section 16³ : ids ushort[4096], states byte[4096]
    /// Header versionné + CRC32 en footer. Écriture atomique .tmp → Replace.
    /// </summary>
    public static class LevelStorage
    {
        public const int FormatVersion = 3;
        public const int ChunkSize = 16;
        public const int SectionHeight = 16;

        private struct Header
        {
            public int magic;         // 'VXSC'
            public int version;       // 3
            public int chunkSize;     // 16
            public int sectionHeight; // 16
            public int payloadBytes;  // bytes après header, hors CRC
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string TempPath(string path) => path + ".tmp";

        public static void SaveSection(string path, ushort[] ids4096, byte[] states4096)
        {
            if (ids4096 == null || states4096 == null) throw new ArgumentNullException(nameof(ids4096));
            if (ids4096.Length != 4096 || states4096.Length != 4096) throw new ArgumentException("Section arrays must be length 4096.");

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var tmp = TempPath(path);

            var header = new Header
            {
                magic = 0x56585343, // 'VXSC'
                version = FormatVersion,
                chunkSize = ChunkSize,
                sectionHeight = SectionHeight,
                payloadBytes = 4096 * sizeof(ushort) + 4096 * sizeof(byte)
            };

            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs))
            {
                // Header
                bw.Write(header.magic);
                bw.Write(header.version);
                bw.Write(header.chunkSize);
                bw.Write(header.sectionHeight);
                bw.Write(header.payloadBytes);

                // Payload
                var idsBytes = new byte[4096 * sizeof(ushort)];
                Buffer.BlockCopy(ids4096, 0, idsBytes, 0, idsBytes.Length);
                bw.Write(idsBytes);
                bw.Write(states4096);

                // CRC32 sur payload seulement
                uint crc = Crc32.Compute(idsBytes, 0, idsBytes.Length);
                crc = Crc32.Compute(states4096, 0, states4096.Length, crc);
                bw.Write(crc);
                bw.Flush();
                fs.Flush(true);
            }

            // Remplacement atomique
            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);
        }

        public static bool TryLoadSection(string path, out ushort[] ids4096, out byte[] states4096)
        {
            ids4096 = null; states4096 = null;
            if (!File.Exists(path)) return false;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs))
            {
                if (fs.Length < 5 * sizeof(int) + 4096 * 3 + sizeof(uint)) return false;

                var magic = br.ReadInt32();
                var version = br.ReadInt32();
                var csize = br.ReadInt32();
                var sheight = br.ReadInt32();
                var payloadBytes = br.ReadInt32();
                if (magic != 0x56585343 || version != FormatVersion || csize != ChunkSize || sheight != SectionHeight)
                    return false;

                if (payloadBytes != 4096 * sizeof(ushort) + 4096 * sizeof(byte)) return false;

                var idsBytes = br.ReadBytes(4096 * sizeof(ushort));
                var states = br.ReadBytes(4096);
                var crcRead = br.ReadUInt32();

                uint crc = Crc32.Compute(idsBytes, 0, idsBytes.Length);
                crc = Crc32.Compute(states, 0, states.Length, crc);
                if (crc != crcRead) return false;

                var ids = new ushort[4096];
                Buffer.BlockCopy(idsBytes, 0, ids, 0, idsBytes.Length);

                ids4096 = ids;
                states4096 = states;
                return true;
            }
        }

        /// <summary> Chemin de section: root/chunks/sx_sy_sz.vxsc </summary>
        public static string SectionPath(string root, int sx, int sy, int sz)
        {
            var dir = Path.Combine(root, "chunks", $"{sx}_{sz}");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{sx}_{sy}_{sz}.vxsc");
        }
    }

    /// <summary> CRC32 (IEEE) minimal, sans table externe. </summary>
    internal static class Crc32
    {
        private static readonly uint[] Table = InitTable();

        private static uint[] InitTable()
        {
            const uint poly = 0xEDB88320u;
            var t = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                    c = ((c & 1) != 0) ? (poly ^ (c >> 1)) : (c >> 1);
                t[i] = c;
            }
            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Compute(byte[] buf, int offset, int count, uint crc = 0xFFFFFFFFu)
        {
            var c = crc;
            int end = offset + count;
            for (int i = offset; i < end; i++)
                c = Table[(c ^ buf[i]) & 0xFF] ^ (c >> 8);
            return c;
        }
    }
}