using System.Collections.Generic;
using UnityEngine;
using BoxingCore;

// 操作版：あなた(RED/P1)がコマンドを選び、CPU(BLUE/P2)と戦う。
// 空のGameObjectに付けて Play。OnGUIなのでシーン設定不要。
public class PlayableBattleController : MonoBehaviour
{
    [Header("1手あたりの再生秒数")]
    public float stepInterval = 0.5f;

    enum Phase { Select, Animate, Finished }
    Phase _phase;

    MatchState _state;
    IRandom _rng;
    IMoveProvider _ai;
    int _p1HeadMax, _p1BodyMax, _p2HeadMax, _p2BodyMax;

    static readonly PunchType[] OFF = {
        PunchType.Jab, PunchType.Hook, PunchType.Straight, PunchType.Upper,
        PunchType.BodyJab, PunchType.BodyHook, PunchType.BodyStraight, PunchType.BodyUpper,
        PunchType.StepIn, PunchType.StepBack, PunchType.Recover, PunchType.Clinch
    };
    static readonly DefenseType[] DEF = {
        DefenseType.GuardUp, DefenseType.GuardLow, DefenseType.Sway, DefenseType.Duck,
        DefenseType.CounterUp, DefenseType.CounterLow
    };
    int _o1, _o2, _d;

    readonly List<BattleEvent> _turn = new List<BattleEvent>();
    int _animIdx;
    float _timer;
    SubturnResult _pending;
    BattleEvent _cur;
    readonly List<string> _lines = new List<string>();
    MatchOutcome _outcome;

    const string P1 = "YOU(RED)";
    const string P2 = "CPU(BLUE)";
    const float DesignW = 960f;
    const float DesignH = 540f;

    void Start() { NewMatch(); }

    void NewMatch()
    {
        var s1 = new BoxerStats {
            Name = "YOU", HpHead = 16, HpBody = 15, Power = 16, Speed = 15, Stamina = 15, Tuffness = 15, Counter = 15,
            JabAtk = 16, JabDef = 15, HookAtk = 16, HookDef = 14, StraightAtk = 16, StraightDef = 15, UpperAtk = 15, UpperDef = 14
        };
        var s2 = new BoxerStats {
            Name = "CPU", HpHead = 15, HpBody = 16, Power = 15, Speed = 14, Stamina = 15, Tuffness = 16, Counter = 16,
            JabAtk = 15, JabDef = 16, HookAtk = 14, HookDef = 15, StraightAtk = 15, StraightDef = 16, UpperAtk = 14, UpperDef = 15
        };
        var f1 = new Fighter(s1);
        var f2 = new Fighter(s2);
        _p1HeadMax = f1.HeadHPMax; _p1BodyMax = f1.BodyHPMax;
        _p2HeadMax = f2.HeadHPMax; _p2BodyMax = f2.BodyHPMax;

        _state = new MatchState(f1, f2, 3) { Distance = Distance.Far };
        _rng = new SystemRandom(System.Environment.TickCount);
        _ai = new BasicAiMoveProvider(_rng);

        _o1 = 0; _o2 = 0; _d = 0;
        _lines.Clear(); _turn.Clear(); _animIdx = 0; _timer = 0; _pending = null;
        _lines.Add("-- Round 1 --");
        _cur = Snapshot();
        _phase = Phase.Select;
    }

    BattleEvent Snapshot()
    {
        return new BattleEvent {
            Round = _state.Round, Turn = _state.Turn, Distance = _state.Distance,
            P1Head = _state.P1.HeadHP, P1Body = _state.P1.BodyHP,
            P2Head = _state.P2.HeadHP, P2Body = _state.P2.BodyHP
        };
    }

    void Fight()
    {
        var p1cmd = new TurnCommands(OFF[_o1], OFF[_o2], DEF[_d]);
        var p2cmd = _ai.GetCommands(_state, 2);
        _turn.Clear();
        _pending = MatchEngine.RunTurn(_state, p1cmd, p2cmd, _rng, _turn);
        _animIdx = 0; _timer = 0; _phase = Phase.Animate;
    }

