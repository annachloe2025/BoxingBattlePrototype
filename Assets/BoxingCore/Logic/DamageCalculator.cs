using System;

namespace BoxingCore
{
    public static class DamageCalculator
    {
        public static int BaseDamage(PunchType p)
        {
            int n = (int)p;
            if (n == 1 || n == 9) return 5;     // ジャブ／ボディジャブ
            if (n == 4 || n == 12) return 40;   // アッパー／ボディアッパー
            return 20;                          // フック/ストレート（＋ボディ）
        }

        // 通常ダメージ。defenderCountered=防御側がカウンター選択して被弾→×1.3
        public static int Normal(BoxerStats atk, BoxerStats def, PunchType punch, bool defenderCountered)
        {
            int baseD = BaseDamage(punch);
            int add = Math.Max(0, atk.Power - def.Tuffness);
            var kind = Punches.Kind(punch);
            double math = baseD * (1.0 + ((atk.Power + atk.Atk(kind)) / 100.0)) + add;
            int dmg = (int)Math.Floor(math);
            if (defenderCountered) dmg = (int)Math.Floor(dmg * 1.3 + 0.5); // round half up
            return dmg;
        }

        // カウンター成功時。base は「相手が打ってきた技」で決まる
        public static int Counter(BoxerStats counterer, BoxerStats attacker, PunchType attackerPunch)
        {
            int n = (int)attackerPunch;
            int baseD = (n == 1 || n == 6 || n == 8 || n == 9) ? 5
                      : (n == 4 || n == 12) ? 25
                      : 15;
            int add = Math.Max(0, counterer.Power - attacker.Tuffness);
            double math = baseD * (1.0 + ((counterer.Power + counterer.Counter) / 100.0)) + add;
            return (int)Math.Floor(math);
        }
    }
}
