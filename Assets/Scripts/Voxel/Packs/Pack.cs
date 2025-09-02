// Assets/Scripts/Voxel/Packs/Pack.cs
// Loader pack MC-like sans JsonUtility pour les Dictionary. Parse minimal via Regex.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Voxel.Packs.Json;

namespace Voxel.Packs
{
    public sealed class Pack
    {
        public readonly string rootPath;

        // Clés SANS préfixe "block/"
        public readonly Dictionary<string, BlockstateJson> blockstates = new(StringComparer.Ordinal);
        public readonly Dictionary<string, ModelJson>      models      = new(StringComparer.Ordinal);
        public readonly Dictionary<string, Texture2D>      textures    = new(StringComparer.Ordinal);

        public Pack(string path) { rootPath = path; }

        public bool Load(out string error)
        {
            error = null;
            try
            {
                var packJson = Path.Combine(rootPath, "pack.json");
                if (!File.Exists(packJson)) { error = "pack.json missing"; return false; }

                blockstates.Clear(); models.Clear(); textures.Clear();

                // ---- blockstates/*.json -> variants[""].model
                var bsDir = Path.Combine(rootPath, "blockstates");
                if (Directory.Exists(bsDir))
                {
                    foreach (var file in Directory.GetFiles(bsDir, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var txt = File.ReadAllText(file);
                        var id = Path.GetFileNameWithoutExtension(file);     // ex: "grass_block"
                        var model = ExtractModelFromBlockstate(txt);         // ex: "grass_block"

                        var bs = new BlockstateJson
                        {
                            variants = new Dictionary<string, BlockstateJson.Variant>(StringComparer.Ordinal)
                            {
                                [""] = new BlockstateJson.Variant { model = model }
                            }
                        };
                        blockstates[id] = bs;
                    }
                }

                // ---- models/block/*.json -> parent + textures{k:v}
                var mbDir = Path.Combine(rootPath, "models/block");
                if (Directory.Exists(mbDir))
                {
                    foreach (var file in Directory.GetFiles(mbDir, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var txt = File.ReadAllText(file);
                        var id = Path.GetFileNameWithoutExtension(file); // "grass_block"
                        var parent = ExtractParent(txt);                 // "block/cube_all", "block/cube", "block/cube_bottom_top"
                        var texMap = ExtractTextures(txt);               // { "all":"stone", "top":"grass_top", ... }

                        var mj = new ModelJson { parent = parent, textures = texMap, elements = null, ambientocclusion = true };
                        models[id] = mj;
                    }
                }

                // ---- textures/block/*.png
                var txDir = Path.Combine(rootPath, "textures/block");
                if (Directory.Exists(txDir))
                {
                    foreach (var file in Directory.GetFiles(txDir, "*.png", SearchOption.TopDirectoryOnly))
                    {
                        var data = File.ReadAllBytes(file);
                        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        tex.LoadImage(data);
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode   = TextureWrapMode.Clamp;
                        textures[Path.GetFileNameWithoutExtension(file)] = tex; // "grass_top", "grass_side", "dirt", ...
                    }
                }

                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        // ===== Helpers =====

        // variants[""].model ou premier "model":"..."
        static string ExtractModelFromBlockstate(string json)
        {
            var m = Regex.Match(json, @"variants\s*:\s*\{[^}]*?""\s*""\s*:\s*\{\s*""model""\s*:\s*""([^""]+)");
            if (m.Success) return TrimBlock(m.Groups[1].Value);

            m = Regex.Match(json, @"""model""\s*:\s*""([^""]+)""");
            return m.Success ? TrimBlock(m.Groups[1].Value) : "stone";
        }

        static string ExtractParent(string json)
        {
            var m = Regex.Match(json, @"""parent""\s*:\s*""([^""]+)""");
            return m.Success ? m.Groups[1].Value : "block/cube_all";
        }

        static Dictionary<string,string> ExtractTextures(string json)
        {
            var dict = new Dictionary<string,string>(StringComparer.Ordinal);
            var m = Regex.Match(json, @"""textures""\s*:\s*\{([^}]*)\}");
            if (!m.Success) return dict;

            foreach (Match kv in Regex.Matches(m.Groups[1].Value, @"""([^""]+)""\s*:\s*""([^""]+)"""))
            {
                var key = kv.Groups[1].Value; // "all","top","side","bottom"...
                var val = kv.Groups[2].Value; // "grass_top" ou "block/grass_top"
                dict[key] = TrimBlock(val);
            }
            return dict;
        }

        static string TrimBlock(string s) => string.IsNullOrEmpty(s) ? "" : (s.StartsWith("block/") ? s[6..] : s);
    }
}