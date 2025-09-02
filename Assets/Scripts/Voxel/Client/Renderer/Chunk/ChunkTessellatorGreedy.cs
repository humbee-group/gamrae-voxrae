// Assets/Scripts/Voxel/Client/Renderer/Chunk/ChunkTessellatorGreedy.cs
// Greedy corrigé. Référence IUVProvider via Voxel.Meshing (défini dans UvProviderFromPack.cs).

using System.Collections.Generic;
using System.Buffers;
using UnityEngine;
using UnityEngine.Rendering;
using Voxel.Meshing;
using Voxel.Domain.Blocks;

namespace Voxel.Client.Renderer.Chunk
{
    public static class ChunkTessellatorGreedy
    {
        private const int SZ = 16;

        private struct Cell
        {
            public ushort id;
            public byte st;
            public RenderType rt;
            public Rect uv;
            public bool visible;
            public bool Same(in Cell o)
            {
                return visible && o.visible && id==o.id && st==o.st && rt==o.rt
                    && Mathf.Approximately(uv.xMin,o.uv.xMin) && Mathf.Approximately(uv.xMax,o.uv.xMax)
                    && Mathf.Approximately(uv.yMin,o.uv.yMin) && Mathf.Approximately(uv.yMax,o.uv.yMax);
            }
        }

        private static readonly Vector3[] FACE_NORMAL =
        {
            new(0,0,-1), // 0 North (Z-)
            new(0,0, 1), // 1 South (Z+)
            new(-1,0,0), // 2 West  (X-)
            new( 1,0,0), // 3 East  (X+)
            new(0, 1,0), // 4 Up    (Y+)
            new(0,-1,0), // 5 Down  (Y-)
        };

        public static Mesh BuildMesh(ISectionNeighborhood nb, IUVProvider uvp)
        {
            var v  = ListPool<Vector3>.Get();
            var n  = ListPool<Vector3>.Get();
            var uv = ListPool<Vector2>.Get();
            var i0 = ListPool<int>.Get();
            var i1 = ListPool<int>.Get();
            var i2 = ListPool<int>.Get();

            GreedyFaceZ(nb, uvp, v, n, uv, i0, i1, i2, faceIndex:0, zIsMin:true);  // North Z-
            GreedyFaceZ(nb, uvp, v, n, uv, i0, i1, i2, faceIndex:1, zIsMin:false); // South Z+
            GreedyFaceX(nb, uvp, v, n, uv, i0, i1, i2, faceIndex:2, xIsMin:true);  // West  X-
            GreedyFaceX(nb, uvp, v, n, uv, i0, i1, i2, faceIndex:3, xIsMin:false); // East  X+
            GreedyFaceY(nb, uvp, v, n, uv, i0, i1, i2, faceIndex:4, yIsMax:true);  // Up    Y+
            GreedyFaceY(nb, uvp, v, n, uv, i0, i1, i2, faceIndex:5, yIsMax:false); // Down  Y-

            var mesh = new Mesh
            {
                indexFormat = v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                bounds = new Bounds(new Vector3(8,8,8), new Vector3(16,16,16))
            };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(i0, 0, false);
            mesh.SetTriangles(i1, 1, false);
            mesh.SetTriangles(i2, 2, false);
            mesh.UploadMeshData(false);

            ListPool<Vector3>.Release(v); ListPool<Vector3>.Release(n);
            ListPool<Vector2>.Release(uv);
            ListPool<int>.Release(i0); ListPool<int>.Release(i1); ListPool<int>.Release(i2);
            return mesh;
        }

        // ===== Z faces =====
        private static void GreedyFaceZ(ISectionNeighborhood nb, IUVProvider uvp,
            List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> i0, List<int> i1, List<int> i2,
            int faceIndex, bool zIsMin)
        {
            var grid = ArrayPool<Cell>.Shared.Rent(SZ * SZ);
            var used = ArrayPool<bool>.Shared.Rent(SZ * SZ);

            for (int z = 0; z < SZ; z++)
            {
                int zFace = z;
                for (int y=0; y<SZ; y++)
                for (int x=0; x<SZ; x++)
                {
                    var (id, st) = nb.GetWithNeighbors(x,y,zFace);
                    var c = new Cell { id=id, st=st, visible=false };
                    if (id!=0)
                    {
                        var b = Voxel.Domain.Registry.BlockRegistry.Get(id);
                        c.rt = b.RenderType;

                        int nz = zFace + (zIsMin ? -1 : +1);
                        var (nid, nst) = nb.GetWithNeighbors(x,y,nz);
                        bool covered = false;
                        if (nid!=0)
                        {
                            var nbk = Voxel.Domain.Registry.BlockRegistry.Get(nid);
                            covered = nbk.RenderType==RenderType.Opaque && nbk.IsOccluding(nst);
                        }

                        if (!covered)
                        {
                            c.uv = uvp.GetUV(id, st, faceIndex);
                            c.visible = true;
                        }
                    }
                    grid[y*SZ + x] = c;
                }

                System.Array.Clear(used, 0, SZ*SZ);

                for (int y=0; y<SZ; y++)
                {
                    for (int x=0; x<SZ; )
                    {
                        int idx = y*SZ + x;
                        var c0 = grid[idx];
                        if (used[idx] || !c0.visible) { x++; continue; }

                        int w = 1;
                        while (x+w<SZ && !used[y*SZ + (x+w)] && grid[y*SZ + (x+w)].Same(c0)) w++;

                        int h = 1; bool ok=true;
                        while (y+h<SZ && ok)
                        {
                            for (int xx=0; xx<w; xx++)
                                if (used[(y+h)*SZ + (x+xx)] || !grid[(y+h)*SZ + (x+xx)].Same(c0)) { ok=false; break; }
                            if (ok) h++;
                        }

                        for (int yy=0; yy<h; yy++)
                            for (int xx=0; xx<w; xx++)
                                used[(y+yy)*SZ + (x+xx)] = true;

                        EmitFace(faceIndex, x, y, zFace, w, h, zIsMin, c0, v,n,uv,i0,i1,i2);
                        x += w;
                    }
                }
            }

            ArrayPool<Cell>.Shared.Return(grid, false);
            ArrayPool<bool>.Shared.Return(used, false);
        }

