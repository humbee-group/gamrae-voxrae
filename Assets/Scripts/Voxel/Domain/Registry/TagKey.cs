// Assets/Scripts/Voxel/domain/registry/TagKey.cs
using System;

namespace Voxel.Domain.Registry
{
    public readonly struct TagKey : IEquatable<TagKey>
    {
        public readonly string Path; // ex: "mineable/pickaxe"
        public TagKey(string path) { Path = path ?? "unknown"; }
        public override string ToString() => $"#{Path}";
        public bool Equals(TagKey other) => string.Equals(Path, other.Path, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TagKey o && Equals(o);
        public override int GetHashCode() => Path != null ? Path.GetHashCode() : 0;
    }
}