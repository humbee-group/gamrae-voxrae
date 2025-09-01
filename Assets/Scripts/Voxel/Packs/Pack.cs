// Assets/Scripts/Voxel/Packs/Pack.cs
// Ne jamais supprimer les commentaires

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Voxel.Packs.Json;

namespace Voxel.Packs
{
    /// <summary>
    /// Charge un pack "MC-like" depuis un dossier contenant OBLIGATOIREMENT un fichier pack.json.
    /// Arbo attendue:
    ///   pack.json
    ///   blockstates/*.json
    ///   models/block/*.json
    ///   textures/block/*.png
    /// </summary>
    public sealed class Pack
    {
        public readonly string rootPath;

        // Tables chargées en mémoire (clés sans préfixe "block/")
        public readonly Dictionary<string, BlockstateJson> blockstates = new(StringComparer.Ordinal);
        public readonly Dictionary<string, ModelJson> models = new(StringComparer.Ordinal);
        public readonly Dictionary<string, Texture2D> textures = new(StringComparer.Ordinal);

        public Pack(string path) { rootPath = path; }

        public bool Load(out string error)
        {
            try
            {
                error = null;

                // pack.json obligatoire (aucune autre variante)
                var packJson = Path.Combine(rootPath, "pack.json");
                if (!File.Exists(packJson))
                {
                    error = "pack.json missing";
                    return false;
                }

                // (Optionnel) lecture du pack.json pour vérification basique
                // On ne s'appuie sur aucun champ pour l'instant.
                // string meta = File.ReadAllText(packJson);

                blockstates.Clear();
                models.Clear();
                textures.Clear();

                // ---- blockstates/*.json
                var bsDir = Path.Combine(rootPath, "blockstates");
                if (Directory.Exists(bsDir))
                {
                    foreach (var file in Directory.GetFiles(bsDir, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var json = File.ReadAllText(file);
                        var obj = JsonUtility.FromJson<BlockstateJson>(json);
                        var id = Path.GetFileNameWithoutExtension(file); // "stone"
                        blockstates[id] = obj;
                    }
                }

                // ---- models/block/*.json
                var mbDir = Path.Combine(rootPath, "models/block");
                if (Directory.Exists(mbDir))
                {
                    foreach (var file in Directory.GetFiles(mbDir, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var json = File.ReadAllText(file);
                        var obj = JsonUtility.FromJson<ModelJson>(json);
                        var id = Path.GetFileNameWithoutExtension(file); // "stone"
                        models[id] = obj;
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
                        tex.wrapMode = TextureWrapMode.Repeat;
                        textures[Path.GetFileNameWithoutExtension(file)] = tex; // "stone"
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}