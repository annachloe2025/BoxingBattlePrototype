namespace BoxingCore
{
    public sealed class MatchState
    {
        public readonly Fighter[] Fighters; // [0]=P1, [1]=P2

        public Distance Distance = Distance.Far; // 開始時は遠（※BAN開始値は要調整）
        public int Position = 4;                 // 1..7（P1基準。1=P1コーナー,7=P2コーナー,4=中央）

        public int Round = 1, Turn = 1, Subturn = 1;
        public readonly int MaxRound;
        public int MaxTurn = 12;

        public readonly int[] RoundHit = new int[2];   // 判定用・各ラウンドの与弾数(ラウンド毎リセット)
        public readonly int[] JudgeTotal = new int[2];  // 判定スコア累計

        public MatchState(Fighter p1, Fighter p2, int maxRound)
        {
            Fighters = new[] { p1, p2 };
            MaxRound = maxRound;
        }

        public Fighter P1 => Fighters[0];
        public Fighter P2 => Fighters[1];
        // side: 1 or 2 → Fighters index
        public Fighter Side(int side) => Fighters[side - 1];
    }
}
