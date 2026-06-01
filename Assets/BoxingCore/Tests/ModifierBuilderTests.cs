using NUnit.Framework;
using BoxingCore;

public class ModifierBuilderTests
{
    static MatchState State()
        => new MatchState(new Fighter(new BoxerStats{HpHead=1,HpBody=1}),
                          new Fighter(new BoxerStats{HpHead=1,HpBody=1}), 3);

    [Test] public void Hook_repeat_penalty_from_attacker_hookCount()
    {
        var s = State();
        s.P1.HookCount = 12; // OnePaternPenaltyVal[1][12] = -4
        var m = ModifierBuilder.Build(s, attackerSide: 1, PunchType.Hook, DefenseType.GuardUp);
        Assert.AreEqual(-4, m.OnePaternPenalty);
    }

    [Test] public void Counter_read_penalty_from_defender_counterCount()
    {
        var s = State();
        s.P2.CounterCount = 6; // CounterPenaltyVal[1][6] = -6
        // P1が攻撃、P2が上カウンター選択
        var m = ModifierBuilder.Build(s, 1, PunchType.Straight, DefenseType.CounterUp);
        Assert.AreEqual(-6, m.CounterPenalty);
    }

    [Test] public void Corner_penalty_when_defender_cornered()
    {
        var s = State(); s.Position = 7; // P2コーナー
        var m = ModifierBuilder.Build(s, attackerSide: 1, PunchType.Jab, DefenseType.GuardUp);
        Assert.AreEqual(-5, m.CornerVal);
    }

    [Test] public void Stamina_hosei_is_stubbed_zero()
    {
        var s = State();
        var m = ModifierBuilder.Build(s, 1, PunchType.Jab, DefenseType.GuardUp);
        Assert.AreEqual(0, m.StaminaHosei);
    }
}
