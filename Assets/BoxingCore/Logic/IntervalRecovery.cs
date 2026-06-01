using System;

namespace BoxingCore
{
    public static class IntervalRecovery
    {
        public static void Apply(MatchState s)
        {
            int reduce;
            switch (s.Round)
            {
                case 1: reduce = 4; break;
                case 2: reduce = 6; break;
                case 3: reduce = 8; break;
                case 4: reduce = 10; break;
                default: reduce = 12; break;
            }

            for (int i = 0; i < 2; i++)
            {
                var f = s.Fighters[i];
                int sta = f.Stats.Stamina;
                f.HeadHP = Recover(f.HeadHP, f.HeadHPMax, reduce, sta);
                f.BodyHP = Recover(f.BodyHP, f.BodyHPMax, reduce, sta);
                f.TuffnessAvailable = true; // 毎ラウンド、ダウン回避1回復活
                s.RoundHit[i] = 0;          // 判定用カウントをリセット
            }
        }

        private static int Recover(int hp, int max, int reduce, int stamina)
        {
            int v = hp + (int)Math.Round((max - hp) / (double)reduce) + stamina;
            return v > max ? max : v;
        }
    }
}
