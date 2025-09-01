// Assets/Scripts/Voxel/Packs/Json/BlockstateJson.cs
// Ne jamais supprimer les commentaires
using System;
using System.Collections.Generic;

namespace Voxel.Packs.Json
{
    [Serializable]
    public sealed class BlockstateJson
    {
        [Serializable] public sealed class Variant
        {
            public string model;
            public int? x;
            public int? y;
            public bool? uvlock;
            public int? weight;
        }

        [Serializable] public sealed class Multipart
        {
            [Serializable] public sealed class When { public string AND; }
            [Serializable] public sealed class Apply { public string model; public int? x; public int? y; public bool? uvlock; }
            public When when;
            public Apply apply;
        }

        public Dictionary<string, Variant> variants;
        public Multipart[] multipart;
    }
}