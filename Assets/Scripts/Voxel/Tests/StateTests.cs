// Assets/Scripts/Voxel/Tests/StateTests.cs
// Corrige le namespace (plus de .Examples)

#if UNITY_EDITOR
using NUnit.Framework;
using Voxel.Domain.Blocks;

public class StateTests
{
    [Test]
    public void StateKey_Order_IsCanonical()
    {
        var p = new StateProps
        {
            facing = Direction.East,
            half = Half.Top,
            shape = StairsShape.InnerLeft,
            waterlogged = false
        };
        var key = StateKeyBuilder.Build(p);
        Assert.AreEqual("facing=east,half=top,shape=inner_left,waterlogged=false", key);
    }

    [Test]
    public void Slab_EncodeDecode_Roundtrip()
    {
        var slab = new SlabBlock();
        var p = new StateProps { slab = SlabType.Top };
        var st = slab.EncodeState(p);
        var back = slab.DecodeState(st);
        Assert.AreEqual(SlabType.Top, back.slab);
    }
}
#endif