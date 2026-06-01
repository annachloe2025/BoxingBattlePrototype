using NUnit.Framework;
using BoxingCore;

public class JudgmentTests
{
    static MatchState State()
        => new MatchState(new Fighter(new BoxerStats{HpHead=1,HpBody=1}),
                          new Fighter(new BoxerStats{HpHead=1,HpBody=1}), 3);

    [Test] public void Round_winner_gets_10_loser_proportional()
    {
        var s = State();
        s.RoundHit[0] = 10; s.RoundHit[1] = 5;   // P1が多く当てた
        s.P1.DownCount = 0;  s.P2.DownCount = 0;
        Judgment.ScoreRound(s);
        // P1=10。P2 = floor((5+20+0)/(10+20+0)*10)= floor(25/30*10)=floor(8.33)=8
        Assert.AreEqual(10, s.JudgeTotal[0]);
        Assert.AreEqual(8,  s.JudgeTotal[1]);
    }

    [Test] public void Accumulates_across_rounds_and_decides_winner()
    {
        var s = State();
        s.RoundHit[0]=10; s.RoundHit[1]=5; Judgment.ScoreRound(s); // R1: P1=10, P2=8 → 累計10,8
        s.RoundHit[0]=8;  s.RoundHit[1]=9; Judgment.ScoreRound(s); // R2: P2=10, P1=floor(28/29*10)=9 → 累計19,18
        var o = Judgment.Decide(s, endRound: 2, ko: 0);
        Assert.AreEqual(1, o.Winner); // 19 > 18 で P1 勝ち
        Assert.AreEqual(WinKind.Decision, o.Kind);
    }
}
