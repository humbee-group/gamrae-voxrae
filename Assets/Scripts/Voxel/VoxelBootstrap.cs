// Assets/Scripts/Voxel/Runtime/VoxelBootstrap.cs
// Appelle l’enregistrement des blocs au démarrage

using UnityEngine;
using Voxel.Domain.Registry;

public sealed class VoxelBootstrap : MonoBehaviour
{
    private void Awake()
    {
        BlockRegister.Init();
    }
}