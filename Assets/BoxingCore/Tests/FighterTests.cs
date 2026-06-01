using NUnit.Framework;
using BoxingCore;

public class FighterTests
{
    static BoxerStats Sample()
    {
        return new BoxerStats {
            Name = "TestA", HpHead = 16, HpBody = 15, Power = 16, Speed = 16,
            Stamina = 15, Tuffness = 15, Counter = 17,
            JabAtk = 18, JabDef = 17, HookAtk = 17, HookDef = 13,
            StraightAtk = 18, StraightDef = 17, UpperAtk = 13, UpperDef = 13
        };
    }

    [Test] public void Hp_is_stat_times_ten_and_starts_full()
    {
        var f = new Fighter(Sample());
        Assert.AreEqual(160, f.HeadHPMax);
        Assert.AreEqual(150, f.BodyHPMax);
        Assert.AreEqual(160, f.HeadHP);
        Assert.AreEqual(150, f.BodyHP);
        Assert.IsTrue(f.TuffnessAvailable);
    }

    [Test] public void Atk_and_def_selectors_by_kind()
    {
        var s = Sample();
        Assert.AreEqual(18, s.Atk(AttackKind.Jab));
        Assert.AreEqual(13, s.Atk(AttackKind.Upper));
        Assert.AreEqual(13, s.Def(AttackKind.Hook));
        Assert.AreEqual(17, s.Def(AttackKind.Straight));
    }
}
