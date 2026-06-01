using System;

namespace BoxingCore
{
    public static class Judgment
    {
        // そのラウンドのスコアを JudgeTotal に加算する
        public static void ScoreRound(MatchState s)
        {
            int p1Hit = s.RoundHit[0], p2Hit = s.RoundHit[1];
            int p1Down = s.P1.DownCount, p2Down = s.P2.DownCount;

            int p1, p2;
            if (p1Hit >= p2Hit)
            {
                p1 = 10;
                p2 = (int)Math.Floor((p2Hit + 20 + p1Down * 5) / (double)(p1Hit + 20 + p2Down * 5) * 10);
                if (p2 >= 10) { p1 = 8; p2 = 10; }
            }
            else
            {
                p2 = 10;
                p1 = (int)Math.Floor((p1Hit + 20 + p2Down * 5) / (double)(p2Hit + 20 + p1Down * 5) * 10);
                if (p1 >= 10) { p2 = 8; p1 = 10; }
            }
            s.JudgeTotal[0] += p1;
            s.JudgeTotal[1] += p2;
        }

        // 試合終了時の勝者。ko: 0=判定, 1=P1のKO勝ち, 2=P2のKO勝ち
        public static MatchOutcome Decide(MatchState s, int endRound, int ko)
        {
            if (ko == 1) return new MatchOutcome { Winner = 1, Kind = WinKind.KO, EndRound = endRound };
            if (ko == 2) return new MatchOutcome { Winner = 2, Kind = WinKind.KO, EndRound = endRound };

            int w;
            if (s.JudgeTotal[0] > s.JudgeTotal[1]) w = 1;
            else if (s.JudgeTotal[0] < s.JudgeTotal[1]) w = 2;
            else w = 0;
            return new MatchOutcome { Winner = w, Kind = w == 0 ? WinKind.Draw : WinKind.Decision, EndRound = endRound };
        }
    }
}
