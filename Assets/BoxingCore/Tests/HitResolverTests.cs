using NUnit.Framework;
using BoxingCore;

public class HitResolverTests
{
    static BoxerStats Stat(int jabA=15,int jabD=15,int strA=15,int strD=15,int upA=15,int upD=15)
        => new BoxerStats { JabAtk=jabA, JabDef=jabD, StraightAtk=strA, StraightDef=strD,
                            UpperAtk=upA, UpperDef=upD, HookAtk=15, HookDef=15 };

    [Test] public void Straight_far_vs_healthy_upperguard_is_zero()
    {
        // ATTACK=15+distance(+2)+hp(-10)=7  DEFENSE=15+defense(+5)=20  ATK_DEF=clamp(-3)=0 -> 0%
        var p = HitResolver.HitProbability(Stat(), Stat(), PunchType.Straight, DefenseType.GuardUp,
                                           Distance.Far, opponentHpEval: 10, default);
        Assert.AreEqual(0.0, p, 1e-9);
    }

    [Test] public void Jab_mid_vs_healthy_upperguard_is_25pct()
    {
        // ATTACK=15  DEFENSE=15+5=20  ATK_DEF=5 -> 0.25
        var p = HitResolver.HitProbability(Stat(), Stat(), PunchType.Jab, DefenseType.GuardUp,
                                           Distance.Mid, 10, default);
        Assert.AreEqual(0.25, p, 1e-9);
    }

    [Test] public void Jab_difference_is_clamped_to_plus_minus_4()
    {
        // jabAtk20 vs jabDef10 → 差10だが±4に丸め: ATK_p=4,DEF_p=0
        // ATTACK=4+dist(0)+hp(0)=4  DEFENSE=0+defense(Sway,jab=0)=0  ATK_DEF=clamp(14)=14 -> 0.9
        var atk = Stat(jabA:20); var def = Stat(jabD:10);
        var p = HitResolver.HitProbability(atk, def, PunchType.Jab, DefenseType.Sway,
                                           Distance.Mid, 10, default);
        Assert.AreEqual(0.9, p, 1e-9);
    }

    [Test] public void Upper_near_vs_hurt_wrongguard_is_50pct()
    {
        // ATTACK=15+dist(-3)+hp(0)=12  DEFENSE=15+defense(GuardLow,upper=-3)=12  ATK_DEF=10 -> 0.5
        var p = HitResolver.HitProbability(Stat(), Stat(), PunchType.Upper, DefenseType.GuardLow,
                                           Distance.Near, opponentHpEval: 1, default);
        Assert.AreEqual(0.5, p, 1e-9);
    }

    [Test] public void ResolveHit_uses_rng_threshold()
    {
        // 命中率0.5。乱数0.4<0.5→命中、0.6→防御成功
        var hit = HitResolver.ResolveHit(Stat(), Stat(), PunchType.Upper, DefenseType.GuardLow,
                                         Distance.Near, 1, default, new FakeRandom(new[]{0.4}));
        Assert.IsTrue(hit);
        var miss = HitResolver.ResolveHit(Stat(), Stat(), PunchType.Upper, DefenseType.GuardLow,
                                          Distance.Near, 1, default, new FakeRandom(new[]{0.6}));
        Assert.IsFalse(miss);
    }
}
