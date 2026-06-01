namespace BoxingCore
{
    public static class MoveOrder
    {
        // 行動順番号 1..6 を決める（1=P1最速 … 6=P2最速）
        public static int DecideMoveNo(int p1Speed, int p2Speed, IRandom rng)
        {
            int spd = p1Speed - p2Speed + 10;
            if (spd < 0) spd = 0;
            if (spd > 20) spd = 20;
            double mp = CombatTables.MovePointVal[spd];

            bool s1 = mp + rng.NextDouble() >= 1.0;
            bool s2 = mp + rng.NextDouble() >= 1.0;
            bool s3 = mp + rng.NextDouble() >= 1.0;

            if (s1 && s2) return 1;
            if (s1 && !s2 && s3) return 2;
            if (s1 && !s2 && !s3) return 3;
            if (!s1 && s2 && s3) return 4;
            if (!s1 && s2 && !s3) return 5;
            return 6; // !s1 && !s2
        }

        // subturn(1..4), moveNo(1..6) → 行動(1=P1攻1,2=P1攻2,3=P2攻1,4=P2攻2)
        public static int ActionAt(int subturn, int moveNo)
            => CombatTables.OffenceSide[subturn][moveNo];
    }
}
