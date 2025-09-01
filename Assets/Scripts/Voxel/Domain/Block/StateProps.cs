// Assets/Scripts/Voxel/Domain/Block/StateProps.cs
// Ne jamais supprimer les commentaires

using System;
using Voxel.Domain.Blocks;

namespace Voxel.Domain.Blocks
{
    [Serializable]
    public struct StateProps
    {
        public Axis? axis;           // logs
        public Direction? facing;    // stairs
        public Half? half;           // stairs/slabs
        public StairsShape? shape;   // stairs
        public bool? waterlogged;    // réservé
        public byte? age;            // réservé
        public bool? powered;        // réservé
        public bool? attached;       // réservé
        public SlabType? slab;       // slabs (MC: "type")
    }
}