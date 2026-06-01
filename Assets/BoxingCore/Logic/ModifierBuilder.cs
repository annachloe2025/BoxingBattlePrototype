namespace BoxingCore
{
    public static class ModifierBuilder
    {
        // attackerSide=1or2。defenderはもう一方。
        public static HitModifiers Build(MatchState s, int attackerSide,
            PunchType attackPunch, DefenseType defenderDefense)
        {
            var m = new HitModifiers();
            var attacker = s.Side(attackerSide);
            var defender = s.Side(attackerSide == 1 ? 2 : 1);
            int ap = (int)attackPunch;
            int dd = (int)defenderDefense;

            // 連打ペナルティ（フック=2/10, ストレート=3/11）。skill変種は Phase2 → [1]固定
            if (ap == 2 || ap == 10)
                m.OnePaternPenalty = Lookup(CombatTables.OnePaternPenaltyVal[1], attacker.HookCount);
            else if (ap == 3 || ap == 11)
                m.OnePaternPenalty = Lookup(CombatTables.OnePaternPenaltyVal[1], attacker.StraightCount);

            // カウンター読みペナルティ（防御側がカウンター 1/5 を選択時）
            if (dd == 1 || dd == 5)
                m.CounterPenalty = Lookup(CombatTables.CounterPenaltyVal[1], defender.CounterCount);

            // コーナー補正：防御側が詰められていれば -5
            if (DistanceSystem.IsCornered(s, attackerSide == 1 ? 2 : 1))
                m.CornerVal = -5;

            // StaminaHosei: 0 スタブ（BANスタミナ消費モデル未抽出。Phase2で実装）
            m.StaminaHosei = 0;
            // Gyakkyou/SkillDodge/SkillGuard/SkillBody/Aisyou/CombiAdd/SioPenalty: 0（Phase2）

            return m;
        }

        // 配列範囲外は末尾値でクランプ
        private static int Lookup(int[] table, int index)
        {
            if (index < 0) index = 0;
            if (index >= table.Length) index = table.Length - 1;
            return table[index];
        }
    }
}