    void Update()
    {
        if (_phase != Phase.Animate) return;
        _timer += Time.deltaTime;
        if (_timer < stepInterval) return;
        _timer = 0f;

        if (_animIdx < _turn.Count)
        {
            _cur = _turn[_animIdx];
            string line = Describe(_cur);
            if (line != null) { _lines.Add(line); if (_lines.Count > 16) _lines.RemoveAt(0); }
            _animIdx++;
        }
        else
        {
            ProcessTurnEnd();
        }
    }

    void ProcessTurnEnd()
    {
        if (_pending != null && _pending.Ko)
        {
            _outcome = Judgment.Decide(_state, _state.Round, _pending.KoWinner);
            EndMatch(); return;
        }

        _state.Turn++;
        if (_state.Turn > _state.MaxTurn)
        {
            Judgment.ScoreRound(_state);
            if (_state.Round >= _state.MaxRound)
            {
                _outcome = Judgment.Decide(_state, _state.MaxRound, 0);
                EndMatch(); return;
            }
            IntervalRecovery.Apply(_state);
            _state.Round++; _state.Turn = 1;
            _lines.Add("-- Round " + _state.Round + " --");
            if (_lines.Count > 16) _lines.RemoveAt(0);
        }

        _cur = Snapshot();
        _phase = Phase.Select;
    }

    void EndMatch()
    {
        string kind = _outcome.Kind == WinKind.KO ? "KO" : (_outcome.Kind == WinKind.Draw ? "" : "Decision");
        string who = _outcome.Winner == 0 ? "DRAW" : ((_outcome.Winner == 1 ? P1 : P2) + " WINS");
        _lines.Add("*** " + kind + " " + who + " ***");
        if (_lines.Count > 16) _lines.RemoveAt(0);
        _cur = Snapshot();
        _phase = Phase.Finished;
    }

    static string PunchName(PunchType p)
    {
        switch (p)
        {
            case PunchType.Jab: return "Jab";
            case PunchType.Hook: return "Hook";
            case PunchType.Straight: return "Straight";
            case PunchType.Upper: return "Upper";
            case PunchType.BodyJab: return "Body Jab";
            case PunchType.BodyHook: return "Body Hook";
            case PunchType.BodyStraight: return "Body Straight";
            case PunchType.BodyUpper: return "Body Upper";
            case PunchType.StepIn: return "Step In";
            case PunchType.StepBack: return "Step Back";
            case PunchType.Recover: return "Recover";
            case PunchType.Clinch: return "Clinch";
        }
        return "?";
    }

    static string DefName(DefenseType d)
    {
        switch (d)
        {
            case DefenseType.GuardUp: return "Guard Up";
            case DefenseType.GuardLow: return "Guard Low";
            case DefenseType.Sway: return "Sway";
            case DefenseType.Duck: return "Duck";
            case DefenseType.CounterUp: return "Counter Up";
            case DefenseType.CounterLow: return "Counter Low";
        }
        return "?";
    }

    static string DistName(Distance d) => d == Distance.Near ? "Near" : d == Distance.Mid ? "Mid" : "Far";

    string Describe(BattleEvent e)
    {
        string atk = e.AttackerSide == 1 ? P1 : P2;
        if (!e.IsAttack) return atk + " : " + PunchName(e.Punch);
        if (e.Countered)
        {
            string c = e.AttackerSide == 1 ? P2 : P1;
            return c + " COUNTER! " + e.Damage + (e.Downed ? "  -> DOWN!" : "");
        }
        if (e.Hit) return atk + " " + PunchName(e.Punch) + " HIT! " + e.Damage + (e.Downed ? "  -> DOWN!" : "");
        return atk + " " + PunchName(e.Punch) + " ... blocked";
    }

