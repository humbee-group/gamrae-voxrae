// Assets/Scripts/Voxel/runtime/StairUtils.cs
using Voxel.Domain.Blocks;

namespace Voxel.Runtime
{
    public static class StairUtils
    {
        public static Direction TurnLeft(Direction d) => d switch
        {
            Direction.North => Direction.West,
            Direction.West => Direction.South,
            Direction.South => Direction.East,
            Direction.East => Direction.North,
            _ => d
        };

        public static Direction TurnRight(Direction d) => d switch
        {
            Direction.North => Direction.East,
            Direction.East => Direction.South,
            Direction.South => Direction.West,
            Direction.West => Direction.North,
            _ => d
        };
    }
}