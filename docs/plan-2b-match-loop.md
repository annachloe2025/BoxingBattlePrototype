# Plan 2B 試合ループ（撃ち合い解決・MatchEngine・簡易AI）実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development または executing-plans。テストは Unity Test Runner（EditMode）。

**Goal:** Plan 2A の部品を使い、`SubturnResolver`（1撃の命中→ダメージ→適用→ダウン/KO→カウント）・`MatchEngine`（ラウンド→ターン→サブターンのループ）・`IMoveProvider`＋簡易AI を実装。**1試合を決定論的にシミュレートして判定/KOまで**到達できるようにする。

**Architecture:** すべて `BoxingCore`（純C#）。乱数 `IRandom` 注入で決定論的にテスト。

**Tech Stack:** Unity 2022.3 LTS, C#, NUnit。コミットは `git -C Unity/BoxingBattlePrototype`。

**Plan 2B の簡略化（明示）:**
- ダウン時の**レフェリーカウントのタップ操作は省略**し、`MatchState.DownRule`（既定2＝2ノックダウンでKO）で決定。立ち上がり時のHP復帰はBAN式（ゼロ側＝round(もう片方/2)＋スタミナ、最大で頭打ち）。
- **回復(7)/クリンチ(8)** は簡易（小回復で代用。スタミナモデルはPhase2）。
- **ガード成功時の削りダメージ(guard chip)** は無し（Phase2）。スキル/コンビ/必殺/忠実AIは対象外（Plan3）。
- 判定は RoundHit（ラウンド毎リセット）＋ DownCount（累計）を使用（Plan 2Aの実式）。

---

## ファイル構成
```
Assets/BoxingCore/
├─ Model/  MatchState.cs(変更：DownRule 追加)
├─ Logic/  IMoveProvider.cs(新) , ScriptedMoveProvider.cs(新) , BasicAiMoveProvider.cs(新) ,
│          SubturnResolver.cs(新) , MatchEngine.cs(新)
└─ Tests/  SubturnResolverTests.cs , MatchEngineTests.cs
```

---

## Task 1: MatchState.DownRule ＋ 着手プロバイダ（I/F・スクリプト・簡易AI）

**Files:**
- Modify: `Assets/BoxingCore/Model/MatchState.cs`（`DownRule` フィールド追加）
- Create: `Assets/BoxingCore/Logic/IMoveProvider.cs`, `ScriptedMoveProvider.cs`, `BasicAiMoveProvider.cs`

- [ ] **Step 1: MatchState に DownRule を追加**

`MatchState.cs` の `public int MaxTurn = 12;` の次の行に追記:
```csharp
        public int DownRule = 2;   // 何ノックダウンでKOか（1/2/3）。既定2
```

- [ ] **Step 2: IMoveProvider と実装2種を書く**

`Assets/BoxingCore/Logic/IMoveProvider.cs`:
```csharp
namespace BoxingCore
{
    // 各ターン、side(1 or 2) のボクサーの手を返す
    public interface IMoveProvider
    {
        TurnCommands GetCommands(MatchState state, int side);
    }
}
```

`Assets/BoxingCore/Logic/ScriptedMoveProvider.cs`:
```csharp
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
```

`Assets/BoxingCore/Logic/BasicAiMoveProvider.cs`:
```csharp
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
```

- [ ] **Step 3: コンパイル確認**

Unity に戻り Console エラー無し（テストはTask2/3で）。

