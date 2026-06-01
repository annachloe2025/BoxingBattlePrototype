using NUnit.Framework;
using BoxingCore;

public class PunchesTests
{
    [Test] public void Kind_maps_body_and_head_to_same_stat()
    {
        Assert.AreEqual(AttackKind.Jab,      Punches.Kind(PunchType.Jab));
        Assert.AreEqual(AttackKind.Jab,      Punches.Kind(PunchType.BodyJab));
        Assert.AreEqual(AttackKind.Hook,     Punches.Kind(PunchType.BodyHook));
        Assert.AreEqual(AttackKind.Straight, Punches.Kind(PunchType.Straight));
        Assert.AreEqual(AttackKind.Upper,    Punches.Kind(PunchType.BodyUpper));
    }

    [Test] public void Head_and_body_classification()
    {
        Assert.IsTrue(Punches.IsHead(PunchType.Hook));
        Assert.IsFalse(Punches.IsHead(PunchType.BodyHook));
        Assert.IsTrue(Punches.IsBody(PunchType.BodyStraight));
        Assert.IsFalse(Punches.IsBody(PunchType.Straight));
        Assert.IsTrue(Punches.IsAttack(PunchType.Upper));
        Assert.IsFalse(Punches.IsAttack(PunchType.Clinch));
    }
}
