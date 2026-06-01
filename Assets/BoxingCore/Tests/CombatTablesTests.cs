using NUnit.Framework;
using BoxingCore;

public class CombatTablesTests
{
    [Test] public void AttackPointVal_curve_key_points()
    {
        Assert.AreEqual(0.0,  CombatTables.AttackPointVal[0],  1e-9);
        Assert.AreEqual(0.5,  CombatTables.AttackPointVal[10], 1e-9); // 互角=50%
        Assert.AreEqual(1.0,  CombatTables.AttackPointVal[15], 1e-9); // 差+5で必中
        Assert.AreEqual(21,   CombatTables.AttackPointVal.Length);
    }

    [Test] public void Distance_table_key_values()
    {
        // 遠距離(3)：ストレート(3)=+2, アッパー(4)=-15
        Assert.AreEqual(2,   CombatTables.DistanceVal[(int)Distance.Far][(int)PunchType.Straight]);
        Assert.AreEqual(-15, CombatTables.DistanceVal[(int)Distance.Far][(int)PunchType.Upper]);
        // 近距離(1)：ストレート=-10
        Assert.AreEqual(-10, CombatTables.DistanceVal[(int)Distance.Near][(int)PunchType.Straight]);
    }

    [Test] public void HpVal_healthy_opponent_penalizes_power_punches_but_not_jab()
    {
        Assert.AreEqual(0,   CombatTables.HpVal[10][(int)PunchType.Jab]);      // ジャブは常に0
        Assert.AreEqual(-10, CombatTables.HpVal[10][(int)PunchType.Straight]); // 健全相手に強打-10
        Assert.AreEqual(-15, CombatTables.HpVal[10][(int)PunchType.Upper]);
    }

    [Test] public void DefenseVal_guard_strong_on_matching_height()
    {
        Assert.AreEqual(5,  CombatTables.DefenseVal[(int)DefenseType.GuardUp][(int)PunchType.Hook]);     // 上ガード×頭+5
        Assert.AreEqual(-3, CombatTables.DefenseVal[(int)DefenseType.GuardUp][(int)PunchType.BodyHook]); // 上ガードはボディに甘い
    }
}
