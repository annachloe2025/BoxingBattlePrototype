using NUnit.Framework;
using BoxingCore;

public class MatchEngineTests
{
    static BoxerStats S(int hpHead=16,int hpBody=16,int power=15,int tuff=15,int speed=10,
                        int jabA=15,int hookA=15,int strA=15,int upA=15,
                        int jabD=15,int hookD=15,int strD=15,int upD=15,int counter=15)
        => new BoxerStats{HpHead=hpHead,HpBody=hpBody,Power=power,Tuffness=tuff,Speed=speed,Counter=counter,
                          JabAtk=jabA,HookAtk=hookA,StraightAtk=strA,UpperAtk=upA,
                          JabDef=jabD,HookDef=hookD,StraightDef=strD,UpperDef=upD};
    static TurnCommands C(PunchType o1, PunchType o2, DefenseType d) => new TurnCommands(o1,o2,d);

    [Test] public void Full_match_runs_to_decision_without_KO()
    {
        var s = new MatchState(new Fighter(S()), new Fighter(S()), maxRound: 1);
        s.Distance = Distance.Far;
        var p1 = new ScriptedMoveProvider(C(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp));
        var p2 = new ScriptedMoveProvider(C(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp));
        var o = MatchEngine.RunMatch(s, p1, p2, new FakeRandom(new[]{0.5}));
        Assert.AreEqual(WinKind.Decision, o.Kind); // ジャブ主体・1ラウンドで判定到達
        Assert.AreEqual(1, o.EndRound);
    }

    [Test] public void Strong_p1_KOs_weak_p2()
    {
        var s = new MatchState(new Fighter(S(power:20, upA:20, speed:30)),
                               new Fighter(S(hpHead:1, upD:8, tuff:3)), maxRound: 3);
        s.Distance = Distance.Near; s.DownRule = 1; s.P2.HeadHP = 10;
        var p1 = new ScriptedMoveProvider(C(PunchType.Upper, PunchType.Upper, DefenseType.GuardUp));
        var p2 = new ScriptedMoveProvider(C(PunchType.Jab, PunchType.Jab, DefenseType.GuardLow));
        var o = MatchEngine.RunMatch(s, p1, p2, new FakeRandom(new[]{0.0}));
        Assert.AreEqual(WinKind.KO, o.Kind);
        Assert.AreEqual(1, o.Winner);
        Assert.AreEqual(1, o.EndRound); // 1ラウンド目で決着
    }

    [Test] public void Basic_ai_match_completes()
    {
        var s = new MatchState(new Fighter(S()), new Fighter(S()), maxRound: 2);
        var rng = new SystemRandom(12345);
        var ai1 = new BasicAiMoveProvider(rng);
        var ai2 = new BasicAiMoveProvider(rng);
        var o = MatchEngine.RunMatch(s, ai1, ai2, rng);
        Assert.IsTrue(o.Winner == 0 || o.Winner == 1 || o.Winner == 2); // 例外なく完走
        Assert.IsTrue(o.EndRound >= 1 && o.EndRound <= 2);
    }
}
