using System.Collections.Generic;
using UnityEngine;
using BoxingCore;

// セットアップ不要の自動対戦ビューア。
// 空のGameObjectにこのスクリプトを付けて Play するだけ。
// IMGUI(OnGUI)でHPバー・実況ログ・勝敗を表示（Canvas/フォント設定不要）。
public class BattleSceneController : MonoBehaviour
{
    [Header("1手あたりの再生秒数")]
    public float stepInterval = 0.45f;

    readonly List<BattleEvent> _log = new List<BattleEvent>();
    readonly List<string> _lines = new List<string>();
    int _idx;
    float _timer;
    bool _finished;

    const string P1 = "RED";
    const string P2 = "BLUE";
    int _p1HeadMax, _p1BodyMax, _p2HeadMax, _p2BodyMax;
    BattleEvent _cur;

    void Start() { NewMatch(); }

    void NewMatch()
    {
        var s1 = new BoxerStats {
            Name = P1, HpHead = 16, HpBody = 15, Power = 16, Speed = 16, Stamina = 15, Tuffness = 15, Counter = 15,
            JabAtk = 16, JabDef = 15, HookAtk = 16, HookDef = 14, StraightAtk = 16, StraightDef = 15, UpperAtk = 15, UpperDef = 14
        };
        var s2 = new BoxerStats {
            Name = P2, HpHead = 15, HpBody = 16, Power = 15, Speed = 14, Stamina = 15, Tuffness = 16, Counter = 16,
            JabAtk = 15, JabDef = 16, HookAtk = 14, HookDef = 15, StraightAtk = 15, StraightDef = 16, UpperAtk = 14, UpperDef = 15
        };
        var f1 = new Fighter(s1);
        var f2 = new Fighter(s2);
        _p1HeadMax = f1.HeadHPMax; _p1BodyMax = f1.BodyHPMax;
        _p2HeadMax = f2.HeadHPMax; _p2BodyMax = f2.BodyHPMax;

        var state = new MatchState(f1, f2, 3) { Distance = Distance.Far };
        var rng = new SystemRandom(System.Environment.TickCount);
        var ai1 = new BasicAiMoveProvider(rng);
        var ai2 = new BasicAiMoveProvider(rng);

        _log.Clear(); _lines.Clear(); _idx = 0; _timer = 0; _finished = false;
        MatchEngine.RunMatch(state, ai1, ai2, rng, _log);

        _cur = new BattleEvent {
            Round = 1, Turn = 1, Distance = Distance.Far,
            P1Head = _p1HeadMax, P1Body = _p1BodyMax, P2Head = _p2HeadMax, P2Body = _p2BodyMax
        };
    }

    void Update()
    {
        if (_idx >= _log.Count) { _finished = true; return; }
        _timer += Time.deltaTime;
        if (_timer >= stepInterval) { _timer = 0f; Step(); }
    }

    void Step()
    {
        _cur = _log[_idx];
        _idx++;
        string line = Describe(_cur);
        if (line != null)
        {
            _lines.Add(line);
            if (_lines.Count > 20) _lines.RemoveAt(0);
        }
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

    static string DistName(Distance d) => d == Distance.Near ? "Near" : d == Distance.Mid ? "Mid" : "Far";

    string Describe(BattleEvent e)
    {
        if (e.MatchEnd)
        {
            string kind = e.Outcome.Kind == WinKind.KO ? "KO" : (e.Outcome.Kind == WinKind.Draw ? "" : "Decision");
            string who = e.Outcome.Winner == 0 ? "DRAW" : ((e.Outcome.Winner == 1 ? P1 : P2) + " WINS");
            return "*** " + kind + " " + who + " ***";
        }
        if (e.RoundEnd) return "-- Round " + e.Round + " end --";

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

    // 基準解像度（この中にレイアウトし、Game画面サイズへ自動フィット＝どのAspectでも全体表示）
    const float DesignW = 960f;
    const float DesignH = 540f;

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
        GUI.Label(new Rect(18, 12, 460, 22), "Round " + e.Round + " / Turn " + e.Turn + "    Distance: " + DistName(e.Distance));

        Bar(18, 36, P1 + " head", e.P1Head, _p1HeadMax, new Color(1f, 0.35f, 0.35f), half - 30);
        Bar(18, 58, P1 + " body", e.P1Body, _p1BodyMax, new Color(1f, 0.6f, 0.3f), half - 30);
        Bar(half + 10, 36, P2 + " head", e.P2Head, _p2HeadMax, new Color(0.4f, 0.7f, 1f), half - 30);
        Bar(half + 10, 58, P2 + " body", e.P2Body, _p2BodyMax, new Color(0.4f, 0.85f, 1f), half - 30);

        float logTop = 94;
        GUI.Box(new Rect(8, logTop, w - 16, DesignH - logTop - 8), "");
        for (int i = 0; i < _lines.Count; i++)
            GUI.Label(new Rect(18, logTop + 6 + i * 20, w - 36, 20), _lines[i]);

        if (_finished)
        {
            if (GUI.Button(new Rect(w - 168, 14, 150, 40), "WATCH AGAIN"))
                NewMatch();
        }

        GUI.matrix = prev;
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
