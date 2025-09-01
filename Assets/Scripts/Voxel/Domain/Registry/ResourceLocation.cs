// Assets/Scripts/Voxel/domain/registry/ResourceLocation.cs
using System;

namespace Voxel.Domain.Registry
{
    public readonly struct ResourceLocation : IEquatable<ResourceLocation>
    {
        public readonly string Namespace;
        public readonly string Path;

        public ResourceLocation(string ns, string path)
        {
            Namespace = ns ?? "minecraft";
            Path = path ?? "unknown";
        }

        public static ResourceLocation Parse(string id)
        {
            if (string.IsNullOrEmpty(id)) return new("minecraft", "unknown");
            var idx = id.IndexOf(':');
            return idx > 0 ? new(id[..idx], id[(idx + 1)..]) : new("minecraft", id);
        }

        public override string ToString() => $"{Namespace}:{Path}";
        public bool Equals(ResourceLocation other) => string.Equals(Namespace, other.Namespace, StringComparison.Ordinal) && string.Equals(Path, other.Path, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ResourceLocation o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(Namespace, Path);
    }
}