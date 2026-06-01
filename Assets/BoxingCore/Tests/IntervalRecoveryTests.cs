using NUnit.Framework;
using BoxingCore;

public class IntervalRecoveryTests
{
    [Test] public void Recovers_fraction_plus_stamina_capped_at_max()
    {
        var st = new BoxerStats { HpHead = 16, HpBody = 16, Stamina = 5 };
        var f = new Fighter(st);            // max=160/160
        f.HeadHP = 60; f.BodyHP = 60;
        var s = new MatchState(f, new Fighter(st), 6);
        s.Round = 1;                         // reduce=4
        // head: 60 + round((160-60)/4) + 5 = 60 + 25 + 5 = 90
        IntervalRecovery.Apply(s);
        Assert.AreEqual(90, f.HeadHP);
        Assert.AreEqual(90, f.BodyHP);
    }

    [Test] public void Round5_uses_reduce_12_and_resets_per_round_state()
    {
        var st = new BoxerStats { HpHead = 12, HpBody = 12, Stamina = 0 };
        var f = new Fighter(st); f.HeadHP = 0; f.BodyHP = 0; f.TuffnessAvailable = false;
        var s = new MatchState(f, new Fighter(st), 6);
        s.Round = 5; s.RoundHit[0] = 9;       // reduce=12
        IntervalRecovery.Apply(s);
        // 0 + round(120/12) + 0 = 10
        Assert.AreEqual(10, f.HeadHP);
        Assert.IsTrue(f.TuffnessAvailable);   // 毎ラウンド復活
        Assert.AreEqual(0, s.RoundHit[0]);    // 判定用カウントはリセット
    }
}
