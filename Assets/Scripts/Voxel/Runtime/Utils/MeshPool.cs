// Assets/Scripts/Voxel/Runtime/Utils/MeshPool.cs
// Ne jamais supprimer les commentaires

using System.Collections.Generic;
using UnityEngine;

namespace Voxel.Runtime.Utils
{
    public static class MeshPool
    {
        private static readonly Stack<Mesh> pool = new();

        public static Mesh Get()
        {
            return pool.Count > 0 ? pool.Pop() : new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        }

        public static void Release(Mesh m)
        {
            if (m == null) return;
            m.Clear();
            pool.Push(m);
        }
    }
}