// Assets/Scripts/Voxel/client/renderer/chunk/RenderSection.cs
// Gestion des GameObjects par section, pool Mesh/GO, frustum + anneaux.

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.World;

namespace Voxel.Client.Renderer.Chunk
{
    [RequireComponent(typeof(Voxel.Runtime.WorldRuntime))]
    public sealed class RenderSection : MonoBehaviour
    {
        public const int SIZE = 16;

        [Header("Renderer (assigné par dispatcher)")]
        public MeshFilter mf;
        public MeshRenderer mr;
        public MeshCollider mc;

        [HideInInspector] public SectionPos pos;              // coordonnées section
        [HideInInspector] public Bounds worldBounds;          // AABB monde
        [HideInInspector] public int ring;                    // anneau courant
        [HideInInspector] public bool hasTranslucent;         // indicateur pour tri B2F
        [HideInInspector] public bool isRegistered;           // dans le dispatcher

        // ===== Pool statique de GameObjects RenderSection =====
        private static readonly Stack<RenderSection> pool = new();

        public static RenderSection Create(Transform parent, SectionPos sp)
        {
            RenderSection rs;
            if (pool.Count > 0)
            {
                rs = pool.Pop();
                rs.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject($"Section_{sp.x}_{sp.y}_{sp.z}");
                rs = go.AddComponent<RenderSection>();
                rs.mf = go.AddComponent<MeshFilter>();
                rs.mr = go.AddComponent<MeshRenderer>();
                rs.mc = go.AddComponent<MeshCollider>();
            }

            rs.transform.SetParent(parent, false);
            rs.transform.position = new Vector3(sp.x * SIZE, sp.y * SIZE, sp.z * SIZE);
            rs.pos = sp;
            rs.worldBounds = new Bounds(rs.transform.position + new Vector3(8, 8, 8), new Vector3(16, 16, 16));
            rs.ring = 0;
            rs.hasTranslucent = false;
            rs.isRegistered = false;
            return rs;
        }

        public static void Recycle(RenderSection rs)
        {
            if (!rs) return;
            rs.isRegistered = false;
            if (rs.mf && rs.mf.sharedMesh) { var m = rs.mf.sharedMesh; rs.mf.sharedMesh = null; Object.Destroy(m); }
            if (rs.mc && rs.mc.sharedMesh) { var m = rs.mc.sharedMesh; rs.mc.sharedMesh = null; Object.Destroy(m); }
            rs.mr.enabled = false;
            rs.gameObject.SetActive(false);
            rs.transform.SetParent(null, false);
            pool.Push(rs);
        }
    }
}