        // ===== X faces =====
        private static void GreedyFaceX(ISectionNeighborhood nb, IUVProvider uvp,
            List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> i0, List<int> i1, List<int> i2,
            int faceIndex, bool xIsMin)
        {
            var grid = ArrayPool<Cell>.Shared.Rent(SZ * SZ);
            var used = ArrayPool<bool>.Shared.Rent(SZ * SZ);

            for (int x = 0; x < SZ; x++)
            {
                int xFace = x;
                for (int y=0; y<SZ; y++)
                for (int z=0; z<SZ; z++)
                {
                    var (id, st) = nb.GetWithNeighbors(xFace,y,z);
                    var c = new Cell { id=id, st=st, visible=false };
                    if (id!=0)
                    {
                        var b = Voxel.Domain.Registry.BlockRegistry.Get(id);
                        c.rt = b.RenderType;

                        int nx = xFace + (xIsMin ? -1 : +1);
                        var (nid, nst) = nb.GetWithNeighbors(nx,y,z);
                        bool covered = false;
                        if (nid!=0)
                        {
                            var nbk = Voxel.Domain.Registry.BlockRegistry.Get(nid);
                            covered = nbk.RenderType==RenderType.Opaque && nbk.IsOccluding(nst);
                        }

                        if (!covered)
                        {
                            c.uv = uvp.GetUV(id, st, faceIndex);
                            c.visible = true;
                        }
                    }
                    grid[y*SZ + z] = c;
                }

                System.Array.Clear(used, 0, SZ*SZ);

                for (int y=0; y<SZ; y++)
                {
                    for (int z=0; z<SZ; )
                    {
                        int idx = y*SZ + z;
                        var c0 = grid[idx];
                        if (used[idx] || !c0.visible) { z++; continue; }

                        int w = 1;
                        while (z+w<SZ && !used[y*SZ + (z+w)] && grid[y*SZ + (z+w)].Same(c0)) w++;

                        int h = 1; bool ok=true;
                        while (y+h<SZ && ok)
                        {
                            for (int zz=0; zz<w; zz++)
                                if (used[(y+h)*SZ + (z+zz)] || !grid[(y+h)*SZ + (z+zz)].Same(c0)) { ok=false; break; }
                            if (ok) h++;
                        }

                        for (int yy=0; yy<h; yy++)
                            for (int zz=0; zz<w; zz++)
                                used[(y+yy)*SZ + (z+zz)] = true;

                        EmitFace(faceIndex, xFace, y, z, w, h, xIsMin, c0, v,n,uv,i0,i1,i2);
                        z += w;
                    }
                }
            }

            ArrayPool<Cell>.Shared.Return(grid, false);
            ArrayPool<bool>.Shared.Return(used, false);
        }

