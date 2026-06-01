namespace BoxingCore
{
    public enum WinKind { Decision, KO, Draw }

    public struct MatchOutcome
    {
        public int Winner;   // 0=draw, 1=P1, 2=P2
        public WinKind Kind;
        public int EndRound;
    }
}
