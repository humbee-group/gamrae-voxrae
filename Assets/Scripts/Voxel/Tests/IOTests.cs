// Assets/Scripts/Voxel/Tests/IOTests.cs
// Test v4 : Save + Load avec sky/block.

using UnityEngine;
using Voxel.IO;

public sealed class IOTests : MonoBehaviour
{
    void Start()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "iotest.vxsc");

        var ids = new ushort[4096];
        var st  = new byte[4096];
        var sky = new byte[4096];
        var blk = new byte[4096];

        // Écrire (v4)
        LevelStorage.SaveSection(path, ids, st, sky, blk);

        // Relire (v4)
        if (LevelStorage.TryLoadSection(path, out var ids2, out var st2, out var sky2, out var blk2))
        {
            Debug.Log($"IOTests OK: {ids2.Length}/{st2.Length}/{sky2.Length}/{blk2.Length}");
        }
        else
        {
            Debug.LogError("IOTests FAIL: cannot load section.");
        }
    }
}