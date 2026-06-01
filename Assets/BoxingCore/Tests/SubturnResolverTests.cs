using NUnit.Framework;
using BoxingCore;

public class SubturnResolverTests
{
    static MatchState Make(BoxerStats p1, BoxerStats p2, Distance d = Distance.Mid)
    {
        var s = new MatchState(new Fighter(p1), new Fighter(p2), 3);
        s.Distance = d; s.Position = 4; return s;
    }
    static BoxerStats S(int hpHead=16,int hpBody=16,int power=15,int tuff=15,
                        int jabA=15,int hookA=15,int strA=15,int upA=15,
                        int jabD=15,int hookD=15,int strD=15,int upD=15,int counter=15,int speed=10)
        => new BoxerStats{HpHead=hpHead,HpBody=hpBody,Power=power,Tuffness=tuff,Counter=counter,Speed=speed,
                          JabAtk=jabA,HookAtk=hookA,StraightAtk=strA,UpperAtk=upA,
                          JabDef=jabD,HookDef=hookD,StraightDef=strD,UpperDef=upD};

    static TurnCommands Cmd(PunchType o1, PunchType o2, DefenseType d) => new TurnCommands(o1,o2,d);

    [Test] public void Landing_jab_damages_head_but_never_downs()
    {
        var s = Make(S(), S(hpHead:1, jabD:8), Distance.Mid); // P2 head max=10, jab防御低
        s.P2.HeadHP = 3;
        var r = SubturnResolver.Resolve(s, action:1,
            p1: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp),
            p2: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp),
            rng: new FakeRandom(new[]{0.0})); // 命中強制
        Assert.IsFalse(r.Ko);
        Assert.AreEqual(1, s.P2.HeadHP);          // ジャブはダウンさせない→1で停止
        Assert.AreEqual(0, s.P2.DownCount);
        Assert.AreEqual(1, s.RoundHit[0]);        // P1の与弾カウント
    }

    [Test] public void Power_punch_zeroing_head_causes_down_and_KO_with_downRule_1()
    {
        var s = Make(S(power:20, upA:20, speed:30), S(hpHead:1, upD:8, tuff:5), Distance.Near);
        s.DownRule = 1; s.P2.HeadHP = 10;
        var r = SubturnResolver.Resolve(s, action:1,
            p1: Cmd(PunchType.Upper, PunchType.Upper, DefenseType.GuardUp),
            p2: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.GuardLow),
            rng: new FakeRandom(new[]{0.0}));
        Assert.IsTrue(r.Ko);
        Assert.AreEqual(1, r.KoWinner);
        Assert.AreEqual(1, s.P2.DownCount);
    }

    [Test] public void Successful_counter_damages_the_attacker()
    {
        var s = Make(S(), S(power:18, counter:17), Distance.Mid);
        s.P1.HeadHP = 160;
        var r = SubturnResolver.Resolve(s, action:1,
            p1: Cmd(PunchType.Straight, PunchType.Jab, DefenseType.GuardUp),
            p2: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.CounterUp),
            rng: new FakeRandom(new[]{0.999})); // 命中失敗→防御(カウンター)成功
        Assert.Less(s.P1.HeadHP, 160);            // P1(攻撃側)が反撃を受けた
        Assert.AreEqual(1, s.RoundHit[1]);        // P2の与弾
    }

    [Test] public void StepIn_changes_distance()
    {
        var s = Make(S(), S(), Distance.Far);
        SubturnResolver.Resolve(s, action:1,
            p1: Cmd(PunchType.StepIn, PunchType.Jab, DefenseType.GuardUp),
            p2: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp),
            rng: new FakeRandom(new[]{0.5}));
        Assert.AreEqual(Distance.Mid, s.Distance);
    }
}
