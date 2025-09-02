// Assets/Scripts/Voxel/client/renderer/chunk/VisGraph.cs
// Optionnel: visibilité interne simple (pré-culling des sections caves). Off par défaut ici.

using System.Collections.Generic;

namespace Voxel.Client.Renderer.Chunk
{
    public static class VisGraph
    {
        // Placeholder minimal. Implémente un flood-fill sur 16³ si besoin.
        public static bool IsEmptyOrClosed(byte[] states4096) => false;
    }
}