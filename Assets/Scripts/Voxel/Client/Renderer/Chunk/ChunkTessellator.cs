// Assets/Scripts/Voxel/Client/Renderer/Chunk/ChunkTessellator.cs
// Non-greedy. UV (incl. ColumnBlock) + vertex colors: RGB=sky/15, A=block/15.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Voxel.Meshing;
using Voxel.Domain.Blocks;

namespace Voxel.Meshing
{
    public interface ILightProvider
    {
        byte GetSky(int lx, int ly, int lz);
        byte GetBlk(int lx, int ly, int lz);
    }
}

namespace Voxel.Client.Renderer.Chunk
{
    public interface ISectionNeighborhood
    {
        (ushort id, byte state) GetWithNeighbors(int lx, int ly, int lz);
        bool Exists(int lx, int ly, int lz);
    }

    public static class ChunkTessellator
    {
        private static readonly (int dx,int dy,int dz)[] NEI =
        { (0,0,-1),(0,0,1),(-1,0,0),(1,0,0),(0,1,0),(0,-1,0) };

        public static Mesh BuildMesh(ISectionNeighborhood nb, IUVProvider uvp, ILightProvider lp)
        {
            var v  = ListPool<Vector3>.Get();
            var n  = ListPool<Vector3>.Get();
            var uv = ListPool<Vector2>.Get();
            var col= ListPool<Color32>.Get();
            var i0 = ListPool<int>.Get();
            var i1 = ListPool<int>.Get();
            var i2 = ListPool<int>.Get();

            for (int y=0; y<16; y++)
            for (int z=0; z<16; z++)
            for (int x=0; x<16; x++)
            {
                var (id, st0) = nb.GetWithNeighbors(x,y,z);
                if (id==0) continue;

                var block = Voxel.Domain.Registry.BlockRegistry.Get(id);
                var rt = block.RenderType;

                bool isColumn = block is ColumnBlock;
                Axis axis = Axis.Y;
                if (isColumn) axis = block.DecodeState(st0).axis ?? Axis.Y;

                for (int f=0; f<6; f++)
                {
                    var d = NEI[f];
                    var (nid,nst) = nb.GetWithNeighbors(x+d.dx, y+d.dy, z+d.dz);
                    bool covered = nid!=0 && Voxel.Domain.Registry.BlockRegistry.Get(nid).RenderType==RenderType.Opaque
                                          && Voxel.Domain.Registry.BlockRegistry.Get(nid).IsOccluding(nst);
                    if (covered) continue;

                    Rect r = uvp.GetUV(id, st0, f);
                    int baseIndex = v.Count;

                    EmitQuadGeo(v,n, new Vector3(x,y,z), f);
                    if (isColumn) EmitUVColumn(uv, r, f, axis);
                    else          EmitUVDefault(uv, r);
                    EmitFaceColors(col, lp, x,y,z,f);   // <<< encodage correct

                    var tgt = rt switch { RenderType.Opaque=>i0, RenderType.Cutout=>i1, _=>i2 };
                    tgt.Add(baseIndex+0); tgt.Add(baseIndex+2); tgt.Add(baseIndex+1);
                    tgt.Add(baseIndex+0); tgt.Add(baseIndex+3); tgt.Add(baseIndex+2);
                }
            }

            var mesh = new Mesh {
                indexFormat = v.Count>65535? IndexFormat.UInt32: IndexFormat.UInt16,
                bounds = new Bounds(new Vector3(8,8,8), new Vector3(16,16,16))
            };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetColors(col);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(i0,0,false); mesh.SetTriangles(i1,1,false); mesh.SetTriangles(i2,2,false);
            mesh.UploadMeshData(false);

            ListPool<Vector3>.Release(v); ListPool<Vector3>.Release(n);
            ListPool<Vector2>.Release(uv); ListPool<Color32>.Release(col);
            ListPool<int>.Release(i0); ListPool<int>.Release(i1); ListPool<int>.Release(i2);
            return mesh;
        }

