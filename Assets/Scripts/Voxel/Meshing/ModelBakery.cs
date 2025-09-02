// Assets/Scripts/Voxel/Meshing/ModelBakery.cs
// inchangé côté signatures, conservé pour les blocs non-cubes si tu en ajoutes plus tard.

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.Blocks;

namespace Voxel.Meshing
{
    public static class ModelBakery
    {
        private static readonly Vector3[] CUBE_VERTS =
        {
            new(0,0,0), new(1,0,0), new(1,1,0), new(0,1,0),
            new(0,0,1), new(1,0,1), new(1,1,1), new(0,1,1),
        };

        private static readonly int[][] FACE_QUADS =
        {
            new[] {0,1,2,3}, // North (Z-)
            new[] {5,4,7,6}, // South (Z+)
            new[] {4,0,3,7}, // West  (X-)
            new[] {1,5,6,2}, // East  (X+)
            new[] {3,2,6,7}, // Up    (Y+)
            new[] {4,5,1,0}, // Down  (Y-)
        };

        private static readonly Vector3[] FACE_NORMALS =
        {
            new(0, 0,-1),
            new(0, 0, 1),
            new(-1,0, 0),
            new(1, 0, 0),
            new(0, 1, 0),
            new(0,-1, 0),
        };

        public static void EmitQuad(
            List<Vector3> v, List<Vector3> n, List<Vector2> uv0,
            List<int> idxOpaque, List<int> idxCutout, List<int> idxTransp,
            RenderType rt,
            Vector3 origin, int faceIndex, Rect uv, bool uvlock)
        {
            var q = FACE_QUADS[faceIndex];
            int baseIndex = v.Count;

            for (int i = 0; i < 4; i++)
                v.Add(origin + CUBE_VERTS[q[i]]);

            var nor = FACE_NORMALS[faceIndex];
            n.Add(nor); n.Add(nor); n.Add(nor); n.Add(nor);

            uv0.Add(new Vector2(uv.xMin, uv.yMin));
            uv0.Add(new Vector2(uv.xMax, uv.yMin));
            uv0.Add(new Vector2(uv.xMax, uv.yMax));
            uv0.Add(new Vector2(uv.xMin, uv.yMax));

            var target = rt switch
            {
                RenderType.Opaque => idxOpaque,
                RenderType.Cutout => idxCutout,
                _ => idxTransp
            };
            target.Add(baseIndex + 0); target.Add(baseIndex + 2); target.Add(baseIndex + 1);
            target.Add(baseIndex + 0); target.Add(baseIndex + 3); target.Add(baseIndex + 2);
        }
    }
}