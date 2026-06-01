using System;

namespace BoxingCore
{
    public sealed class SubturnResult
    {
        public int AttackerSide;
        public PunchType Punch;
        public bool IsAttack;
        public bool Hit;
        public bool Countered;
        public int Damage;
        public bool Downed; public int DownedSide;
        public bool Ko; public int KoWinner; // 1 or 2
    }

    public static class SubturnResolver
    {
        // action: 1=P1攻1,2=P1攻2,3=P2攻1,4=P2攻2
        public static SubturnResult Resolve(MatchState s, int action,
            TurnCommands p1, TurnCommands p2, IRandom rng)
        {
            var res = new SubturnResult();

            int atkSide = (action == 1 || action == 2) ? 1 : 2;
            int defSide = atkSide == 1 ? 2 : 1;
            TurnCommands atkCmd = atkSide == 1 ? p1 : p2;
            TurnCommands defCmd = defSide == 1 ? p1 : p2;
            PunchType punch = (action == 1 || action == 3) ? atkCmd.Offence1 : atkCmd.Offence2;
            DefenseType defense = defCmd.Defense;
            int pn = (int)punch;

            res.AttackerSide = atkSide;
            res.Punch = punch;

            // 非攻撃（移動/回復/クリンチ）
            if (pn == 6) { DistanceSystem.StepIn(s, atkSide); return res; }
            if (pn == 5) { DistanceSystem.StepBack(s, atkSide); return res; }
            if (pn == 7 || pn == 8) { Recover(s.Side(atkSide)); return res; } // 回復/クリンチ簡略

            res.IsAttack = true;
            var attacker = s.Side(atkSide);
            var defender = s.Side(defSide);

            UpdatePatternCount(attacker, pn); // 連打カウント更新

            int defEval = HpEval.Evaluate(defender.HeadHP, defender.HeadHPMax, defender.BodyHP, defender.BodyHPMax);
            HitModifiers mods = ModifierBuilder.Build(s, atkSide, punch, defense);
            bool hit = HitResolver.ResolveHit(attacker.Stats, defender.Stats, punch, defense, s.Distance, defEval, mods, rng);
            bool defenderCountered = (defense == DefenseType.CounterUp || defense == DefenseType.CounterLow);

            if (hit)
            {
                res.Hit = true;
                int dmg = DamageCalculator.Normal(attacker.Stats, defender.Stats, punch, defenderCountered);
                res.Damage = dmg;
                ApplyDamage(s, defender, defSide, pn, dmg, res, koWinner: atkSide);
                attacker.HitCount++; attacker.SpPts++; s.RoundHit[atkSide - 1]++;
            }
            else if (defenderCountered)
            {
                // カウンター成功：攻撃側が反撃を受ける（上カウンター=頭, 下=腹）
                res.Countered = true;
                int cdmg = DamageCalculator.Counter(defender.Stats, attacker.Stats, punch);
                res.Damage = cdmg;
                bool toHead = (defense == DefenseType.CounterUp);
                ApplyCounterDamage(s, attacker, atkSide, toHead, cdmg, res, koWinner: defSide);
                defender.HitCount++; defender.SpPts++; s.RoundHit[defSide - 1]++;
            }
            // guard/sway/duck の防御成功はダメージ無し（guard chip は Phase2）

            return res;
        }

        private static void UpdatePatternCount(Fighter f, int pn)
        {
            if (pn == 2 || pn == 10) { f.HookCount++; f.StraightCount = 0; }
            else if (pn == 3 || pn == 11) { f.StraightCount++; f.HookCount = 0; }
            else { f.HookCount = 0; f.StraightCount = 0; }
        }

        private static void Recover(Fighter f)
        {
            f.HeadHP = Math.Min(f.HeadHPMax, f.HeadHP + 5);
            f.BodyHP = Math.Min(f.BodyHPMax, f.BodyHP + 5);
        }

        private static void ApplyDamage(MatchState s, Fighter defender, int defSide, int pn, int dmg,
            SubturnResult res, int koWinner)
        {
            bool headPunch = pn >= 1 && pn <= 4;
            if (headPunch)
            {
                defender.HeadHP -= dmg;
                if (defender.HeadHP <= 0)
                {
                    if (pn == 1) defender.HeadHP = 1;                // ジャブはダウンさせない
                    else { defender.HeadHP = 0; Down(s, defender, true, res, koWinner, defSide); }
                }
            }
            else // body 9..12
            {
                defender.BodyHP -= dmg;
                if (defender.BodyHP <= 0)
                {
                    if (pn == 9) defender.BodyHP = 1;                // ボディジャブはダウンさせない
                    else { defender.BodyHP = 0; Down(s, defender, false, res, koWinner, defSide); }
                }
            }
        }

        private static void ApplyCounterDamage(MatchState s, Fighter target, int targetSide, bool toHead, int dmg,
            SubturnResult res, int koWinner)
        {
            if (toHead)
            {
                target.HeadHP -= dmg;
                if (target.HeadHP <= 0) { target.HeadHP = 0; Down(s, target, true, res, koWinner, targetSide); }
            }
            else
            {
                target.BodyHP -= dmg;
                if (target.BodyHP <= 0) { target.BodyHP = 0; Down(s, target, false, res, koWinner, targetSide); }
            }
        }

        // ダウン処理（Plan2B簡略：downRuleで決定、立ち上がりはBAN式HP復帰）
        private static void Down(MatchState s, Fighter f, bool headZeroed, SubturnResult res, int koWinner, int downedSide)
        {
            f.DownCount++;
            res.Downed = true; res.DownedSide = downedSide;
            if (f.DownCount >= s.DownRule) { res.Ko = true; res.KoWinner = koWinner; return; }

            int sta = f.Stats.Stamina;
            if (headZeroed)
                f.HeadHP = Math.Min(f.HeadHPMax, (int)Math.Round(f.BodyHP / 2.0) + sta);
            else
                f.BodyHP = Math.Min(f.BodyHPMax, (int)Math.Round(f.HeadHP / 2.0) + sta);

            if (f.HeadHP <= 0) f.HeadHP = 1;
            if (f.BodyHP <= 0) f.BodyHP = 1;
        }
    }
}
