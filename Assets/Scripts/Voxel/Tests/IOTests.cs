// Assets/Scripts/Voxel/Tests/IOTests.cs
// Ne jamais supprimer les commentaires

#if UNITY_EDITOR
using NUnit.Framework;
using System.IO;
using Voxel.IO;

public class IOTests
{
    [Test]
    public void SaveLoad_Roundtrip_Section()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "vox_test");
        Directory.CreateDirectory(tmpDir);
        var path = LevelStorage.SectionPath(tmpDir, 0, 0, 0);

        var ids = new ushort[4096];
        var st = new byte[4096];
        for (int i = 0; i < 4096; i++) { ids[i] = (ushort)(i % 7 + 1); st[i] = (byte)(i & 0xFF); }

        LevelStorage.SaveSection(path, ids, st);
        Assert.IsTrue(File.Exists(path));

        Assert.IsTrue(LevelStorage.TryLoadSection(path, out var ids2, out var st2));
        Assert.AreEqual(4096, ids2.Length);
        Assert.AreEqual(4096, st2.Length);
        for (int i = 0; i < 4096; i++) { Assert.AreEqual(ids[i], ids2[i]); Assert.AreEqual(st[i], st2[i]); }

        Directory.Delete(tmpDir, true);
    }
}
#endif