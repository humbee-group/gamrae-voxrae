// Assets/Scripts/Voxel/Runtime/Streaming/NoiseTerrainGenerator.cs
// Terrain MC-like : stone fond, couches de dirt, herbe en surface. Interface ISectionSource inchangée.

using UnityEngine;
using Voxel.Runtime.Streaming;

namespace Voxel.Runtime.Generation
{
    public sealed class NoiseTerrainGenerator : ISectionSource
    {
        readonly int seed;
        readonly float scale;
        readonly int baseHeight;
        readonly int amplitude;

        readonly ushort stoneId;
        readonly ushort dirtId;
        readonly ushort grassId;

        readonly int dirtThickness;
        readonly bool bedrock;

        public NoiseTerrainGenerator(
            int seed, float scale, int baseHeight, int amplitude,
            ushort stoneId, ushort dirtId, ushort grassId,
            int dirtThickness = 3, bool bedrock = true)
        {
            this.seed = seed;
            this.scale = Mathf.Max(0.0001f, scale);
            this.baseHeight = baseHeight;
            this.amplitude = Mathf.Max(0, amplitude);
            this.stoneId = stoneId;
            this.dirtId = dirtId;
            this.grassId = grassId;
            this.dirtThickness = Mathf.Max(1, dirtThickness);
            this.bedrock = bedrock;
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

                    int h = ComputeHeight(wx, wz); // hauteur surface incluse

                    // bedrock (optionnel) : couche y=0 irrégulière
                    if (bedrock && sy == 0)
                    {
                        for (int ly = 0; ly < 2; ly++)
                        {
                            int wy = ly;
                            if (RandomBedrock(wx, wy, wz)) ids[((ly*16)+lz)*16+lx] = stoneId;
                        }
                    }

                    for (int ly = 0; ly < 16; ly++)
                    {
                        int wy = worldY0 + ly;
                        if (wy > h) continue;

                        int i = ((ly * 16) + lz) * 16 + lx;

                        // surface = herbe
                        if (wy == h)
                        {
                            ids[i] = grassId; st[i] = 0;
                        }
                        // sous-sol immédiat = dirt (épaisseur configurable)
                        else if (wy >= h - dirtThickness + 1)
                        {
                            ids[i] = dirtId; st[i] = 0;
                        }
                        // profond = stone
                        else
                        {
                            ids[i] = stoneId; st[i] = 0;
                        }
                    }
                }
            }

            return (ids, st, false);
        }

        int ComputeHeight(int wx, int wz)
        {
            float x = (wx + seed * 13.123f) * scale;
            float z = (wz + seed * 17.531f) * scale;

            float n =
                Mathf.PerlinNoise(x, z) * 0.55f +
                Mathf.PerlinNoise(x * 2.03f, z * 2.03f) * 0.27f +
                Mathf.PerlinNoise(x * 4.07f, z * 4.07f) * 0.13f +
                Mathf.PerlinNoise(x * 8.19f, z * 8.19f) * 0.05f;

            return baseHeight + Mathf.FloorToInt(n * amplitude);
        }

static bool RandomBedrock(int wx, int wy, int wz)
{
    // petite irrégularité : plus haut rare
    long h = (long)wx * 734287L + (long)wz * 912271L + (long)wy * 137L;
    int hash = (int)(h ^ 0x9E3779B9);
    int r = (hash ^ (hash >> 16)) & 255;
    return wy == 0 || (wy == 1 && r < 64);
}
    }
}