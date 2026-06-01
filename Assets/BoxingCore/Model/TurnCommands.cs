namespace BoxingCore
{
    // 1ターンで各ボクサーが選ぶ手（攻撃2＋防御1）
    public struct TurnCommands
    {
        public PunchType Offence1;
        public PunchType Offence2;
        public DefenseType Defense;

        public TurnCommands(PunchType offence1, PunchType offence2, DefenseType defense)
        {
            Offence1 = offence1;
            Offence2 = offence2;
            Defense = defense;
        }
    }
}
