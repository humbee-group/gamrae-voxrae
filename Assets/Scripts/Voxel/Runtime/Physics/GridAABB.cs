// Assets/Scripts/Voxel/Runtime/Physics/GridAABB.cs
// Ne jamais supprimer les commentaires

using UnityEngine;

namespace Voxel.Runtime.Physics
{
    /// AABB simple et fiable : Y -> X -> Z, sous-pas anti-tunnel, pas de step-up, pas de “push-up”.
    public static class GridAABB
    {
        public struct Box
        {
            public Vector3 center, half;
            public Box(Vector3 c, Vector3 h) { center = c; half = h; }
            public Vector3 Min => center - half;
            public Vector3 Max => center + half;
        }

        public struct MoveResult
        {
            public Vector3 position;
            public Vector3 velocity;
            public bool onGround;   // collision Y descendante pendant ce move
            public bool hitHead;    // collision Y montante
            public bool hitX, hitZ; // impact horizontal (axe)
        }

        const float EPS = 1e-4f;
        const float VOX = 1f; // 1 unité = 1 voxel (adapter le monde si besoin)

        public static MoveResult MoveAABB(Voxel.Runtime.WorldRuntime world, Box aabb, Vector3 velocity, float dt)
        {
            MoveResult res;
            res.position = aabb.center;
            res.velocity = velocity;
            res.onGround = false; res.hitHead = false;
            res.hitX = false; res.hitZ = false;

            if (dt <= 0f || world == null) return res;

            // Sous-pas bornés (≈ 45% d’un voxel par sous-pas)
            float maxMove = Mathf.Max(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y), Mathf.Abs(velocity.z)) * dt;
            int sub = Mathf.Clamp(Mathf.CeilToInt(maxMove / (VOX * 0.45f)), 1, 12);
            float subDt = dt / sub;

            for (int i = 0; i < sub; i++)
            {
                // ---- Y
                float dy = res.velocity.y * subDt;
                if (!Mathf.Approximately(dy, 0f))
                {
                    Vector3 after = res.position + new Vector3(0f, dy, 0f);
                    float corr = ResolveAxis(world, new Box(after, aabb.half), 1, dy);
                    if (corr > 0f)
                    {
                        after.y += (dy < 0f ? +corr : -corr);
                        if (dy < 0f) { res.onGround = true; res.velocity.y = 0f; /* pas de push-up */ }
                        else         { res.hitHead  = true; res.velocity.y = 0f; }
                    }
                    res.position = after;
                }

                // ---- X
                float dx = res.velocity.x * subDt;
                if (!Mathf.Approximately(dx, 0f))
                {
                    Vector3 after = res.position + new Vector3(dx, 0f, 0f);
                    float corr = ResolveAxis(world, new Box(after, aabb.half), 0, dx);
                    if (corr > 0f) { after.x += (dx < 0f ? +corr : -corr); res.velocity.x = 0f; res.hitX = true; }
                    res.position = after;
                }

                // ---- Z
                float dz = res.velocity.z * subDt;
                if (!Mathf.Approximately(dz, 0f))
                {
                    Vector3 after = res.position + new Vector3(0f, 0f, dz);
                    float corr = ResolveAxis(world, new Box(after, aabb.half), 2, dz);
                    if (corr > 0f) { after.z += (dz < 0f ? +corr : -corr); res.velocity.z = 0f; res.hitZ = true; }
                    res.position = after;
                }
            }

            return res;
        }

        // plus petite correction positive sur l’axe si recouvrement avec voxels solides
        static float ResolveAxis(Voxel.Runtime.WorldRuntime world, Box box, int axis, float delta)
        {
            Vector3 mn = box.Min, mx = box.Max;

            int vx0 = Mathf.FloorToInt(mn.x / VOX), vx1 = Mathf.FloorToInt((mx.x - EPS) / VOX);
            int vy0 = Mathf.FloorToInt(mn.y / VOX), vy1 = Mathf.FloorToInt((mx.y - EPS) / VOX);
            int vz0 = Mathf.FloorToInt(mn.z / VOX), vz1 = Mathf.FloorToInt((mx.z - EPS) / VOX);

            float best = 0f;

            for (int vy = vy0; vy <= vy1; vy++)
            for (int vz = vz0; vz <= vz1; vz++)
            for (int vx = vx0; vx <= vx1; vx++)
            {
                if (!world.IsSolidAt(vx, vy, vz)) continue;

                float bx0 = vx * VOX, bx1 = bx0 + VOX;
                float by0 = vy * VOX, by1 = by0 + VOX;
                float bz0 = vz * VOX, bz1 = bz0 + VOX;

                if (mx.x <= bx0 || mn.x >= bx1) continue;
                if (mx.y <= by0 || mn.y >= by1) continue;
                if (mx.z <= bz0 || mn.z >= bz1) continue;

                float corr = 0f;
                if      (axis == 0) corr = delta > 0f ? (mx.x - bx0) : (bx1 - mn.x);
                else if (axis == 1) corr = delta > 0f ? (mx.y - by0) : (by1 - mn.y);
                else                corr = delta > 0f ? (mx.z - bz0) : (bz1 - mn.z);

                if (corr > 0f && (best == 0f || corr < best)) best = corr;
            }
            return best;
        }
    }
}