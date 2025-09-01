// Assets/Scripts/Voxel/Packs/Json/ModelJson.cs
// Ne jamais supprimer les commentaires
using System;
using System.Collections.Generic;

namespace Voxel.Packs.Json
{
    [Serializable]
    public sealed class ModelJson
    {
        public string parent;
        public Dictionary<string, string> textures;

        [Serializable] public sealed class Element
        {
            public float[] from;
            public float[] to;
            public Dictionary<string, Face> faces;
        }

        [Serializable] public sealed class Face
        {
            public string texture;
            public int? rotation;
            public float[] uv;
        }

        public List<Element> elements;
        public bool ambientocclusion = true;
    }
}