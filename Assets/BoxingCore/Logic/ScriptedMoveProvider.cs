using System.Collections.Generic;

namespace BoxingCore
{
    // テスト用：与えたコマンドを順に返す（尽きたら最後を繰り返す）
    public sealed class ScriptedMoveProvider : IMoveProvider
    {
        private readonly List<TurnCommands> _cmds;
        private int _i;

        public ScriptedMoveProvider(params TurnCommands[] cmds)
        {
            _cmds = new List<TurnCommands>(cmds);
        }

        public TurnCommands GetCommands(MatchState state, int side)
        {
            if (_cmds.Count == 0)
                return new TurnCommands(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp);
            var c = _cmds[_i < _cmds.Count ? _i : _cmds.Count - 1];
            _i++;
            return c;
        }
    }
}
