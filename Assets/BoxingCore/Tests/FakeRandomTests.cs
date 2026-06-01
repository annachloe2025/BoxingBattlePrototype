using NUnit.Framework;
using BoxingCore;

public class FakeRandomTests
{
    [Test] public void FakeRandom_returns_queued_doubles_in_order()
    {
        var r = new FakeRandom(new double[] { 0.1, 0.9 }, new int[] { 2 });
        Assert.AreEqual(0.1, r.NextDouble(), 1e-9);
        Assert.AreEqual(0.9, r.NextDouble(), 1e-9);
        Assert.AreEqual(2, r.Next(10));
    }

    [Test] public void FakeRandom_loops_when_exhausted()
    {
        var r = new FakeRandom(new double[] { 0.5 }, new int[] { 0 });
        Assert.AreEqual(0.5, r.NextDouble(), 1e-9);
        Assert.AreEqual(0.5, r.NextDouble(), 1e-9); // 巡回
    }
}
