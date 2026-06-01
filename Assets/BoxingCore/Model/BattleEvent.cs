namespace BoxingCore
{
    // 1手ぶんの結果＋その時点のスナップショット（UIで再生するためのデータ）
    public struct BattleEvent
    {
        public int Round, Turn;
        public int AttackerSide;     // 1 or 2
        public PunchType Punch;
        public bool IsAttack;        // 攻撃技だったか（移動/回復/クリンチはfalse）
        public bool Hit;             // 攻撃が当たったか
        public bool Countered;       // 防御側のカウンターが決まったか
        public int Damage;
        public bool Downed; public int DownedSide;

        public int P1Head, P1Body, P2Head, P2Body; // この手の後のHP
        public Distance Distance;

        public bool RoundEnd;
        public bool MatchEnd;
        public MatchOutcome Outcome; // MatchEnd時に有効
    }
}