        // ===== Y faces =====
        private static void GreedyFaceY(ISectionNeighborhood nb, IUVProvider uvp,
            List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> i0, List<int> i1, List<int> i2,
            int faceIndex, bool yIsMax)
        {
            var grid = ArrayPool<Cell>.Shared.Rent(SZ * SZ);
            var used = ArrayPool<bool>.Shared.Rent(SZ * SZ);

            for (int y = 0; y < SZ; y++)
            {
                int yFace = y;
                for (int z=0; z<SZ; z++)
                for (int x=0; x<SZ; x++)
                {
                    var (id, st) = nb.GetWithNeighbors(x,yFace,z);
                    var c = new Cell { id=id, st=st, visible=false };
                    if (id!=0)
                    {
                        var b = Voxel.Domain.Registry.BlockRegistry.Get(id);
                        c.rt = b.RenderType;

                        int ny = yFace + (yIsMax ? +1 : -1);
                        var (nid, nst) = nb.GetWithNeighbors(x,ny,z);
                        bool covered = false;
                        if (nid!=0)
                        {
                            var nbk = Voxel.Domain.Registry.BlockRegistry.Get(nid);
                            covered = nbk.RenderType==RenderType.Opaque && nbk.IsOccluding(nst);
                        }

                        if (!covered)
                        {
                            c.uv = uvp.GetUV(id, st, faceIndex);
                            c.visible = true;
                        }
                    }
                    grid[z*SZ + x] = c;
                }

                System.Array.Clear(used, 0, SZ*SZ);

                for (int z=0; z<SZ; z++)
                {
                    for (int x=0; x<SZ; )
                    {
                        int idx = z*SZ + x;
                        var c0 = grid[idx];
                        if (used[idx] || !c0.visible) { x++; continue; }

                        int w = 1;
                        while (x+w<SZ && !used[z*SZ + (x+w)] && grid[z*SZ + (x+w)].Same(c0)) w++;

                        int h = 1; bool ok=true;
                        while (z+h<SZ && ok)
                        {
                            for (int xx=0; xx<w; xx++)
                                if (used[(z+h)*SZ + (x+xx)] || !grid[(z+h)*SZ + (x+xx)].Same(c0)) { ok=false; break; }
                            if (ok) h++;
                        }

                        for (int zz=0; zz<h; zz++)
                            for (int xx=0; xx<w; xx++)
                                used[(z+zz)*SZ + (x+xx)] = true;

                        EmitFace(faceIndex, x, yFace, z, w, h, yIsMax, c0, v,n,uv,i0,i1,i2);
                        x += w;
                    }
                }
            }

            ArrayPool<Cell>.Shared.Return(grid, false);
            ArrayPool<bool>.Shared.Return(used, false);
        }

        // === Emission avec l’ordre EXACT de ModelBakery par face ===
        private static void EmitFace(int face, int x, int y, int z, int w, int h, bool isMinOrMax, in Cell c,
            List<Vector3> V, List<Vector3> N, List<Vector2> UV, List<int> I0, List<int> I1, List<int> I2)
        {
            int baseIndex = V.Count;
            Vector3 p0, p1, p2, p3;

            switch (face)
            {
                case 0: // North (Z-)  {0,1,2,3}
                    {
                        float zf = z;
                        p0 = new Vector3(x,     y,     zf);
                        p1 = new Vector3(x + w, y,     zf);
                        p2 = new Vector3(x + w, y + h, zf);
                        p3 = new Vector3(x,     y + h, zf);
                        break;
                    }
                case 1: // South (Z+)  {5,4,7,6}
                    {
                        float zf = z + 1f;
                        p0 = new Vector3(x + w, y,     zf);
                        p1 = new Vector3(x,     y,     zf);
                        p2 = new Vector3(x,     y + h, zf);
                        p3 = new Vector3(x + w, y + h, zf);
                        break;
                    }
                case 2: // West (X-)   {4,0,3,7}
                    {
                        float xf = x;
                        // CORRIGÉ: ordre exact 4,0,3,7
                        p0 = new Vector3(xf, y,     z + w);
                        p1 = new Vector3(xf, y,     z);
                        p2 = new Vector3(xf, y + h, z);
                        p3 = new Vector3(xf, y + h, z + w);
                        break;
                    }
                case 3: // East (X+)   {1,5,6,2}
                    {
                        float xf = x + 1f;
                        // CORRIGÉ: ordre exact 1,5,6,2
                        p0 = new Vector3(xf, y,     z);
                        p1 = new Vector3(xf, y,     z + w);
                        p2 = new Vector3(xf, y + h, z + w);
                        p3 = new Vector3(xf, y + h, z);
                        break;
                    }
                case 4: // Up (Y+)     {3,2,6,7}
                    {
                        float yf = y + 1f;
                        p0 = new Vector3(x,     yf, z);
                        p1 = new Vector3(x + w, yf, z);
                        p2 = new Vector3(x + w, yf, z + h);
                        p3 = new Vector3(x,     yf, z + h);
                        break;
                    }
                default: // 5 Down (Y-) {4,5,1,0}
                    {
                        float yf = y;
                        p0 = new Vector3(x + w, yf, z);
                        p1 = new Vector3(x,     yf, z);
                        p2 = new Vector3(x,     yf, z + h);
                        p3 = new Vector3(x + w, yf, z + h);
                        break;
                    }
            }

            V.Add(p0); V.Add(p1); V.Add(p2); V.Add(p3);
            var nor = FACE_NORMAL[face]; N.Add(nor); N.Add(nor); N.Add(nor); N.Add(nor);

            UV.Add(new Vector2(c.uv.xMin, c.uv.yMin));
            UV.Add(new Vector2(c.uv.xMax, c.uv.yMin));
            UV.Add(new Vector2(c.uv.xMax, c.uv.yMax));
            UV.Add(new Vector2(c.uv.xMin, c.uv.yMax));

            var tgt = c.rt switch { RenderType.Opaque => I0, RenderType.Cutout => I1, _ => I2 };
            tgt.Add(baseIndex + 0); tgt.Add(baseIndex + 2); tgt.Add(baseIndex + 1);
            tgt.Add(baseIndex + 0); tgt.Add(baseIndex + 3); tgt.Add(baseIndex + 2);
        }
    }
}