- [ ] **Step 4: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Model/MatchState.cs Assets/BoxingCore/Logic/IMoveProvider.cs Assets/BoxingCore/Logic/ScriptedMoveProvider.cs Assets/BoxingCore/Logic/BasicAiMoveProvider.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add DownRule, IMoveProvider, Scripted/BasicAi providers"
```

---

## Task 2: SubturnResolver（1撃の解決）

**Files:**
- Create: `Assets/BoxingCore/Logic/SubturnResolver.cs`
- Test: `Assets/BoxingCore/Tests/SubturnResolverTests.cs`

action: 1=P1攻1, 2=P1攻2, 3=P2攻1, 4=P2攻2。攻撃技なら ModifierBuilder→HitResolver で判定し、命中→DamageCalculator→適用→ダウン/KO。防御がカウンターで防御成功なら反撃。移動/回復/クリンチは非攻撃処理。

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/SubturnResolverTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class SubturnResolverTests
{
    static MatchState Make(BoxerStats p1, BoxerStats p2, Distance d = Distance.Mid)
    {
        var s = new MatchState(new Fighter(p1), new Fighter(p2), 3);
        s.Distance = d; s.Position = 4; return s;
    }
    static BoxerStats S(int hpHead=16,int hpBody=16,int power=15,int tuff=15,
                        int jabA=15,int hookA=15,int strA=15,int upA=15,
                        int jabD=15,int hookD=15,int strD=15,int upD=15,int counter=15,int speed=10)
        => new BoxerStats{HpHead=hpHead,HpBody=hpBody,Power=power,Tuffness=tuff,Counter=counter,Speed=speed,
                          JabAtk=jabA,HookAtk=hookA,StraightAtk=strA,UpperAtk=upA,
                          JabDef=jabD,HookDef=hookD,StraightDef=strD,UpperDef=upD};

    static TurnCommands Cmd(PunchType o1, PunchType o2, DefenseType d) => new TurnCommands(o1,o2,d);

    [Test] public void Landing_jab_damages_head_but_never_downs()
    {
        // P2のhead上限を極小(10)。ジャブ命中でも 0 にはせず 1 で止める＝ダウンしない
        var s = Make(S(), S(hpHead:1, jabD:8), Distance.Mid); // P2 head max=10, jab防御低
        s.P2.HeadHP = 3;
        var r = SubturnResolver.Resolve(s, action:1,
            p1: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp),
            p2: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp),
            rng: new FakeRandom(new[]{0.0})); // 命中強制
        Assert.IsFalse(r.Ko);
        Assert.AreEqual(1, s.P2.HeadHP);          // ジャブはダウンさせない→1で停止
        Assert.AreEqual(0, s.P2.DownCount);
        Assert.AreEqual(1, s.RoundHit[0]);        // P1の与弾カウント
    }

    [Test] public void Power_punch_zeroing_head_causes_down_and_KO_with_downRule_1()
    {
        var s = Make(S(power:20, upA:20, speed:30), S(hpHead:1, upD:8, tuff:5), Distance.Near);
        s.DownRule = 1; s.P2.HeadHP = 10;
        var r = SubturnResolver.Resolve(s, action:1,
            p1: Cmd(PunchType.Upper, PunchType.Upper, DefenseType.GuardUp),
            p2: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.GuardLow),
            rng: new FakeRandom(new[]{0.0}));
        Assert.IsTrue(r.Ko);
        Assert.AreEqual(1, r.KoWinner);
        Assert.AreEqual(1, s.P2.DownCount);
    }

    [Test] public void Successful_counter_damages_the_attacker()
    {
        // 攻撃を外させる(命中失敗=防御成功)。P1のストレートをP2が上カウンター成功→P1がダメージ
        var s = Make(S(), S(power:18, counter:17), Distance.Mid);
        s.P1.HeadHP = 160;
        var r = SubturnResolver.Resolve(s, action:1,
            p1: Cmd(PunchType.Straight, PunchType.Jab, DefenseType.GuardUp),
            p2: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.CounterUp),
            rng: new FakeRandom(new[]{0.999})); // 命中失敗→防御(カウンター)成功
        Assert.Less(s.P1.HeadHP, 160);            // P1(攻撃側)が反撃を受けた
        Assert.AreEqual(1, s.RoundHit[1]);        // P2の与弾
    }

    [Test] public void StepIn_changes_distance()
    {
        var s = Make(S(), S(), Distance.Far);
        SubturnResolver.Resolve(s, action:1,
            p1: Cmd(PunchType.StepIn, PunchType.Jab, DefenseType.GuardUp),
            p2: Cmd(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp),
            rng: new FakeRandom(new[]{0.5}));
        Assert.AreEqual(Distance.Mid, s.Distance);
    }
}
```

- [ ] **Step 2: テスト実行（失敗）**

Run All。Expected: `SubturnResolver` 未定義。

- [ ] **Step 3: 実装**

