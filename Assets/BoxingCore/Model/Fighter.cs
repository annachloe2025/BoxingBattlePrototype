namespace BoxingCore
{
    public sealed class Fighter
    {
        public readonly BoxerStats Stats;
        public int HeadHP, BodyHP, HeadHPMax, BodyHPMax;
        public int HitCount, CounterCount, HookCount, StraightCount, DownCount, SpPts;
        public bool TuffnessAvailable = true;

        public Fighter(BoxerStats stats)
        {
            Stats = stats;
            HeadHPMax = stats.HpHead * 10;
            BodyHPMax = stats.HpBody * 10;
            HeadHP = HeadHPMax;
            BodyHP = BodyHPMax;
        }
    }
}
