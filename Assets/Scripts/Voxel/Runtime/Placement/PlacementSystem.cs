// Assets/Scripts/Voxel/Runtime/Placement/PlacementSystem.cs
// Ne jamais supprimer les commentaires

using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.Domain.Blocks;
using Voxel.Domain.World;
using Voxel.Domain.WorldRuntime;

namespace Voxel.Runtime.Placement
{
    public static class PlacementSystem
    {
        // Pose dans le voxel ADJACENT à la face frappée (MC-like)
        public static bool PlaceByRay(Camera cam, System.Func<int,int,int,ushort,byte,bool> setFunc, ushort blockId, float maxDist = 128f)
        {
            if (!TryRaycast(cam, out var p, out var n, maxDist))
                return GridMarch(cam, maxDist, (wx,wy,wz,faceN) => PlaceAdjacent(wx,wy,wz, faceN, cam.transform.forward, setFunc, blockId));

            var hit = p - n * 0.001f;
            int wx = Mathf.FloorToInt(hit.x);
            int wy = Mathf.FloorToInt(hit.y);
            int wz = Mathf.FloorToInt(hit.z);
            var dir = NormalToInt3(n);
            return PlaceAt(wx + dir.x, wy + dir.y, wz + dir.z, dir, cam.transform.forward, setFunc, blockId);
        }

        // Supprime le voxel frappé
        public static bool RemoveByRay(Camera cam, System.Func<int,int,int,ushort,byte,bool> setFunc, float maxDist = 128f)
        {
            if (!TryRaycast(cam, out var p, out var n, maxDist))
                return GridMarch(cam, maxDist, (wx,wy,wz,faceN) => setFunc(wx,wy,wz, 0, 0));

            var hit = p - n * 0.001f;
            return setFunc(Mathf.FloorToInt(hit.x), Mathf.FloorToInt(hit.y), Mathf.FloorToInt(hit.z), 0, 0);
        }

        // ---------- internals ----------
        private static bool TryRaycast(Camera cam, out Vector3 p, out Vector3 n, float maxDist)
        {
            Vector2 screen = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width*0.5f, Screen.height*0.5f);
            if (UnityEngine.Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, maxDist))
            { p = hit.point; n = hit.normal; return true; }
            p = default; n = default; return false;
        }

        // DDA grid march (taille voxel = 1)
        private static bool GridMarch(Camera cam, float maxDist, System.Func<int,int,int,Vector3,bool> onHit)
        {
            Vector3 o = cam.transform.position;
            Vector3 d = cam.transform.forward.normalized;
            float t = 0f;

            int x = Mathf.FloorToInt(o.x), y = Mathf.FloorToInt(o.y), z = Mathf.FloorToInt(o.z);
            int stepX = d.x >= 0 ? 1 : -1, stepY = d.y >= 0 ? 1 : -1, stepZ = d.z >= 0 ? 1 : -1;

            float nextX = ((stepX > 0 ? x + 1 : x) - o.x) / (d.x == 0 ? 1e-9f : d.x);
            float nextY = ((stepY > 0 ? y + 1 : y) - o.y) / (d.y == 0 ? 1e-9f : d.y);
            float nextZ = ((stepZ > 0 ? z + 1 : z) - o.z) / (d.z == 0 ? 1e-9f : d.z);

            float tDeltaX = Mathf.Abs(1f / (d.x == 0 ? 1e-9f : d.x));
            float tDeltaY = Mathf.Abs(1f / (d.y == 0 ? 1e-9f : d.y));
            float tDeltaZ = Mathf.Abs(1f / (d.z == 0 ? 1e-9f : d.z));

            for (int i = 0; i < 256 && t <= maxDist; i++)
            {
                if (nextX < nextY && nextX < nextZ) { x += stepX; t = nextX; nextX += tDeltaX; if (onHit(x,y,z, new Vector3(-stepX,0,0))) return true; }
                else if (nextY < nextZ)             { y += stepY; t = nextY; nextY += tDeltaY; if (onHit(x,y,z, new Vector3(0,-stepY,0))) return true; }
                else                                 { z += stepZ; t = nextZ; nextZ += tDeltaZ; if (onHit(x,y,z, new Vector3(0,0,-stepZ))) return true; }
            }
            return false;
        }

        private static bool PlaceAdjacent(int wx, int wy, int wz, Vector3 faceN, Vector3 fwd, System.Func<int,int,int,ushort,byte,bool> setFunc, ushort blockId)
        {
            var dir = NormalToInt3(faceN);
            return PlaceAt(wx + dir.x, wy + dir.y, wz + dir.z, dir, fwd, setFunc, blockId);
        }

        private static bool PlaceAt(int wx, int wy, int wz, Vector3Int faceDir, Vector3 fwd,
                                    System.Func<int,int,int,ushort,byte,bool> setFunc, ushort blockId)
        {
            var face = NormalToDir(new Vector3(faceDir.x, faceDir.y, faceDir.z));
            var ctx = new BlockPlaceContext(new BlockPos(wx, wy, wz), face, new Vector3(0.5f,0.5f,0.5f), FaceToCardinal(fwd), false);
            byte st = PlacementRules.Compute(blockId, ctx);
            return setFunc(wx, wy, wz, blockId, st);
        }

        private static Vector3Int NormalToInt3(Vector3 n)
        {
            if (Mathf.Abs(n.x) > Mathf.Abs(n.y) && Mathf.Abs(n.x) > Mathf.Abs(n.z)) return new Vector3Int(n.x > 0 ? 1 : -1, 0, 0);
            if (Mathf.Abs(n.z) > Mathf.Abs(n.x) && Mathf.Abs(n.z) > Mathf.Abs(n.y)) return new Vector3Int(0, 0, n.z > 0 ? 1 : -1);
            return new Vector3Int(0, n.y > 0 ? 1 : -1, 0);
        }

        private static Direction NormalToDir(Vector3 n)
        {
            if (Mathf.Abs(n.x) > Mathf.Abs(n.y) && Mathf.Abs(n.x) > Mathf.Abs(n.z)) return n.x > 0 ? Direction.East : Direction.West;
            if (Mathf.Abs(n.z) > Mathf.Abs(n.x) && Mathf.Abs(n.z) > Mathf.Abs(n.y)) return n.z > 0 ? Direction.South : Direction.North;
            return n.y > 0 ? Direction.Up : Direction.Down;
        }

        private static Direction FaceToCardinal(Vector3 fwd)
        {
            Vector2 f = new(fwd.x, fwd.z);
            if (f.sqrMagnitude < 1e-6f) return Direction.North;
            float a = Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg;
            a = (a + 360f) % 360f;
            if (a >= 315 || a < 45) return Direction.East;
            if (a < 135) return Direction.North;
            if (a < 225) return Direction.West;
            return Direction.South;
        }
    }
}