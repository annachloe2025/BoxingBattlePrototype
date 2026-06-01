using NUnit.Framework;
using BoxingCore;

public class DamageCalculatorTests
{
    static BoxerStats Atk() => new BoxerStats { Power=16, JabAtk=18, UpperAtk=13, StraightAtk=18, HookAtk=17, Counter=17 };
    static BoxerStats Def() => new BoxerStats { Tuffness=15 };

    [Test] public void Base_damage_by_punch()
    {
        Assert.AreEqual(5,  DamageCalculator.BaseDamage(PunchType.Jab));
        Assert.AreEqual(5,  DamageCalculator.BaseDamage(PunchType.BodyJab));
        Assert.AreEqual(20, DamageCalculator.BaseDamage(PunchType.Hook));
        Assert.AreEqual(20, DamageCalculator.BaseDamage(PunchType.Straight));
        Assert.AreEqual(40, DamageCalculator.BaseDamage(PunchType.Upper));
        Assert.AreEqual(40, DamageCalculator.BaseDamage(PunchType.BodyUpper));
    }

    [Test] public void Jab_normal_damage()
    {
        // 5*(1+(16+18)/100)+max(0,16-15)=5*1.34+1=7.7 -> floor 7
        Assert.AreEqual(7, DamageCalculator.Normal(Atk(), Def(), PunchType.Jab, defenderCountered:false));
    }

    [Test] public void Upper_normal_damage()
    {
        // 40*(1+(16+13)/100)+1 = 40*1.29+1 = 51.6+1 = 52.6 -> floor 52
        Assert.AreEqual(52, DamageCalculator.Normal(Atk(), Def(), PunchType.Upper, false));
    }

    [Test] public void Counter_failure_multiplies_by_1_3_round_half_up()
    {
        // 通常7 → ×1.3 = 9.1 → round 9
        Assert.AreEqual(9, DamageCalculator.Normal(Atk(), Def(), PunchType.Jab, defenderCountered:true));
    }

    [Test] public void Counter_success_damage_uses_counter_stat()
    {
        // 相手がストレート(3)を打ってきたのをカウンター成功: base=15
        // 15*(1+(16+17)/100)+max(0,16-15)=15*1.33+1=19.95+1=20.95 -> floor 20
        Assert.AreEqual(20, DamageCalculator.Counter(Atk(), Def(), attackerPunch: PunchType.Straight));
    }
}
