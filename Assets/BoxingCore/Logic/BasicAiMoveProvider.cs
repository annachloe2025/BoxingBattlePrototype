namespace BoxingCore
{
    // 簡易AI：距離と相手HP状態を見た素直なヒューリスティック（忠実AIはPlan3）
    public sealed class BasicAiMoveProvider : IMoveProvider
    {
        private readonly IRandom _rng;
        public BasicAiMoveProvider(IRandom rng) { _rng = rng; }

        public TurnCommands GetCommands(MatchState s, int side)
        {
            var self = s.Side(side);
            var opp = s.Side(side == 1 ? 2 : 1);
            int oppEval = HpEval.Evaluate(opp.HeadHP, opp.HeadHPMax, opp.BodyHP, opp.BodyHPMax);
            int selfEval = HpEval.Evaluate(self.HeadHP, self.HeadHPMax, self.BodyHP, self.BodyHPMax);

            PunchType atk1, atk2;
            switch (s.Distance)
            {
                case Distance.Far:
                    atk1 = PunchType.Straight; atk2 = PunchType.StepIn; break;
                case Distance.Mid:
                    atk1 = (oppEval <= 3) ? PunchType.Hook : PunchType.Jab;
                    atk2 = PunchType.Straight; break;
                default: // Near
                    atk1 = (oppEval <= 3) ? PunchType.Upper : PunchType.Jab;
                    atk2 = PunchType.BodyHook; break;
            }

            // 体力ピンチで時々クリンチ
            if (selfEval <= 2 && _rng.NextDouble() < 0.3) atk1 = PunchType.Clinch;

            DefenseType def;
            double r = _rng.NextDouble();
            if (r < 0.4) def = DefenseType.GuardUp;
            else if (r < 0.7) def = DefenseType.GuardLow;
            else if (r < 0.85) def = DefenseType.Sway;
            else def = DefenseType.CounterUp;

            return new TurnCommands(atk1, atk2, def);
        }
    }
}
