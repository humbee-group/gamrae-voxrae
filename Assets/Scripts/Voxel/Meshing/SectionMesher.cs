// Assets/Scripts/Voxel/Meshing/SectionMesher.cs
// Utilise le registre statique (supprime les refs à VoxelBlockRegistry)

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.Blocks;

namespace Voxel.Meshing
{
    public interface ISectionReader
    {
        (ushort id, byte state) Get(int lx, int ly, int lz);
        bool InBounds(int lx, int ly, int lz);
    }

    public interface IUVProvider
    {
        Rect GetUV(ushort id, byte state, int faceIndex);
        bool UseUVLock(ushort id, byte state);
    }

    public static class SectionMesher
    {
        public static Mesh BuildMesh(ISectionReader data, IUVProvider uvProvider)
        {
            var v = ListPool<Vector3>.Get();
            var n = ListPool<Vector3>.Get();
            var uv0 = ListPool<Vector2>.Get();
            var idxOpaque = ListPool<int>.Get();
            var idxCutout = ListPool<int>.Get();
            var idxTransp = ListPool<int>.Get();

            for (int y = 0; y < 16; y++)
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
            {
                var (id, st) = data.Get(x, y, z);
                if (id == 0) continue;

                var block = Voxel.Domain.Registry.BlockRegistry.Get(id);
                var rt = block.RenderType;
                bool uvlock = uvProvider.UseUVLock(id, st);

                for (int f = 0; f < 6; f++)
                {
                    int nx = x, ny = y, nz = z;
                    switch (f) { case 0: nz--; break; case 1: nz++; break; case 2: nx--; break; case 3: nx++; break; case 4: ny++; break; case 5: ny--; break; }

                    bool neighborCovers = false;
                    if (data.InBounds(nx, ny, nz))
                    {
                        var (nid, nst) = data.Get(nx, ny, nz);
                        if (nid != 0)
                        {
                            var nb = Voxel.Domain.Registry.BlockRegistry.Get(nid);
                            neighborCovers = nb.RenderType == RenderType.Opaque && nb.IsOccluding(nst);
                        }
                    }

                    if (!neighborCovers)
                    {
                        Rect uv = uvProvider.GetUV(id, st, f);
                        ModelBakery.EmitQuad(v, n, uv0, idxOpaque, idxCutout, idxTransp, rt, new Vector3(x, y, z), f, uv, uvlock);
                    }
                }
            }

            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv0);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(idxOpaque, 0, false);
            mesh.SetTriangles(idxCutout, 1, false);
            mesh.SetTriangles(idxTransp, 2, false);
            mesh.RecalculateBounds();

            ListPool<Vector3>.Release(v);
            ListPool<Vector3>.Release(n);
            ListPool<Vector2>.Release(uv0);
            ListPool<int>.Release(idxOpaque);
            ListPool<int>.Release(idxCutout);
            ListPool<int>.Release(idxTransp);

            return mesh;
        }
    }

    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new();
        public static List<T> Get() { if (Pool.Count > 0){ var l=Pool.Pop(); l.Clear(); return l; } return new List<T>(1024); }
        public static void Release(List<T> list){ list.Clear(); Pool.Push(list); }
    }
}