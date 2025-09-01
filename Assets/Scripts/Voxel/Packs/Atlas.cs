// Assets/Scripts/Voxel/Packs/Atlas.cs
// Ne jamais supprimer les commentaires
using System.Collections.Generic;
using UnityEngine;

namespace Voxel.Packs
{
    /// Atlas pixel-perfect : padding 2 px (dupliqué), UV = zone interne (sans padding), Point/Clamp, sans mipmaps.
    public sealed class Atlas
    {
        private readonly Dictionary<string, Rect> rects = new(System.StringComparer.Ordinal);
        public Texture2D atlas { get; private set; }

        public void Build(Dictionary<string, Texture2D> textures)
        {
            rects.Clear();

            if (textures == null || textures.Count == 0)
            {
                atlas = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                atlas.SetPixels32(new Color32[16]); atlas.Apply(false,false);
                return;
            }

            const int PAD = 2; // 2 px sur chaque bord
            var padded = new List<Texture2D>(textures.Count);
            var keys   = new List<string>(textures.Count);
            var sizes  = new List<Vector2Int>(textures.Count); // taille originale (ex: 16x16)

            foreach (var kv in textures)
            {
                var src = kv.Value;
                int w = src.width, h = src.height;
                sizes.Add(new Vector2Int(w, h));
                keys.Add(kv.Key);

                // Crée une tuile padée (w+4, h+4) en dupliquant les bords
                var dst = new Texture2D(w + PAD*2, h + PAD*2, TextureFormat.RGBA32, false);
                dst.filterMode = FilterMode.Point;
                dst.wrapMode   = TextureWrapMode.Clamp;

                // centre
                dst.SetPixels(PAD, PAD, w, h, src.GetPixels());

                // bandes haut/bas
                for (int x=0;x<w;x++)
                {
                    var cTop = src.GetPixel(x, h-1);
                    var cBot = src.GetPixel(x, 0);
                    for (int p=0;p<PAD;p++)
                    {
                        dst.SetPixel(PAD+x, PAD+h+p, cTop);
                        dst.SetPixel(PAD+x, PAD-1-p, cBot);
                    }
                }
                // bandes gauche/droite
                for (int y=0;y<h;y++)
                {
                    var cL = src.GetPixel(0, y);
                    var cR = src.GetPixel(w-1, y);
                    for (int p=0;p<PAD;p++)
                    {
                        dst.SetPixel(PAD-1-p, PAD+y, cL);
                        dst.SetPixel(PAD+w+p, PAD+y, cR);
                    }
                }
                // coins
                var cTL = src.GetPixel(0,   h-1);
                var cTR = src.GetPixel(w-1, h-1);
                var cBL = src.GetPixel(0,   0);
                var cBR = src.GetPixel(w-1, 0);
                for (int py=0;py<PAD;py++)
                for (int px=0;px<PAD;px++)
                {
                    dst.SetPixel(px,           PAD+h+py, cTL);
                    dst.SetPixel(PAD+w+px,     PAD+h+py, cTR);
                    dst.SetPixel(px,           py,       cBL);
                    dst.SetPixel(PAD+w+px,     py,       cBR);
                }

                dst.Apply(false,false);
                padded.Add(dst);
            }

            // Pack des tuiles padées. Pas de padding PackTextures (on a déjà PAD), pas de mipmaps.
            var at = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
            var rs = at.PackTextures(padded.ToArray(), 0, 4096, false);
            at.filterMode = FilterMode.Point;
            at.wrapMode   = TextureWrapMode.Clamp;
            at.Apply(false,false);
            atlas = at;

            // Pour chaque entrée, retourne le rect **interne** (sans les 2 px de padding)
            for (int i=0;i<keys.Count;i++)
            {
                var full = rs[i];                      // rect UV de la tuile padée
                var size = sizes[i];                   // taille originale (ex: 16x16)
                int wPad = size.x + PAD*2;
                int hPad = size.y + PAD*2;

                // UV par pixel dans l’atlas pour CETTE tuile
                float du = full.width  / wPad;
                float dv = full.height / hPad;

                // Enlève 2 px sur chaque bord (PAD)
                float xMin = full.xMin + du * PAD;
                float yMin = full.yMin + dv * PAD;
                float xMax = full.xMax - du * PAD;
                float yMax = full.yMax - dv * PAD;

                rects[keys[i]] = Rect.MinMaxRect(xMin, yMin, xMax, yMax); // exactement 16x16 visible
            }
        }

        public Rect GetUV(string textureKey)
            => rects.TryGetValue(textureKey, out var uv) ? uv : new Rect(0,0,1,1);
    }
}