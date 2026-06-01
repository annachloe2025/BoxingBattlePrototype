namespace BoxingCore
{
    public struct HitModifiers
    {
        // ATTACK 側
        public int OnePaternPenalty, CombiAdd, SkillBody, Aisyou;
        // DEFENSE 側
        public int CornerVal, StaminaHosei, Gyakkyou, CounterPenalty, SioPenalty, SkillDodge, SkillGuard;
    }

    public static class HitResolver
    {
        // ATTACK_point - DEFENSE_point + 10 を 0..20 にクランプ
        public static int ComputeAtkDef(BoxerStats atk, BoxerStats def, PunchType punch,
            DefenseType defense, Distance dist, int opponentHpEval, in HitModifiers m)
        {
            int p = (int)punch;
            var kind = Punches.Kind(punch);
            int atkP = atk.Atk(kind);
            int defP = def.Def(kind);

            // ジャブ(1/9)は攻防差を±4に丸める
            if (p == 1 || p == 9)
            {
                int diff = atkP - defP;
                if (diff >= 4) { atkP = 4; defP = 0; }
                else if (diff <= -4) { atkP = 0; defP = 4; }
            }

            int attack = atkP
                + CombatTables.DistanceVal[(int)dist][p]
                + CombatTables.HpVal[opponentHpEval][p]
                + m.OnePaternPenalty + m.CombiAdd + m.SkillBody + m.Aisyou;

            int defensePoint = defP
                + CombatTables.DefenseVal[(int)defense][p]
                + m.CornerVal + m.StaminaHosei + m.Gyakkyou + m.CounterPenalty
                + m.SioPenalty + m.SkillDodge + m.SkillGuard;

            int atkDef = attack - defensePoint + 10;
            if (atkDef < 0) atkDef = 0;
            if (atkDef > 20) atkDef = 20;
            return atkDef;
        }

        public static double HitProbability(BoxerStats atk, BoxerStats def, PunchType punch,
            DefenseType defense, Distance dist, int opponentHpEval, in HitModifiers m)
        {
            return CombatTables.AttackPointVal[
                ComputeAtkDef(atk, def, punch, defense, dist, opponentHpEval, in m)];
        }

        // 攻撃成功なら true（=offence_success）
        public static bool ResolveHit(BoxerStats atk, BoxerStats def, PunchType punch,
            DefenseType defense, Distance dist, int opponentHpEval, in HitModifiers m, IRandom rng)
        {
            return rng.NextDouble() < HitProbability(atk, def, punch, defense, dist, opponentHpEval, in m);
        }
    }
}
