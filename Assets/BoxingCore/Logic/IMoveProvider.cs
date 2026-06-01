namespace BoxingCore
{
    // 各ターン、side(1 or 2) のボクサーの手を返す
    public interface IMoveProvider
    {
        TurnCommands GetCommands(MatchState state, int side);
    }
}