`Assets/BoxingCore/Logic/SubturnResolver.cs`:
```csharp
using System;

namespace BoxingCore
{
    public sealed class SubturnResult
    {
        public bool Ko;
        public int KoWinner; // 1 or 2
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

            // 非攻撃（移動/回復/クリンチ）
            if (pn == 6) { DistanceSystem.StepIn(s, atkSide); return res; }
            if (pn == 5) { DistanceSystem.StepBack(s, atkSide); return res; }
            if (pn == 7 || pn == 8) { Recover(s.Side(atkSide)); return res; } // 回復/クリンチ簡略

            var attacker = s.Side(atkSide);
            var defender = s.Side(defSide);

            UpdatePatternCount(attacker, pn); // 連打カウント更新

            int defEval = HpEval.Evaluate(defender.HeadHP, defender.HeadHPMax, defender.BodyHP, defender.BodyHPMax);
            HitModifiers mods = ModifierBuilder.Build(s, atkSide, punch, defense);
            bool hit = HitResolver.ResolveHit(attacker.Stats, defender.Stats, punch, defense, s.Distance, defEval, mods, rng);
            bool defenderCountered = (defense == DefenseType.CounterUp || defense == DefenseType.CounterLow);

            if (hit)
            {
                int dmg = DamageCalculator.Normal(attacker.Stats, defender.Stats, punch, defenderCountered);
                ApplyDamage(s, defender, defSide, pn, dmg, res, koWinner: atkSide);
                attacker.HitCount++; attacker.SpPts++; s.RoundHit[atkSide - 1]++;
            }
            else if (defenderCountered)
            {
                // カウンター成功：攻撃側が反撃を受ける（上カウンター=頭, 下=腹）
                int cdmg = DamageCalculator.Counter(defender.Stats, attacker.Stats, punch);
                bool toHead = (defense == DefenseType.CounterUp);
                ApplyCounterDamage(s, attacker, toHead, cdmg, res, koWinner: defSide);
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
                    else { defender.HeadHP = 0; Down(s, defender, true, res, koWinner); }
                }
            }
            else // body 9..12
            {
                defender.BodyHP -= dmg;
                if (defender.BodyHP <= 0)
                {
                    if (pn == 9) defender.BodyHP = 1;                // ボディジャブはダウンさせない
                    else { defender.BodyHP = 0; Down(s, defender, false, res, koWinner); }
                }
            }
        }

        private static void ApplyCounterDamage(MatchState s, Fighter target, bool toHead, int dmg,
            SubturnResult res, int koWinner)
        {
            if (toHead)
            {
                target.HeadHP -= dmg;
                if (target.HeadHP <= 0) { target.HeadHP = 0; Down(s, target, true, res, koWinner); }
            }
            else
            {
                target.BodyHP -= dmg;
                if (target.BodyHP <= 0) { target.BodyHP = 0; Down(s, target, false, res, koWinner); }
            }
        }

        // ダウン処理（Plan2B簡略：downRuleで決定、立ち上がりはBAN式HP復帰）
        private static void Down(MatchState s, Fighter f, bool headZeroed, SubturnResult res, int koWinner)
        {
            f.DownCount++;
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
```

- [ ] **Step 4: テスト実行（成功）**

Run All。Expected: SubturnResolverTests 4件 PASS。

