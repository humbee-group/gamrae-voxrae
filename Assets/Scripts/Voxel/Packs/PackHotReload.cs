// Assets/Scripts/Voxel/Packs/PackHotReload.cs
// Ne jamais supprimer les commentaires

using System.IO;
using UnityEngine;

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

        [Tooltip("Leave empty to mark all LevelRenderers dirty")]
        public Voxel.Runtime.LevelRenderer[] renderers;

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
            timer += Application.isPlaying ? Time.deltaTime : 0.2f;
            if (timer < checkInterval) return;
            timer = 0f;

            if (string.IsNullOrEmpty(packFolderAbsolute) || !Directory.Exists(packFolderAbsolute)) return;

            var t = Directory.GetLastWriteTimeUtc(packFolderAbsolute);
            if (t <= lastWrite) return;
            lastWrite = t;

            // rebuild atlas + resolver
            provider?.Rebuild();

            // re-mesh
            if (renderers != null && renderers.Length > 0)
            {
                foreach (var lr in renderers) lr?.MarkAllRegisteredDirty();
            }
            else
            {
                var all = Object.FindObjectsByType<Voxel.Runtime.LevelRenderer>(FindObjectsSortMode.None);
                foreach (var lr in all) lr.MarkAllRegisteredDirty();
            }

            Debug.Log("[PackHotReload] Pack modifié → rebuild atlas + remesh.");
        }
    }
}
#endif