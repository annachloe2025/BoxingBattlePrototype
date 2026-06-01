using System.Collections.Generic;

namespace BoxingCore
{
    public static class MatchEngine
    {
        // log を渡すと各手のイベントを記録（UI再生用）。null なら従来どおり結果だけ返す。
        public static MatchOutcome RunMatch(MatchState s,
            IMoveProvider p1Provider, IMoveProvider p2Provider, IRandom rng,
            List<BattleEvent> log = null)
        {
            for (s.Round = 1; s.Round <= s.MaxRound; s.Round++)
            {
                for (s.Turn = 1; s.Turn <= s.MaxTurn; s.Turn++)
                {
                    TurnCommands p1 = p1Provider.GetCommands(s, 1);
                    TurnCommands p2 = p2Provider.GetCommands(s, 2);

                    int moveNo = MoveOrder.DecideMoveNo(s.P1.Stats.Speed, s.P2.Stats.Speed, rng);

                    for (s.Subturn = 1; s.Subturn <= 4; s.Subturn++)
                    {
                        int action = MoveOrder.ActionAt(s.Subturn, moveNo);
                        var r = SubturnResolver.Resolve(s, action, p1, p2, rng);
                        if (log != null) log.Add(MakeEvent(s, r, false, false, default));
                        if (r.Ko)
                        {
                            var ko = Judgment.Decide(s, s.Round, r.KoWinner);
                            if (log != null) log.Add(MakeEvent(s, null, false, true, ko));
                            return ko;
                        }
                    }

                    UpdateCounterCount(s.P1, p1.Defense);
                    UpdateCounterCount(s.P2, p2.Defense);
                }

                Judgment.ScoreRound(s);
                if (log != null) log.Add(MakeEvent(s, null, true, false, default));
                if (s.Round < s.MaxRound) IntervalRecovery.Apply(s);
            }

            var fin = Judgment.Decide(s, s.MaxRound, 0);
            if (log != null) log.Add(MakeEvent(s, null, false, true, fin));
            return fin;
        }

        // 1ターン(4サブターン)を解決。KOが起きたら Ko=true の結果を返す（events に各手を追記）。
        // ラウンド/ターンの進行・判定・回復は呼び出し側（操作版のコントローラ）が管理する。
        public static SubturnResult RunTurn(MatchState s, TurnCommands p1, TurnCommands p2,
            IRandom rng, List<BattleEvent> events)
        {
            int moveNo = MoveOrder.DecideMoveNo(s.P1.Stats.Speed, s.P2.Stats.Speed, rng);
            for (s.Subturn = 1; s.Subturn <= 4; s.Subturn++)
            {
                int action = MoveOrder.ActionAt(s.Subturn, moveNo);
                var r = SubturnResolver.Resolve(s, action, p1, p2, rng);
                if (events != null) events.Add(MakeEvent(s, r, false, false, default));
                if (r.Ko) return r;
            }
            UpdateCounterCount(s.P1, p1.Defense);
            UpdateCounterCount(s.P2, p2.Defense);
            return new SubturnResult(); // KOなし
        }

        private static BattleEvent MakeEvent(MatchState s, SubturnResult r, bool roundEnd, bool matchEnd, MatchOutcome outcome)
        {
            var e = new BattleEvent
            {
                Round = s.Round, Turn = s.Turn, Distance = s.Distance,
                P1Head = s.P1.HeadHP, P1Body = s.P1.BodyHP,
                P2Head = s.P2.HeadHP, P2Body = s.P2.BodyHP,
                RoundEnd = roundEnd, MatchEnd = matchEnd, Outcome = outcome
            };
            if (r != null)
            {
                e.AttackerSide = r.AttackerSide; e.Punch = r.Punch; e.IsAttack = r.IsAttack;
                e.Hit = r.Hit; e.Countered = r.Countered; e.Damage = r.Damage;
                e.Downed = r.Downed; e.DownedSide = r.DownedSide;
            }
            return e;
        }

        private static void UpdateCounterCount(Fighter f, DefenseType def)
        {
            if (def == DefenseType.CounterUp || def == DefenseType.CounterLow) f.CounterCount++;
            else f.CounterCount = 0;
        }
    }
}