- [ ] **Step 5: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Logic/SubturnResolver.cs Assets/BoxingCore/Tests/SubturnResolverTests.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add SubturnResolver (hit/damage/counter/down/KO)"
```

---

## Task 3: MatchEngine（試合ループ）

**Files:**
- Create: `Assets/BoxingCore/Logic/MatchEngine.cs`
- Test: `Assets/BoxingCore/Tests/MatchEngineTests.cs`

ラウンド→ターン(1..12)→サブターン(1..4)。毎ターン両者の手を取得→行動順→4サブターン解決（KOで即終了）→ターン末にカウンター連発カウント更新。ラウンド末に判定スコア＋（最終以外）回復。全ラウンド後に判定。

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/MatchEngineTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class MatchEngineTests
{
    static BoxerStats S(int hpHead=16,int hpBody=16,int power=15,int tuff=15,int speed=10,
                        int jabA=15,int hookA=15,int strA=15,int upA=15,
                        int jabD=15,int hookD=15,int strD=15,int upD=15,int counter=15)
        => new BoxerStats{HpHead=hpHead,HpBody=hpBody,Power=power,Tuffness=tuff,Speed=speed,Counter=counter,
                          JabAtk=jabA,HookAtk=hookA,StraightAtk=strA,UpperAtk=upA,
                          JabDef=jabD,HookDef=hookD,StraightDef=strD,UpperDef=upD};
    static TurnCommands C(PunchType o1, PunchType o2, DefenseType d) => new TurnCommands(o1,o2,d);

    [Test] public void Full_match_runs_to_decision_without_KO()
    {
        var s = new MatchState(new Fighter(S()), new Fighter(S()), maxRound: 1);
        s.Distance = Distance.Far;
        var p1 = new ScriptedMoveProvider(C(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp));
        var p2 = new ScriptedMoveProvider(C(PunchType.Jab, PunchType.Jab, DefenseType.GuardUp));
        var o = MatchEngine.RunMatch(s, p1, p2, new FakeRandom(new[]{0.5}));
        Assert.AreEqual(WinKind.Decision, o.Kind); // ジャブ主体・1ラウンドで判定到達
        Assert.AreEqual(1, o.EndRound);
    }

    [Test] public void Strong_p1_KOs_weak_p2()
    {
        // P1高速・高火力、P2はhead上限極小。近距離からアッパー連打、1ノックダウンKO
        var s = new MatchState(new Fighter(S(power:20, upA:20, speed:30)),
                               new Fighter(S(hpHead:1, upD:8, tuff:3)), maxRound: 3);
        s.Distance = Distance.Near; s.DownRule = 1; s.P2.HeadHP = 10;
        var p1 = new ScriptedMoveProvider(C(PunchType.Upper, PunchType.Upper, DefenseType.GuardUp));
        var p2 = new ScriptedMoveProvider(C(PunchType.Jab, PunchType.Jab, DefenseType.GuardLow));
        var o = MatchEngine.RunMatch(s, p1, p2, new FakeRandom(new[]{0.0}));
        Assert.AreEqual(WinKind.KO, o.Kind);
        Assert.AreEqual(1, o.Winner);
        Assert.AreEqual(1, o.EndRound); // 1ラウンド目で決着
    }

    [Test] public void Basic_ai_match_completes()
    {
        var s = new MatchState(new Fighter(S()), new Fighter(S()), maxRound: 2);
        var rng = new SystemRandom(12345);
        var ai1 = new BasicAiMoveProvider(rng);
        var ai2 = new BasicAiMoveProvider(rng);
        var o = MatchEngine.RunMatch(s, ai1, ai2, rng);
        Assert.IsTrue(o.Winner == 0 || o.Winner == 1 || o.Winner == 2); // 例外なく完走
        Assert.IsTrue(o.EndRound >= 1 && o.EndRound <= 2);
    }
}
```

- [ ] **Step 2: テスト実行（失敗）**

Run All。Expected: `MatchEngine` 未定義。

- [ ] **Step 3: 実装**

`Assets/BoxingCore/Logic/MatchEngine.cs`:
```csharp
namespace BoxingCore
{
    public static class MatchEngine
    {
        public static MatchOutcome RunMatch(MatchState s,
            IMoveProvider p1Provider, IMoveProvider p2Provider, IRandom rng)
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
                        if (r.Ko)
                            return Judgment.Decide(s, s.Round, r.KoWinner);
                    }

                    // ターン末：カウンター連発カウント（カウンター選択で増、非選択で0）
                    UpdateCounterCount(s.P1, p1.Defense);
                    UpdateCounterCount(s.P2, p2.Defense);
                }

                Judgment.ScoreRound(s);                       // ラウンド判定スコア
                if (s.Round < s.MaxRound) IntervalRecovery.Apply(s); // 次ラウンドへ回復(RoundHitもリセット)
            }
            return Judgment.Decide(s, s.MaxRound, 0);         // 判定
        }

        private static void UpdateCounterCount(Fighter f, DefenseType def)
        {
            if (def == DefenseType.CounterUp || def == DefenseType.CounterLow) f.CounterCount++;
            else f.CounterCount = 0;
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功）**

Run All。Expected: MatchEngineTests 3件 PASS（全体：Plan1=25 + Plan2A=17 + Plan2B: SubturnResolver4 + MatchEngine3 = **49件**）。

- [ ] **Step 5: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Logic/MatchEngine.cs Assets/BoxingCore/Tests/MatchEngineTests.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add MatchEngine (full deterministic match loop)"
```

---

## 完了の定義（Plan 2B）
- `BoxingCore` がコンパイルでき、EditMode テスト **49件** PASS。
- **1試合を決定論的にシミュレートして判定/KOまで到達**できる（UIなし）。
- 次：**Plan 3**＝忠実AI（atkLogic/defLogic のCDF抽選＋戦術上書き）を `IMoveProvider` 実装として差し込む。**Plan 4**＝Unityシーン/UI（HPバー・コマンド・ログでプレイ可能化）。
- 保留：レフェリーカウント演出・guard chip・スタミナ/回復/クリンチ厳密化・スキル・コンビ・必殺（Phase 2 / UI層）。
