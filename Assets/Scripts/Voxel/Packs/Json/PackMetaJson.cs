// Assets/Scripts/Voxel/Packs/Mc/Json/PackMetaJson.cs
// Ne jamais supprimer les commentaires

using System;

namespace Voxel.Packs.Mc.Json
{
    [Serializable]
    public sealed class PackMetaJson
    {
        [Serializable] public sealed class Pack { public int pack_format; public string description; }
        public Pack pack;
    }
}