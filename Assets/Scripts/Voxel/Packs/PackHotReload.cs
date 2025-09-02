// Assets/Scripts/Voxel/Packs/PackHotReload.cs
// Ne jamais supprimer les commentaires

using System.IO;
using UnityEngine;
using Voxel.Client.Renderer.Chunk; // ChunkRenderDispatcher

#if UNITY_EDITOR
namespace Voxel.Packs
{
    [ExecuteAlways]
    public sealed class PackHotReload : MonoBehaviour
    {
        [Tooltip("Chemin du pack (racine avec pack.json). Vide => StreamingAssets/packs/default")]
        public string packFolderAbsolute;

        [Tooltip("UvProviderFromPack à recharger")]
        public UvProviderFromPack provider;

        [Tooltip("Laisser vide pour marquer tous les ChunkRenderDispatcher dirty")]
        public ChunkRenderDispatcher[] dispatchers;

        [Tooltip("Intervalle de vérification (s)")]
        public float checkInterval = 0.5f;

        float timer;
        System.DateTime lastWrite;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(packFolderAbsolute))
                packFolderAbsolute = Path.Combine(Application.streamingAssetsPath, "packs/default");

            if (Directory.Exists(packFolderAbsolute))
                lastWrite = Directory.GetLastWriteTimeUtc(packFolderAbsolute);
        }

        private void Update()
        {
            // En mode Éditeur hors Play, on “simule” le temps pour vérifier périodiquement
            timer += Application.isPlaying ? Time.deltaTime : 0.2f;
            if (timer < checkInterval) return;
            timer = 0f;

            if (string.IsNullOrEmpty(packFolderAbsolute) || !Directory.Exists(packFolderAbsolute)) return;

            var t = Directory.GetLastWriteTimeUtc(packFolderAbsolute);
            if (t <= lastWrite) return;
            lastWrite = t;

            // Rebuild atlas + resolver
            provider?.Rebuild();

            // Remesh: marque tous les dispatchers dirty
            if (dispatchers != null && dispatchers.Length > 0)
            {
                foreach (var d in dispatchers) d?.MarkAllRegisteredDirty();
            }
            else
            {
                var all = Object.FindObjectsByType<ChunkRenderDispatcher>(FindObjectsSortMode.None);
                foreach (var d in all) d.MarkAllRegisteredDirty();
            }

            Debug.Log("[PackHotReload] Pack modifié → rebuild atlas + remesh.");
        }
    }
}
#endif