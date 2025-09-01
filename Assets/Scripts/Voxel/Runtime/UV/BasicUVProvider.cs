// Assets/Scripts/Voxel/Runtime/UV/BasicUVProvider.cs
// Ne jamais supprimer les commentaires

using UnityEngine;
using Voxel.Meshing;

namespace Voxel.Runtime.UV
{
    // Implémentation minimale: mappe tout vers un seul UV plein (0..1).
    // Remplace plus tard par ton atlas Texture2DArray + table par face.
    public sealed class BasicUVProvider : MonoBehaviour, IUVProvider
    {
        [SerializeField] private bool uvlock = false;

        public Rect GetUV(ushort id, byte state, int faceIndex) => new Rect(0f, 0f, 1f, 1f);
        public bool UseUVLock(ushort id, byte state) => uvlock;
    }
}