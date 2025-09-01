// Assets/Scripts/Voxel/Runtime/Streaming/NoiseTerrainGenerator.cs
// Générateur terrain type Minecraft (heightmap), impl ISectionSource. Disk-first géré dans ChunkStreamer.
using UnityEngine;
using Voxel.Runtime.Streaming;
using Voxel.Domain.Registry;

namespace Voxel.Runtime.Generation
{
    public sealed class NoiseTerrainGenerator : ISectionSource
    {
        readonly int seed;
        readonly float scale;
        readonly int baseHeight;
        readonly int amplitude;
        readonly ushort stoneId;
        readonly ushort logId; // facultatif pour tests

        public NoiseTerrainGenerator(int seed, float scale, int baseHeight, int amplitude, ushort stoneId, ushort logId = 0)
        {
            this.seed = seed;
            this.scale = Mathf.Max(0.0001f, scale);
            this.baseHeight = baseHeight;
            this.amplitude = Mathf.Max(0, amplitude);
            this.stoneId = stoneId;
            this.logId = logId;
        }

        public (ushort[] ids, byte[] states, bool fromDisk) LoadOrGenerate(int sx, int sy, int sz)
        {
            var ids = new ushort[16 * 16 * 16];
            var st  = new byte  [16 * 16 * 16];

            int worldY0 = sy * 16;
            for (int lz = 0; lz < 16; lz++)
            {
                int wz = sz * 16 + lz;
                for (int lx = 0; lx < 16; lx++)
                {
                    int wx = sx * 16 + lx;

                    int h = ComputeHeight(wx, wz);
                    // Remplissage colonne locale
                    for (int ly = 0; ly < 16; ly++)
                    {
                        int wy = worldY0 + ly;
                        if (wy <= h)
                        {
                            int i = ((ly * 16) + lz) * 16 + lx;
                            ids[i] = stoneId;
                            st[i] = 0;
                        }
                    }
                }
            }

            return (ids, st, false);
        }

        int ComputeHeight(int wx, int wz)
        {
            // FBM simple Perlin
            float x = (wx + seed * 13.123f) * scale;
            float z = (wz + seed * 17.531f) * scale;

            float n =
                Mathf.PerlinNoise(x, z) * 0.55f +
                Mathf.PerlinNoise(x * 2.03f, z * 2.03f) * 0.27f +
                Mathf.PerlinNoise(x * 4.07f, z * 4.07f) * 0.13f +
                Mathf.PerlinNoise(x * 8.19f, z * 8.19f) * 0.05f;

            int h = baseHeight + Mathf.FloorToInt(n * amplitude);
            return h;
        }
    }
}