        private static void EmitQuadGeo(List<Vector3> v, List<Vector3> n, Vector3 o, int f)
        {
            switch(f)
            {
                case 0: { var nor = Vector3.back;   v.Add(o+new Vector3(0,0,0)); v.Add(o+new Vector3(1,0,0)); v.Add(o+new Vector3(1,1,0)); v.Add(o+new Vector3(0,1,0)); n.Add(nor);n.Add(nor);n.Add(nor);n.Add(nor); } break;
                case 1: { var nor = Vector3.forward;v.Add(o+new Vector3(1,0,1)); v.Add(o+new Vector3(0,0,1)); v.Add(o+new Vector3(0,1,1)); v.Add(o+new Vector3(1,1,1)); n.Add(nor);n.Add(nor);n.Add(nor);n.Add(nor); } break;
                case 2: { var nor = Vector3.left;   v.Add(o+new Vector3(0,0,1)); v.Add(o+new Vector3(0,0,0)); v.Add(o+new Vector3(0,1,0)); v.Add(o+new Vector3(0,1,1)); n.Add(nor);n.Add(nor);n.Add(nor);n.Add(nor); } break;
                case 3: { var nor = Vector3.right;  v.Add(o+new Vector3(1,0,0)); v.Add(o+new Vector3(1,0,1)); v.Add(o+new Vector3(1,1,1)); v.Add(o+new Vector3(1,1,0)); n.Add(nor);n.Add(nor);n.Add(nor);n.Add(nor); } break;
                case 4: { var nor = Vector3.up;     v.Add(o+new Vector3(0,1,0)); v.Add(o+new Vector3(1,1,0)); v.Add(o+new Vector3(1,1,1)); v.Add(o+new Vector3(0,1,1)); n.Add(nor);n.Add(nor);n.Add(nor);n.Add(nor); } break;
                default:{ var nor = Vector3.down;   v.Add(o+new Vector3(1,0,0)); v.Add(o+new Vector3(0,0,0)); v.Add(o+new Vector3(0,0,1)); v.Add(o+new Vector3(1,0,1)); n.Add(nor);n.Add(nor);n.Add(nor);n.Add(nor); } break;
            }
        }

        private static void EmitUVDefault(List<Vector2> uv, Rect r)
        {
            uv.Add(new Vector2(r.xMin, r.yMin));
            uv.Add(new Vector2(r.xMax, r.yMin));
            uv.Add(new Vector2(r.xMax, r.yMax));
            uv.Add(new Vector2(r.xMin, r.yMax));
        }

        private static void EmitUVColumn(List<Vector2> uv, Rect r, int f, Axis axis)
        {
            bool end = (axis==Axis.Y&&(f==4||f==5)) || (axis==Axis.X&&(f==2||f==3)) || (axis==Axis.Z&&(f==0||f==1));
            if (end) { EmitUVDefault(uv,r); return; }

            void CW(){ uv.Add(new Vector2(r.xMin,r.yMax)); uv.Add(new Vector2(r.xMin,r.yMin)); uv.Add(new Vector2(r.xMax,r.yMin)); uv.Add(new Vector2(r.xMax,r.yMax)); }
            void R180(){ uv.Add(new Vector2(r.xMax,r.yMax)); uv.Add(new Vector2(r.xMin,r.yMax)); uv.Add(new Vector2(r.xMin,r.yMin)); uv.Add(new Vector2(r.xMax,r.yMin)); }

            if (axis==Axis.Y) EmitUVDefault(uv,r);
            else if (axis==Axis.X) { if (f==0||f==1||f==4||f==5) CW(); else EmitUVDefault(uv,r); }
            else { if (f==2||f==3) CW(); else if (f==4||f==5) R180(); else EmitUVDefault(uv,r); }
        }

        // RGB = sky/15, A = block/15  (séparation claire pour le shader)
        private static void EmitFaceColors(List<Color32> c, Voxel.Meshing.ILightProvider lp, int x,int y,int z,int f)
        {
            (int ox,int oy,int oz)[] vtx = f switch
            {
                0 => new[] {(0,0,0),(1,0,0),(1,1,0),(0,1,0)},
                1 => new[] {(1,0,1),(0,0,1),(0,1,1),(1,1,1)},
                2 => new[] {(0,0,1),(0,0,0),(0,1,0),(0,1,1)},
                3 => new[] {(1,0,0),(1,0,1),(1,1,1),(1,1,0)},
                4 => new[] {(0,1,0),(1,1,0),(1,1,1),(0,1,1)},
                _ => new[] {(1,0,0),(0,0,0),(0,0,1),(1,0,1)},
            };
            for (int i=0;i<4;i++)
            {
                int lx=x+vtx[i].ox, ly=y+vtx[i].oy, lz=z+vtx[i].oz;
                float s = lp.GetSky(lx,ly,lz) / 15f;
                float b = lp.GetBlk(lx,ly,lz) / 15f;
                byte sv = (byte)Mathf.RoundToInt(Mathf.Clamp01(s)*255f);
                byte bv = (byte)Mathf.RoundToInt(Mathf.Clamp01(b)*255f);
                c.Add(new Color32(sv,sv,sv,bv));     // <<< RGB=sky, A=block
            }
        }
    }

    internal static class ListPool<T>
    {
        private static readonly System.Collections.Generic.Stack<System.Collections.Generic.List<T>> Pool = new();
        public static System.Collections.Generic.List<T> Get(){ if (Pool.Count>0){ var l=Pool.Pop(); l.Clear(); return l; } return new System.Collections.Generic.List<T>(2048); }
        public static void Release(System.Collections.Generic.List<T> l){ l.Clear(); Pool.Push(l); }
    }
}