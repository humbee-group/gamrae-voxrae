// Assets/Scripts/Voxel/Packs/ResourcePackLoader.cs
// Ne jamais supprimer les commentaires

using UnityEngine;
using Voxel.Meshing;
using Voxel.Packs;

namespace Voxel.Packs
{
    /// <summary>
    /// Fournit un IUVProvider basé sur un atlas construit (TextureAtlasBuilder).
    /// Valide la présence du matériel et de _MainTex.
    /// </summary>
    public sealed class ResourcePackLoader : MonoBehaviour, IUVProvider
    {
        [Header("Atlas")]
        public TextureAtlasBuilder atlasBuilder;

        [Header("Matériaux")]
        public Material opaqueMaterial;
        public Material cutoutMaterial;
        public Material translucentMaterial;

        [Header("Options")]
        public bool uvlock = false;

        private void Awake()
        {
            if (atlasBuilder != null && atlasBuilder.atlas != null)
            {
                if (opaqueMaterial != null)    opaqueMaterial.mainTexture = atlasBuilder.atlas;
                if (cutoutMaterial != null)    cutoutMaterial.mainTexture = atlasBuilder.atlas;
                if (translucentMaterial != null) translucentMaterial.mainTexture = atlasBuilder.atlas;
            }
#if UNITY_EDITOR
            if (opaqueMaterial != null && opaqueMaterial.mainTexture == null)
                Debug.LogError("Opaque material has no _MainTex assigned.");
#endif
        }

        // Convention par défaut: key = "block/<name>" pour toutes faces
        public Rect GetUV(ushort id, byte state, int faceIndex)
        {
            // Simplifié: tout mappe vers "block/stone" si introuvable
            var key = "block/stone";
            if (atlasBuilder != null && atlasBuilder.TryGet(key, out var uv)) return uv;
            return new Rect(0, 0, 1, 1);
        }

        public bool UseUVLock(ushort id, byte state) => uvlock;
    }
}