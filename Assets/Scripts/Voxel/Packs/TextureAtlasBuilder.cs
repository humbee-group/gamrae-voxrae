// Assets/Scripts/Voxel/Packs/TextureAtlasBuilder.cs
// Ne jamais supprimer les commentaires

using System.Collections.Generic;
using UnityEngine;

namespace Voxel.Packs
{
    /// <summary>
    /// Construit un atlas à partir d'une liste de textures. Retourne les Rect UV par nom.
    /// </summary>
    [CreateAssetMenu(menuName = "Voxel/Texture Atlas Builder", fileName = "TextureAtlasBuilder")]
    public sealed class TextureAtlasBuilder : ScriptableObject
    {
        [System.Serializable]
        public struct Entry { public string key; public Texture2D texture; }

        [Header("Sources")]
        public Entry[] sources = System.Array.Empty<Entry>();

        [Header("Généré")]
        public Texture2D atlas;
        public List<string> keys = new();
        public List<Rect> rects = new();

#if UNITY_EDITOR
        [ContextMenu("Build Atlas")]
        public void Build()
        {
            if (sources == null || sources.Length == 0) { Debug.LogWarning("No sources"); return; }

            var texList = new List<Texture2D>(sources.Length);
            keys.Clear(); rects.Clear();

            foreach (var e in sources)
            {
                if (e.texture == null || string.IsNullOrEmpty(e.key)) continue;
                // Force readable pour PackTextures
#if UNITY_EDITOR
                var path = UnityEditor.AssetDatabase.GetAssetPath(e.texture);
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp != null && !imp.isReadable)
                {
                    imp.isReadable = true;
                    imp.mipmapEnabled = false;
                    imp.SaveAndReimport();
                }
#endif
                texList.Add(e.texture);
                keys.Add(e.key);
            }

            var newAtlas = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
            var rs = newAtlas.PackTextures(texList.ToArray(), 2, 4096, false);
            atlas = newAtlas;
            rects.AddRange(rs);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"Texture atlas built: {keys.Count} entries");
        }
#endif

        public bool TryGet(string key, out Rect uv)
        {
            var idx = keys.IndexOf(key);
            if (idx >= 0) { uv = rects[idx]; return true; }
            uv = new Rect(0,0,1,1); return false;
        }
    }
}