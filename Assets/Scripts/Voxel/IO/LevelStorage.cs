// Assets/Scripts/Voxel/IO/LevelStorage.cs
// v4: ids(4096*2) + states(4096) + sky(4096) + block(4096) + CRC

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Voxel.IO
{
    public static class LevelStorage
    {
        public const int FormatVersion = 4;
        public const int ChunkSize = 16;
        public const int SectionHeight = 16;

        private struct Header
        {
            public int magic;         // 'VXSC'
            public int version;       // 4
            public int chunkSize;     // 16
            public int sectionHeight; // 16
            public int payloadBytes;  // bytes après header, hors CRC
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string TempPath(string path) => path + ".tmp";

        public static void SaveSection(string path, ushort[] ids4096, byte[] states4096, byte[] sky4096, byte[] blk4096)
        {
            if (ids4096?.Length!=4096 || states4096?.Length!=4096 || sky4096?.Length!=4096 || blk4096?.Length!=4096)
                throw new ArgumentException("Section arrays must be length 4096.");

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var tmp = TempPath(path);

            var header = new Header
            {
                magic = 0x56585343, // 'VXSC'
                version = FormatVersion,
                chunkSize = ChunkSize,
                sectionHeight = SectionHeight,
                payloadBytes = 4096*2 + 4096 + 4096 + 4096 // ids(2B) + st + sky + blk
            };

            using var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None);
            using var bw = new BinaryWriter(fs);

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
            bw.Write(sky4096);
            bw.Write(blk4096);

            // CRC
            uint crc = Crc32.Compute(idsBytes, 0, idsBytes.Length);
            crc = Crc32.Compute(states4096, 0, states4096.Length, crc);
            crc = Crc32.Compute(sky4096,    0, sky4096.Length,    crc);
            crc = Crc32.Compute(blk4096,    0, blk4096.Length,    crc);
            bw.Write(crc);
            bw.Flush(); fs.Flush(true);

            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        public static bool TryLoadSection(string path, out ushort[] ids4096, out byte[] states4096, out byte[] sky4096, out byte[] blk4096)
        {
            ids4096=null; states4096=null; sky4096=null; blk4096=null;
            if (!File.Exists(path)) return false;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);
            if (fs.Length < 5*sizeof(int) + 4096*3 + sizeof(uint)) return false;

            var magic = br.ReadInt32();
            var version = br.ReadInt32();
            var csize = br.ReadInt32();
            var sheight = br.ReadInt32();
            var payloadBytes = br.ReadInt32();
            if (magic!=0x56585343 || csize!=ChunkSize || sheight!=SectionHeight) return false;

            // Lecture payloads
            var idsBytes = br.ReadBytes(4096*2);
            var st = br.ReadBytes(4096);

            if (version >= 4)
            {
                var sky = br.ReadBytes(4096);
                var blk = br.ReadBytes(4096);
                var crcRead = br.ReadUInt32();
                uint crc = Crc32.Compute(idsBytes,0,idsBytes.Length);
                crc = Crc32.Compute(st,0,st.Length,crc);
                crc = Crc32.Compute(sky,0,sky.Length,crc);
                crc = Crc32.Compute(blk,0,blk.Length,crc);
                if (crc!=crcRead) return false;

                var ids = new ushort[4096];
                Buffer.BlockCopy(idsBytes,0,ids,0,idsBytes.Length);
                ids4096 = ids; states4096 = st; sky4096 = sky; blk4096 = blk; return true;
            }
            else
            {
                // v3 rétro-compat: pas de lumière → init à 0
                var crcRead = br.ReadUInt32();
                uint crc = Crc32.Compute(idsBytes,0,idsBytes.Length);
                crc = Crc32.Compute(st,0,st.Length,crc);
                if (crc!=crcRead) return false;

                var ids = new ushort[4096];
                Buffer.BlockCopy(idsBytes,0,ids,0,idsBytes.Length);
                ids4096 = ids; states4096 = st; sky4096 = new byte[4096]; blk4096 = new byte[4096]; return true;
            }
        }

        public static string SectionPath(string root, int sx, int sy, int sz)
        {
            var dir = Path.Combine(root, "chunks", $"{sx}_{sz}");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{sx}_{sy}_{sz}.vxsc");
        }

        // CRC identique à avant
        internal static class Crc32
        {
            private static readonly uint[] Table = InitTable();
            private static uint[] InitTable()
            {
                const uint poly = 0xEDB88320u;
                var t = new uint[256];
                for (uint i=0;i<256;i++)
                {
                    uint c=i;
                    for (int k=0;k<8;k++)
                        c = ((c & 1)!=0) ? (poly ^ (c>>1)) : (c>>1);
                    t[i]=c;
                }
                return t;
            }
            public static uint Compute(byte[] buf,int off,int len,uint crc=0xFFFFFFFFu)
            {
                var c = crc;
                for (int i=off;i<off+len;i++) c = Table[(c ^ buf[i]) & 0xFF] ^ (c >> 8);
                return c;
            }
        }
    }
}