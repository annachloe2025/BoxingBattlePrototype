using NUnit.Framework;
using BoxingCore;

public class DistanceSystemTests
{
    static MatchState State(Distance d = Distance.Mid, int pos = 4)
    {
        var s = new MatchState(new Fighter(new BoxerStats{HpHead=1,HpBody=1}),
                               new Fighter(new BoxerStats{HpHead=1,HpBody=1}), 3);
        s.Distance = d; s.Position = pos; return s;
    }

    [Test] public void P1_stepin_closes_distance_and_advances()
    {
        var s = State(Distance.Far, 4);
        DistanceSystem.StepIn(s, 1);
        Assert.AreEqual(Distance.Mid, s.Distance);
        Assert.AreEqual(5, s.Position);
    }

    [Test] public void P1_stepin_at_near_only_pushes_position()
    {
        var s = State(Distance.Near, 4);
        DistanceSystem.StepIn(s, 1);
        Assert.AreEqual(Distance.Near, s.Distance);
        Assert.AreEqual(5, s.Position);
    }

    [Test] public void P2_stepin_at_mid_only_closes_distance()
    {
        var s = State(Distance.Mid, 4);
        DistanceSystem.StepIn(s, 2);
        Assert.AreEqual(Distance.Near, s.Distance);
        Assert.AreEqual(4, s.Position); // P2前進では位置不変(dist>1)
    }

    [Test] public void P1_stepback_at_own_corner_is_blocked()
    {
        var s = State(Distance.Mid, 1);
        DistanceSystem.StepBack(s, 1);
        Assert.AreEqual(Distance.Mid, s.Distance);
        Assert.AreEqual(1, s.Position);
    }

    [Test] public void P2_cornered_detection()
    {
        Assert.IsTrue(DistanceSystem.IsCornered(State(Distance.Far, 7), 2));
        Assert.IsTrue(DistanceSystem.IsCornered(State(Distance.Far, 5), 2));
        Assert.IsTrue(DistanceSystem.IsCornered(State(Distance.Mid, 1), 1));
        Assert.IsFalse(DistanceSystem.IsCornered(State(Distance.Mid, 4), 2));
    }
}
