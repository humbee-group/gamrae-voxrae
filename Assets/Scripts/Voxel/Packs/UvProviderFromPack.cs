// Assets/Scripts/Voxel/Packs/UvProviderFromPack.cs
// Provider UV: cube_all, cube, cube_bottom_top, cube_column (axis-aware). Sans logs.

using UnityEngine;
using Voxel.Packs.Json;
using Voxel.Client.Renderer.Chunk;
using Voxel.Domain.Registry;
using Voxel.Domain.Blocks; // Axis/StateProps

namespace Voxel.Meshing
{
    public interface IUVProvider
    {
        Rect GetUV(ushort id, byte state, int faceIndex);
        bool UseUVLock(ushort id, byte state);
    }

    public interface ISectionReader
    {
        (ushort id, byte state) Get(int lx, int ly, int lz);
        bool InBounds(int lx, int ly, int lz);
    }
}

namespace Voxel.Packs
{
    public sealed class UvProviderFromPack : MonoBehaviour, Voxel.Meshing.IUVProvider
    {
        [Tooltip("Chemin absolu (pack.json). Vide => StreamingAssets/packs/default")]
        public string packFolderAbsolute;

        [Header("Materials (0=Opaque,1=Cutout,2=Translucent)")]
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
            BlockRegister.Init();
            if (string.IsNullOrEmpty(packFolderAbsolute))
                packFolderAbsolute = System.IO.Path.Combine(Application.streamingAssetsPath, "packs/default");
            BuildAll();

            var dispatchers = Object.FindObjectsByType<ChunkRenderDispatcher>(FindObjectsSortMode.None);
            foreach (var d in dispatchers)
            {
                d.opaqueMaterial      = opaqueMaterial;
                d.cutoutMaterial      = cutoutMaterial;
                d.translucentMaterial = translucentMaterial;
                d.MarkAllRegisteredDirty();
            }
        }

        public void Rebuild()
        {
            BlockRegister.Init();
            BuildAll();
            var ds = Object.FindObjectsByType<ChunkRenderDispatcher>(FindObjectsSortMode.None);
            foreach (var d in ds) d.MarkAllRegisteredDirty();
        }

        private void BuildAll()
        {
            pack = new Pack(packFolderAbsolute);
            if (!pack.Load(out _)) return;

            atlas = new Atlas();
            atlas.Build(pack.textures);
            resolver = new BlockstateResolver(pack);

            ApplyAtlas(opaqueMaterial,      atlas.atlas, cutout:false, transparent:false);
            ApplyAtlas(cutoutMaterial,      atlas.atlas, cutout:true,  transparent:false);
            ApplyAtlas(translucentMaterial, atlas.atlas, cutout:false, transparent:true);
        }

        private static void ApplyAtlas(Material mat, Texture2D tex, bool cutout, bool transparent)
        {
            if (!mat || !tex) return;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode   = TextureWrapMode.Clamp;

            // important pour ton shader VertexLit : active l’émission (glow faible en plus des vertex colors)
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);

            if (cutout)
            {
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
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

            if (model.parent == "block/cube_all")
            {
                if (model.textures != null && model.textures.TryGetValue("all", out var allRef))
                    return atlas.GetUV(TrimBlockPrefix(ResolveTex(model, allRef)));
                return atlas.GetUV(fallback);
            }

            if (model.parent == "block/cube")
            {
                if (model.textures == null) return atlas.GetUV(fallback);
                string faceKey = faceIndex switch
                {
                    0=>"north", 1=>"south", 2=>"west", 3=>"east", 4=>"up", 5=>"down", _=>"north"
                };
                if (!model.textures.TryGetValue(faceKey, out var tref))
                {
                    if (!model.textures.TryGetValue("side", out tref) && !model.textures.TryGetValue("all", out tref))
                        return atlas.GetUV(fallback);
                }
                return atlas.GetUV(TrimBlockPrefix(ResolveTex(model, tref)));
            }

            if (model.parent == "block/cube_bottom_top")
            {
                if (model.textures == null) return atlas.GetUV(fallback);
                string key = faceIndex switch { 4=>"top", 5=>"bottom", _=>"side" };
                if (!model.textures.TryGetValue(key, out var tref)) return atlas.GetUV(fallback);
                return atlas.GetUV(TrimBlockPrefix(ResolveTex(model, tref)));
            }

            // cube_column (axis-aware)
            if (model.parent == "block/cube_column")
            {
                if (model.textures == null) return atlas.GetUV(fallback);

                var blk = BlockRegistry.Get(id);
                var props = blk.DecodeState(state);
                var axis = props.axis.HasValue ? props.axis.Value : Axis.Y;

                bool end =
                    (axis == Axis.Y && (faceIndex == 4 || faceIndex == 5)) ||
                    (axis == Axis.X && (faceIndex == 2 || faceIndex == 3)) ||
                    (axis == Axis.Z && (faceIndex == 0 || faceIndex == 1));

                string key = end ? "end" : "side";
                if (!model.textures.TryGetValue(key, out var tref)) return atlas.GetUV(fallback);
                return atlas.GetUV(TrimBlockPrefix(ResolveTex(model, tref)));
            }

            return atlas.GetUV(fallback);
        }

        public bool UseUVLock(ushort id, byte state) => uvlockAll;

        private static string TrimBlockPrefix(string s)
            => string.IsNullOrEmpty(s) ? "" : (s.StartsWith("block/") ? s[6..] : s);

        private static string ResolveTex(ModelJson m, string refStr)
        {
            if (string.IsNullOrEmpty(refStr)) return "stone";
            if (refStr.StartsWith("#"))
            {
                var key = refStr[1..];
                if (m.textures != null && m.textures.TryGetValue(key, out var resolved))
                    return resolved;
                return "stone";
            }
            return refStr;
        }
    }
}