    void OnGUI()
    {
        float scale = Mathf.Min(Screen.width / DesignW, Screen.height / DesignH);
        if (scale <= 0f) scale = 1f;
        float offX = (Screen.width - DesignW * scale) * 0.5f;
        float offY = (Screen.height - DesignH * scale) * 0.5f;
        Matrix4x4 prev = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offX, offY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

        var e = _cur;
        float w = DesignW;
        float half = w / 2f;

        GUI.Box(new Rect(8, 8, w - 16, 78), "");
        GUI.Label(new Rect(18, 12, 480, 22), "Round " + e.Round + " / Turn " + e.Turn + "    Distance: " + DistName(e.Distance));
        Bar(18, 36, P1 + " head", e.P1Head, _p1HeadMax, new Color(1f, 0.35f, 0.35f), half - 30);
        Bar(18, 58, P1 + " body", e.P1Body, _p1BodyMax, new Color(1f, 0.6f, 0.3f), half - 30);
        Bar(half + 10, 36, P2 + " head", e.P2Head, _p2HeadMax, new Color(0.4f, 0.7f, 1f), half - 30);
        Bar(half + 10, 58, P2 + " body", e.P2Body, _p2BodyMax, new Color(0.4f, 0.85f, 1f), half - 30);

        GUI.Box(new Rect(8, 94, w - 16, 300), "");
        for (int i = 0; i < _lines.Count; i++)
            GUI.Label(new Rect(18, 99 + i * 18, w - 36, 18), _lines[i]);

        float py = 400;
        GUI.Box(new Rect(8, py, w - 16, DesignH - py - 8), "");
        if (_phase == Phase.Select)
        {
            OffSelector(20, py + 10, "Attack 1", ref _o1);
            OffSelector(20, py + 44, "Attack 2", ref _o2);
            DefSelector(20, py + 78, "Defense", ref _d);
            if (GUI.Button(new Rect(w - 190, py + 28, 170, 64), "FIGHT!")) Fight();
        }
        else if (_phase == Phase.Animate)
        {
            GUI.Label(new Rect(20, py + 44, 400, 24), "...fighting...");
        }
        else // Finished
        {
            GUI.Label(new Rect(20, py + 44, 500, 24), "Match over.");
            if (GUI.Button(new Rect(w - 190, py + 28, 170, 64), "REMATCH")) NewMatch();
        }

        GUI.matrix = prev;
    }

    void OffSelector(float x, float y, string label, ref int idx)
    {
        GUI.Label(new Rect(x, y, 80, 24), label);
        if (GUI.Button(new Rect(x + 90, y, 30, 24), "<")) idx = (idx - 1 + OFF.Length) % OFF.Length;
        GUI.Label(new Rect(x + 126, y, 170, 24), PunchName(OFF[idx]));
        if (GUI.Button(new Rect(x + 300, y, 30, 24), ">")) idx = (idx + 1) % OFF.Length;
    }

    void DefSelector(float x, float y, string label, ref int idx)
    {
        GUI.Label(new Rect(x, y, 80, 24), label);
        if (GUI.Button(new Rect(x + 90, y, 30, 24), "<")) idx = (idx - 1 + DEF.Length) % DEF.Length;
        GUI.Label(new Rect(x + 126, y, 170, 24), DefName(DEF[idx]));
        if (GUI.Button(new Rect(x + 300, y, 30, 24), ">")) idx = (idx + 1) % DEF.Length;
    }

    void Bar(float x, float y, string label, int hp, int max, Color col, float barWidth)
    {
        GUI.Label(new Rect(x, y, 96, 20), label);
        float bx = x + 100;
        float bw = Mathf.Max(40f, barWidth - 100f);
        GUI.color = new Color(0.12f, 0.12f, 0.14f);
        GUI.DrawTexture(new Rect(bx, y + 3, bw, 13), Texture2D.whiteTexture);
        float frac = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
        GUI.color = col;
        GUI.DrawTexture(new Rect(bx, y + 3, bw * frac, 13), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(bx + bw + 6, y, 60, 20), hp.ToString());
    }
}
