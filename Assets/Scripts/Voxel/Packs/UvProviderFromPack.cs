// Assets/Scripts/Voxel/Packs/UvProviderFromPack.cs
// Ne jamais supprimer les commentaires
using UnityEngine;
using Voxel.Meshing;
using Voxel.Packs.Json;

namespace Voxel.Packs
{
    public sealed class UvProviderFromPack : MonoBehaviour, IUVProvider
    {
        [Tooltip("Chemin absolu (pack.json). Vide => StreamingAssets/packs/default")]
        public string packFolderAbsolute;

        [Header("Materials (mêmes instances que dans LevelRenderer)")]
        public Material opaqueMaterial;
        public Material cutoutMaterial;
        public Material translucentMaterial;

        [Header("Options")]
        public bool uvlockAll = false;
        public string fallback = "stone";

        private Pack pack;
        private Atlas atlas;
        private BlockstateResolver resolver;

        private void Awake()
        {
            if (string.IsNullOrEmpty(packFolderAbsolute))
                packFolderAbsolute = System.IO.Path.Combine(Application.streamingAssetsPath, "packs/default");

            BuildAll();

            var renderers = Object.FindObjectsByType<Voxel.Runtime.LevelRenderer>(FindObjectsSortMode.None);
            foreach (var lr in renderers)
            {
                lr.opaqueMaterial      = opaqueMaterial;
                lr.cutoutMaterial      = cutoutMaterial;
                lr.translucentMaterial = translucentMaterial;
                lr.MarkAllRegisteredDirty();
            }
        }

        private void BuildAll()
        {
            pack = new Pack(packFolderAbsolute);
            if (!pack.Load(out var err))
            {
                Debug.LogError($"[UvProviderFromPack] Pack load failed at '{packFolderAbsolute}': {err}");
                return;
            }

            atlas = new Atlas();
            atlas.Build(pack.textures);
            resolver = new BlockstateResolver(pack);

            ApplyAtlas(opaqueMaterial,      atlas.atlas, cutout:false, transparent:false);
            ApplyAtlas(cutoutMaterial,      atlas.atlas, cutout:true,  transparent:false);
            ApplyAtlas(translucentMaterial, atlas.atlas, cutout:false, transparent:true);

            Debug.Log($"[UvProviderFromPack] Loaded {pack.textures.Count} textures → atlas OK ({atlas.atlas.width}x{atlas.atlas.height})");
        }

        private static void ApplyAtlas(Material mat, Texture2D tex, bool cutout, bool transparent)
        {
            if (!mat || !tex) return;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);   // URP
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);   // Built-in
            // Forcer le sampling pixel-perfect côté texture
            tex.filterMode = FilterMode.Point;
            tex.wrapMode   = TextureWrapMode.Clamp;

            if (cutout)
            {
                mat.EnableKeyword("_ALPHATEST_ON");
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.5f);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else if (transparent)
            {
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
            }
            else
            {
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
            }
        }

        public Rect GetUV(ushort id, byte state, int faceIndex)
        {
            if (atlas == null) return new Rect(0,0,1,1);
            if (!resolver.TryResolve(id, state, out var mref)) return atlas.GetUV(fallback);

            var modelName = TrimBlockPrefix(mref.model);
            if (!pack.models.TryGetValue(modelName, out var model)) return atlas.GetUV(fallback);

            if (model.parent == "block/cube_all" && model.textures != null && model.textures.TryGetValue("all", out var allRef))
                return atlas.GetUV(ResolveTex(model, allRef));

            if (model.parent == "block/cube" && model.textures != null)
            {
                string faceKey = faceIndex switch { 0=>"north",1=>"south",2=>"west",3=>"east",4=>"up",5=>"down", _=>"north" };
                if (!model.textures.TryGetValue(faceKey, out var tref))
                    if (!model.textures.TryGetValue("side", out tref) && !model.textures.TryGetValue("all", out tref))
                        tref = "block/stone";
                return atlas.GetUV(ResolveTex(model, tref));
            }
            return atlas.GetUV(fallback);
        }

        public bool UseUVLock(ushort id, byte state) => uvlockAll;

        public void Rebuild()
        {
            BuildAll();
            var renderers = Object.FindObjectsByType<Voxel.Runtime.LevelRenderer>(FindObjectsSortMode.None);
            foreach (var lr in renderers) lr.MarkAllRegisteredDirty();
        }

        private static string TrimBlockPrefix(string s) =>
            string.IsNullOrEmpty(s) ? "stone" : (s.StartsWith("block/") ? s[6..] : s);

        private static string ResolveTex(ModelJson m, string refStr)
        {
            if (string.IsNullOrEmpty(refStr)) return "stone";
            if (refStr.StartsWith("#"))
            {
                var key = refStr[1..];
                if (m.textures != null && m.textures.TryGetValue(key, out var resolved))
                    return TrimBlockPrefix(resolved);
                return "stone";
            }
            return TrimBlockPrefix(refStr);
        }
    }
}