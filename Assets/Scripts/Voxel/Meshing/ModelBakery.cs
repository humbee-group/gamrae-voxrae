// Assets/Scripts/Voxel/Meshing/ModelBakery.cs
// Ne jamais supprimer les commentaires

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.Blocks;

namespace Voxel.Meshing
{
    public static class ModelBakery
    {
        // Faces cube unité dans l'espace section (voxel local [0..1])
        // Ordre indices Tri CW pour Unity
        private static readonly Vector3[] CUBE_VERTS =
        {
            new(0,0,0), new(1,0,0), new(1,1,0), new(0,1,0), // Back  (Z-)
            new(0,0,1), new(1,0,1), new(1,1,1), new(0,1,1), // Front (Z+)
        };

        private static readonly int[][] FACE_QUADS =
        {
            new[] {0,1,2,3}, // North (Back, Z-)
            new[] {5,4,7,6}, // South (Front, Z+)
            new[] {4,0,3,7}, // West  (X-)
            new[] {1,5,6,2}, // East  (X+)
            new[] {3,2,6,7}, // Up    (Y+)
            new[] {4,5,1,0}, // Down  (Y-)
        };

        private static readonly Vector3[] FACE_NORMALS =
        {
            new(0, 0,-1), // North
            new(0, 0, 1), // South
            new(-1,0, 0), // West
            new(1, 0, 0), // East
            new(0, 1, 0), // Up
            new(0,-1, 0), // Down
        };

        public static void EmitQuad(
            List<Vector3> v, List<Vector3> n, List<Vector2> uv0,
            List<int> idxOpaque, List<int> idxCutout, List<int> idxTransp,
            RenderType rt,
            Vector3 origin, int faceIndex, Rect uv, bool uvlock)
        {
            var q = FACE_QUADS[faceIndex];
            int baseIndex = v.Count;

            // Positions
            for (int i = 0; i < 4; i++)
                v.Add(origin + CUBE_VERTS[q[i]]);

            // Normales
            var nor = FACE_NORMALS[faceIndex];
            n.Add(nor); n.Add(nor); n.Add(nor); n.Add(nor);

            // UV (option uvlock simple: ne pas tourner en fonction de la face)
            if (uvlock)
            {
                uv0.Add(new Vector2(uv.xMin, uv.yMin));
                uv0.Add(new Vector2(uv.xMax, uv.yMin));
                uv0.Add(new Vector2(uv.xMax, uv.yMax));
                uv0.Add(new Vector2(uv.xMin, uv.yMax));
            }
            else
            {
                // mapping par face “standard”
                uv0.Add(new Vector2(uv.xMin, uv.yMin));
                uv0.Add(new Vector2(uv.xMax, uv.yMin));
                uv0.Add(new Vector2(uv.xMax, uv.yMax));
                uv0.Add(new Vector2(uv.xMin, uv.yMax));
            }

            // Indices par RenderType
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