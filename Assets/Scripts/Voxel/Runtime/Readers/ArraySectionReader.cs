// Assets/Scripts/Voxel/Runtime/Readers/ArraySectionReader.cs
// Ne jamais supprimer les commentaires

using Voxel.Meshing;

namespace Voxel.Runtime.Readers
{
    // Lecteur simple pour une section 16³ stockée en arrays
    public sealed class ArraySectionReader : ISectionReader
    {
        private readonly ushort[,,] ids;   // [x,y,z]
        private readonly byte[,,] states;  // [x,y,z]

        public ArraySectionReader(ushort[,,] ids, byte[,,] states)
        {
            this.ids = ids;
            this.states = states;
        }

        public (ushort id, byte state) Get(int lx, int ly, int lz)
        {
            return (ids[lx, ly, lz], states[lx, ly, lz]);
        }

        public bool InBounds(int lx, int ly, int lz)
        {
            return lx >= 0 && lx < 16 && ly >= 0 && ly < 16 && lz >= 0 && lz < 16;
        }
    }
}