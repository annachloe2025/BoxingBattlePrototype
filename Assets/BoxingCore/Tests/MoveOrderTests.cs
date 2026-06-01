using NUnit.Framework;
using BoxingCore;

public class MoveOrderTests
{
    [Test] public void Tables_present()
    {
        Assert.AreEqual(21, CombatTables.MovePointVal.Length);
        Assert.AreEqual(1, CombatTables.OffenceSide[1][1]); // subturn1,moveNo1 → P1攻撃1
        Assert.AreEqual(3, CombatTables.OffenceSide[1][6]); // subturn1,moveNo6 → P2攻撃1
        Assert.AreEqual(4, CombatTables.OffenceSide[4][1]); // subturn4,moveNo1 → P2攻撃2
    }

    [Test] public void Faster_p1_gets_moveNo_1()
    {
        var mn = MoveOrder.DecideMoveNo(p1Speed: 30, p2Speed: 0, new FakeRandom(new[]{0.0,0.0,0.0}));
        Assert.AreEqual(1, mn);
    }

    [Test] public void Slower_p1_gets_moveNo_6()
    {
        var mn = MoveOrder.DecideMoveNo(0, 30, new FakeRandom(new[]{0.999,0.999,0.999}));
        Assert.AreEqual(6, mn);
    }

    [Test] public void ActionAt_uses_offence_side_table()
    {
        Assert.AreEqual(1, MoveOrder.ActionAt(subturn:1, moveNo:1));
        Assert.AreEqual(4, MoveOrder.ActionAt(4, 1));
        Assert.AreEqual(1, MoveOrder.ActionAt(3, 6));
    }
}
