# Plan 2A 試合メカニクス（状態・行動順・距離・補正・回復・判定）実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development or superpowers:executing-plans。各ステップは checkbox (`- [ ]`)。テスト実行は Unity の Test Runner（EditMode）。

**Goal:** BAN の試合進行に必要な「状態・行動順・距離/コーナー・命中補正・ラウンド間回復・判定スコア」を、Plan 1 の `BoxingCore` に追加する（純C#・Unity非依存）。実際の撃ち合い解決とループは Plan 2B。

**Architecture:** すべて `BoxingCore` 内。状態 `MatchState` と純粋関数群（`MoveOrder`/`DistanceSystem`/`ModifierBuilder`/`IntervalRecovery`/`Judgment`）。乱数は `IRandom` 注入。テストは決定論的。

**Tech Stack:** Unity 2022.3 LTS, C#, NUnit (Unity Test Framework)。

**前提:** ファイル生成はこの環境で可、テスト実行はユーザーが Unity Test Runner で。コミットは `git -C Unity/BoxingBattlePrototype` で（GitHub連携済 `annachloe2025/BoxingBattlePrototype`）。

**既知の保留（Plan 2A では未実装、明示スタブ）:**
- `stamina_hosei`（スタミナ疲労補正）は **0 固定スタブ**。BANのスタミナ消費モデル未抽出のため。コメントで明示。
- スキル効果・コンビ補正・必殺は HitModifiers で 0（Phase 2 で実装）。

---

## ファイル構成（追加・変更）
```
Assets/BoxingCore/
├─ Model/  TurnCommands.cs(新) , MatchState.cs(新) , MatchOutcome.cs(新)
├─ Tables/ CombatTables.cs(変更：MovePointVal, OffenceSide を追加)
├─ Logic/  MoveOrder.cs(新) , DistanceSystem.cs(新) , ModifierBuilder.cs(新) ,
│          IntervalRecovery.cs(新) , Judgment.cs(新)
└─ Tests/  MoveOrderTests.cs , DistanceSystemTests.cs , ModifierBuilderTests.cs ,
           IntervalRecoveryTests.cs , JudgmentTests.cs
```

---

## Task 1: TurnCommands / MatchState / MatchOutcome

**Files:**
- Create: `Assets/BoxingCore/Model/TurnCommands.cs`, `MatchState.cs`, `MatchOutcome.cs`
- Test: （MatchState はTask2以降のテストで間接的に検証。ここでは型定義のみ）

- [ ] **Step 1: 実装を書く（型定義）**

`Assets/BoxingCore/Model/TurnCommands.cs`:
```csharp
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
```

`Assets/BoxingCore/Model/MatchOutcome.cs`:
```csharp
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
```

`Assets/BoxingCore/Model/MatchState.cs`:
```csharp
namespace BoxingCore
{
    public sealed class MatchState
    {
        public readonly Fighter[] Fighters; // [0]=P1, [1]=P2

        public Distance Distance = Distance.Far; // 開始時は遠（※BAN開始値は要調整）
        public int Position = 4;                 // 1..7（P1基準。1=P1コーナー,7=P2コーナー,4=中央）

        public int Round = 1, Turn = 1, Subturn = 1;
        public readonly int MaxRound;
        public int MaxTurn = 12;

        public readonly int[] RoundHit = new int[2]; // 判定用・各ラウンドで使う被弾/与弾カウント(ラウンド毎リセット)
        public readonly int[] JudgeTotal = new int[2]; // 判定スコア累計

        public MatchState(Fighter p1, Fighter p2, int maxRound)
        {
            Fighters = new[] { p1, p2 };
            MaxRound = maxRound;
        }

        public Fighter P1 => Fighters[0];
        public Fighter P2 => Fighters[1];
        // side: 1 or 2 → Fighters index
        public Fighter Side(int side) => Fighters[side - 1];
    }
}
```

- [ ] **Step 2: コンパイル確認**

Unity に戻り Console にエラーが無いこと（型定義のみ）。Test Runner はまだ変化なし。

- [ ] **Step 3: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Model
git -C Unity/BoxingBattlePrototype commit -m "feat: add TurnCommands, MatchState, MatchOutcome"
```

---

## Task 2: CombatTables 拡張（MovePointVal, OffenceSide）

**Files:**
- Modify: `Assets/BoxingCore/Tables/CombatTables.cs`（末尾の `}` 直前にフィールド追加）
- Test: `Assets/BoxingCore/Tests/MoveOrderTests.cs`（Task3で使用。ここではテーブル値の確認のみ別テストでも可）

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/MoveOrderTests.cs`（まずテーブル存在の確認から。Task3で本体を足す）:
```csharp
using NUnit.Framework;
using BoxingCore;

public class MoveOrderTests
{
    [Test] public void Tables_present()
    {
        Assert.AreEqual(21, CombatTables.MovePointVal.Length);
        // offence_side[subturn 1..4][moveNo 1..6]
        Assert.AreEqual(1, CombatTables.OffenceSide[1][1]); // subturn1,moveNo1 → P1攻撃1
        Assert.AreEqual(3, CombatTables.OffenceSide[1][6]); // subturn1,moveNo6 → P2攻撃1
        Assert.AreEqual(4, CombatTables.OffenceSide[4][1]); // subturn4,moveNo1 → P2攻撃2
    }
}
```

- [ ] **Step 2: テスト実行（失敗を確認）**

Run All。Expected: `CombatTables.MovePointVal` 未定義でコンパイルエラー。

- [ ] **Step 3: 実装（CombatTables にフィールド追加）**

`Assets/BoxingCore/Tables/CombatTables.cs` の最後のフィールド（`DownRecover`）の後ろ、クラス閉じ括弧の前に追記:
```csharp
        // index = spd_diff(0..20) 行動順の重み（P1優先確率）
        public static readonly double[] MovePointVal =
        { 0,0,0.05,0.1,0.15,0.2,0.25,0.3,0.35,0.4,0.5,0.6,0.65,0.7,0.75,0.8,0.85,0.9,0.95,1,1 };

        // [subturn(1..4)][moveNo(1..6)] → 行動(1=P1攻1,2=P1攻2,3=P2攻1,4=P2攻2)。index0は未使用
        public static readonly int[][] OffenceSide =
        {
            null,
            new[]{0,1,1,1,3,3,3},
            new[]{0,2,3,3,1,1,4},
            new[]{0,3,2,4,2,4,1},
            new[]{0,4,4,2,4,2,2},
        };
```

- [ ] **Step 4: テスト実行（成功を確認）**

Run All。Expected: MoveOrderTests.Tables_present PASS。

- [ ] **Step 5: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Tables/CombatTables.cs Assets/BoxingCore/Tests/MoveOrderTests.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add MovePointVal and OffenceSide tables"
```

---

## Task 3: MoveOrder（行動順の決定）

**Files:**
- Create: `Assets/BoxingCore/Logic/MoveOrder.cs`
- Test: 追記 `Assets/BoxingCore/Tests/MoveOrderTests.cs`

BAN: `spd_diff = clamp(P1.speed - P2.speed + 10, 0, 20)`、`mp = MovePointVal[spd_diff]`。3回の判定 `SPn = mp + randn`（`SPn>=1` ⟺ P1がそのスロットを取る、確率 mp）。結果で moveNo 1..6 を決める。

- [ ] **Step 1: 失敗するテストを追記**

`MoveOrderTests.cs` に追記:
```csharp
    // mp=1 のとき全スロットP1 → SP1>=1 & SP2>=1 → moveNo=1
    [Test] public void Faster_p1_gets_moveNo_1()
    {
        // 速度差を十分大きく(mp=1)。乱数は何でもSPn>=1
        var mn = MoveOrder.DecideMoveNo(p1Speed: 30, p2Speed: 0, new FakeRandom(new[]{0.0,0.0,0.0}));
        Assert.AreEqual(1, mn);
    }

    // mp=0(P1遅い)のとき全スロットP2 → SP1<1 & SP2<1 → moveNo=6
    [Test] public void Slower_p1_gets_moveNo_6()
    {
        var mn = MoveOrder.DecideMoveNo(0, 30, new FakeRandom(new[]{0.999,0.999,0.999}));
        Assert.AreEqual(6, mn);
    }

    [Test] public void ActionAt_uses_offence_side_table()
    {
        Assert.AreEqual(1, MoveOrder.ActionAt(subturn:1, moveNo:1));
        Assert.AreEqual(4, MoveOrder.ActionAt(4, 1));
        Assert.AreEqual(1, MoveOrder.ActionAt(3, 6));
    }
```

- [ ] **Step 2: テスト実行（失敗）**

Run All。Expected: `MoveOrder` 未定義。

- [ ] **Step 3: 実装**

`Assets/BoxingCore/Logic/MoveOrder.cs`:
```csharp
namespace BoxingCore
{
    public static class MoveOrder
    {
        // 行動順番号 1..6 を決める（1=P1最速 … 6=P2最速）
        public static int DecideMoveNo(int p1Speed, int p2Speed, IRandom rng)
        {
            int spd = p1Speed - p2Speed + 10;
            if (spd < 0) spd = 0;
            if (spd > 20) spd = 20;
            double mp = CombatTables.MovePointVal[spd];

            bool s1 = mp + rng.NextDouble() >= 1.0;
            bool s2 = mp + rng.NextDouble() >= 1.0;
            bool s3 = mp + rng.NextDouble() >= 1.0;

            if (s1 && s2) return 1;
            if (s1 && !s2 && s3) return 2;
            if (s1 && !s2 && !s3) return 3;
            if (!s1 && s2 && s3) return 4;
            if (!s1 && s2 && !s3) return 5;
            return 6; // !s1 && !s2
        }

        // subturn(1..4), moveNo(1..6) → 行動(1=P1攻1,2=P1攻2,3=P2攻1,4=P2攻2)
        public static int ActionAt(int subturn, int moveNo)
            => CombatTables.OffenceSide[subturn][moveNo];
    }
}
```

- [ ] **Step 4: テスト実行（成功）**

Run All。Expected: MoveOrderTests 4件 PASS。

- [ ] **Step 5: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Logic/MoveOrder.cs Assets/BoxingCore/Tests/MoveOrderTests.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add MoveOrder (action order from speed)"
```

---

## Task 4: DistanceSystem（前進・後退・コーナー）

**Files:**
- Create: `Assets/BoxingCore/Logic/DistanceSystem.cs`
- Test: `Assets/BoxingCore/Tests/DistanceSystemTests.cs`

BAN仕様（`scenario_siai_sub.ks`）:
- P1前進: `dist==1` → `pos++`(pos<=6); それ以外 → `pos++; dist--`
- P1後退: `pos==1`→不可; `dist==3`→`pos--`; それ以外→`dist++; pos--`
- P2前進: `dist==1` → `pos--`(pos>=2); それ以外 → `dist--`
- P2後退: `(pos5&&dist3)||(pos6&&dist2)||(pos7)`→不可; `dist==3`→`pos++`; それ以外→`dist++`
- P2コーナー: `(pos5&&dist3)||(pos6&&dist2)||(pos7)` / P1コーナー: `pos==1`

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/DistanceSystemTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class DistanceSystemTests
{
    static MatchState State(Distance d = Distance.Mid, int pos = 4)
    {
        var s = new MatchState(new Fighter(new BoxerStats{HpHead=1,HpBody=1}),
                               new Fighter(new BoxerStats{HpHead=1,HpBody=1}), 3);
        s.Distance = d; s.Position = pos; return s;
    }

    [Test] public void P1_stepin_closes_distance_and_advances()
    {
        var s = State(Distance.Far, 4);
        DistanceSystem.StepIn(s, 1);
        Assert.AreEqual(Distance.Mid, s.Distance);
        Assert.AreEqual(5, s.Position);
    }

    [Test] public void P1_stepin_at_near_only_pushes_position()
    {
        var s = State(Distance.Near, 4);
        DistanceSystem.StepIn(s, 1);
        Assert.AreEqual(Distance.Near, s.Distance);
        Assert.AreEqual(5, s.Position);
    }

    [Test] public void P2_stepin_at_mid_only_closes_distance()
    {
        var s = State(Distance.Mid, 4);
        DistanceSystem.StepIn(s, 2);
        Assert.AreEqual(Distance.Near, s.Distance);
        Assert.AreEqual(4, s.Position); // P2前進では位置不変(dist>1)
    }

    [Test] public void P1_stepback_at_own_corner_is_blocked()
    {
        var s = State(Distance.Mid, 1);
        DistanceSystem.StepBack(s, 1);
        Assert.AreEqual(Distance.Mid, s.Distance);
        Assert.AreEqual(1, s.Position);
    }

    [Test] public void P2_cornered_detection()
    {
        Assert.IsTrue(DistanceSystem.IsCornered(State(Distance.Far, 7), 2));
        Assert.IsTrue(DistanceSystem.IsCornered(State(Distance.Far, 5), 2));
        Assert.IsTrue(DistanceSystem.IsCornered(State(Distance.Mid, 1), 1));
        Assert.IsFalse(DistanceSystem.IsCornered(State(Distance.Mid, 4), 2));
    }
}
```

- [ ] **Step 2: テスト実行（失敗）**

Run All。Expected: `DistanceSystem` 未定義。

- [ ] **Step 3: 実装**

`Assets/BoxingCore/Logic/DistanceSystem.cs`:
```csharp
namespace BoxingCore
{
    public static class DistanceSystem
    {
        public static void StepIn(MatchState s, int side)
        {
            if (side == 1)
            {
                if (s.Distance == Distance.Near) { if (s.Position <= 6) s.Position++; }
                else { s.Position++; s.Distance = (Distance)((int)s.Distance - 1); }
            }
            else // P2
            {
                if (s.Distance == Distance.Near) { if (s.Position >= 2) s.Position--; }
                else { s.Distance = (Distance)((int)s.Distance - 1); }
            }
        }

        public static void StepBack(MatchState s, int side)
        {
            if (side == 1)
            {
                if (s.Position == 1) { /* 自コーナー：下がれない */ }
                else if (s.Distance == Distance.Far) { s.Position--; }
                else { s.Distance = (Distance)((int)s.Distance + 1); s.Position--; }
            }
            else // P2
            {
                if (IsCornered(s, 2)) { /* 相手コーナー側：下がれない */ }
                else if (s.Distance == Distance.Far) { s.Position++; }
                else { s.Distance = (Distance)((int)s.Distance + 1); }
            }
        }

        // side のボクサーがコーナーに詰められているか
        public static bool IsCornered(MatchState s, int side)
        {
            if (side == 1) return s.Position == 1;
            // P2
            return (s.Position == 5 && s.Distance == Distance.Far)
                || (s.Position == 6 && s.Distance == Distance.Mid)
                || (s.Position == 7);
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功）**

Run All。Expected: DistanceSystemTests 5件 PASS。

- [ ] **Step 5: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Logic/DistanceSystem.cs Assets/BoxingCore/Tests/DistanceSystemTests.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add DistanceSystem (stepin/stepback/corner, faithful to BAN)"
```

---

## Task 5: ModifierBuilder（命中補正の算出）

**Files:**
- Create: `Assets/BoxingCore/Logic/ModifierBuilder.cs`
- Test: `Assets/BoxingCore/Tests/ModifierBuilderTests.cs`

連打ペナルティ(hook/straight)・カウンター読みペナルティ・コーナー補正を `HitModifiers` に詰める。`StaminaHosei` は **0 スタブ**（BANスタミナ消費モデル未抽出）。スキル/コンビ補正も 0（Phase 2）。

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/ModifierBuilderTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class ModifierBuilderTests
{
    static MatchState State()
        => new MatchState(new Fighter(new BoxerStats{HpHead=1,HpBody=1}),
                          new Fighter(new BoxerStats{HpHead=1,HpBody=1}), 3);

    [Test] public void Hook_repeat_penalty_from_attacker_hookCount()
    {
        var s = State();
        s.P1.HookCount = 12; // OnePaternPenaltyVal[1][12] = -4
        var m = ModifierBuilder.Build(s, attackerSide: 1, PunchType.Hook, DefenseType.GuardUp);
        Assert.AreEqual(-4, m.OnePaternPenalty);
    }

    [Test] public void Counter_read_penalty_from_defender_counterCount()
    {
        var s = State();
        s.P2.CounterCount = 6; // CounterPenaltyVal[1][6] = -6
        // P1が攻撃、P2が上カウンター選択
        var m = ModifierBuilder.Build(s, 1, PunchType.Straight, DefenseType.CounterUp);
        Assert.AreEqual(-6, m.CounterPenalty);
    }

    [Test] public void Corner_penalty_when_defender_cornered()
    {
        var s = State(); s.Position = 7; // P2コーナー
        var m = ModifierBuilder.Build(s, attackerSide: 1, PunchType.Jab, DefenseType.GuardUp);
        Assert.AreEqual(-5, m.CornerVal);
    }

    [Test] public void Stamina_hosei_is_stubbed_zero()
    {
        var s = State();
        var m = ModifierBuilder.Build(s, 1, PunchType.Jab, DefenseType.GuardUp);
        Assert.AreEqual(0, m.StaminaHosei);
    }
}
```

- [ ] **Step 2: テスト実行（失敗）**

Run All。Expected: `ModifierBuilder` 未定義。

- [ ] **Step 3: 実装**

`Assets/BoxingCore/Logic/ModifierBuilder.cs`:
```csharp
namespace BoxingCore
{
    public static class ModifierBuilder
    {
        // attackerSide=1or2。defenderはもう一方。
        public static HitModifiers Build(MatchState s, int attackerSide,
            PunchType attackPunch, DefenseType defenderDefense)
        {
            var m = new HitModifiers();
            var attacker = s.Side(attackerSide);
            var defender = s.Side(attackerSide == 1 ? 2 : 1);
            int ap = (int)attackPunch;
            int dd = (int)defenderDefense;

            // 連打ペナルティ（フック=2/10, ストレート=3/11）。skill変種は Phase2 → [1]固定
            if (ap == 2 || ap == 10)
                m.OnePaternPenalty = Lookup(CombatTables.OnePaternPenaltyVal[1], attacker.HookCount);
            else if (ap == 3 || ap == 11)
                m.OnePaternPenalty = Lookup(CombatTables.OnePaternPenaltyVal[1], attacker.StraightCount);

            // カウンター読みペナルティ（防御側がカウンター 1/5 を選択時）
            if (dd == 1 || dd == 5)
                m.CounterPenalty = Lookup(CombatTables.CounterPenaltyVal[1], defender.CounterCount);

            // コーナー補正：防御側が詰められていれば -5
            if (DistanceSystem.IsCornered(s, attackerSide == 1 ? 2 : 1))
                m.CornerVal = -5;

            // StaminaHosei: 0 スタブ（BANスタミナ消費モデル未抽出。Phase2で実装）
            m.StaminaHosei = 0;
            // Gyakkyou/SkillDodge/SkillGuard/SkillBody/Aisyou/CombiAdd/SioPenalty: 0（Phase2）

            return m;
        }

        // 配列範囲外は末尾値でクランプ
        private static int Lookup(int[] table, int index)
        {
            if (index < 0) index = 0;
            if (index >= table.Length) index = table.Length - 1;
            return table[index];
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功）**

Run All。Expected: ModifierBuilderTests 4件 PASS。

- [ ] **Step 5: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Logic/ModifierBuilder.cs Assets/BoxingCore/Tests/ModifierBuilderTests.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add ModifierBuilder (one-pattern/counter/corner; stamina stubbed)"
```

---

## Task 6: IntervalRecovery（ラウンド間回復）

**Files:**
- Create: `Assets/BoxingCore/Logic/IntervalRecovery.cs`
- Test: `Assets/BoxingCore/Tests/IntervalRecoveryTests.cs`

BAN: `reduce = R1:4/R2:6/R3:8/R4:10/R5+:12`、`HP += round((max-HP)/reduce) + stamina`（最大で頭打ち）。あわせてラウンド毎リセット（RoundHit、TuffnessAvailable=true）。

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/IntervalRecoveryTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class IntervalRecoveryTests
{
    [Test] public void Recovers_fraction_plus_stamina_capped_at_max()
    {
        var st = new BoxerStats { HpHead = 16, HpBody = 16, Stamina = 5 };
        var f = new Fighter(st);            // max=160/160
        f.HeadHP = 60; f.BodyHP = 60;
        var s = new MatchState(f, new Fighter(st), 6);
        s.Round = 1;                         // reduce=4
        // head: 60 + round((160-60)/4) + 5 = 60 + 25 + 5 = 90
        IntervalRecovery.Apply(s);
        Assert.AreEqual(90, f.HeadHP);
        Assert.AreEqual(90, f.BodyHP);
    }

    [Test] public void Round5_uses_reduce_12_and_resets_per_round_state()
    {
        var st = new BoxerStats { HpHead = 12, HpBody = 12, Stamina = 0 };
        var f = new Fighter(st); f.HeadHP = 0; f.BodyHP = 0; f.TuffnessAvailable = false;
        var s = new MatchState(f, new Fighter(st), 6);
        s.Round = 5; s.RoundHit[0] = 9;       // reduce=12
        IntervalRecovery.Apply(s);
        // 0 + round(120/12) + 0 = 10
        Assert.AreEqual(10, f.HeadHP);
        Assert.IsTrue(f.TuffnessAvailable);   // 毎ラウンド復活
        Assert.AreEqual(0, s.RoundHit[0]);    // 判定用カウントはリセット
    }
}
```

- [ ] **Step 2: テスト実行（失敗）**

Run All。Expected: `IntervalRecovery` 未定義。

- [ ] **Step 3: 実装**

`Assets/BoxingCore/Logic/IntervalRecovery.cs`:
```csharp
using System;

namespace BoxingCore
{
    public static class IntervalRecovery
    {
        public static void Apply(MatchState s)
        {
            int reduce;
            switch (s.Round)
            {
                case 1: reduce = 4; break;
                case 2: reduce = 6; break;
                case 3: reduce = 8; break;
                case 4: reduce = 10; break;
                default: reduce = 12; break;
            }

            for (int i = 0; i < 2; i++)
            {
                var f = s.Fighters[i];
                int sta = f.Stats.Stamina;
                f.HeadHP = Recover(f.HeadHP, f.HeadHPMax, reduce, sta);
                f.BodyHP = Recover(f.BodyHP, f.BodyHPMax, reduce, sta);
                f.TuffnessAvailable = true; // 毎ラウンド、ダウン回避1回復活
                s.RoundHit[i] = 0;          // 判定用カウントをリセット
            }
        }

        private static int Recover(int hp, int max, int reduce, int stamina)
        {
            int v = hp + (int)Math.Round((max - hp) / (double)reduce) + stamina;
            return v > max ? max : v;
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功）**

Run All。Expected: IntervalRecoveryTests 2件 PASS。

- [ ] **Step 5: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Logic/IntervalRecovery.cs Assets/BoxingCore/Tests/IntervalRecoveryTests.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add IntervalRecovery (per-round HP recovery + state reset)"
```

---

## Task 7: Judgment（ラウンド判定スコア・勝者決定）

**Files:**
- Create: `Assets/BoxingCore/Logic/Judgment.cs`
- Test: `Assets/BoxingCore/Tests/JudgmentTests.cs`

BAN実式（`scenario_siai_sub.ks` 2801-2810）。ラウンドの「与弾が多い側」を勝者=10点、相手は `floor((相手与弾+20+自ダウン×5)/(自与弾+20+相手ダウン×5)×10)`（10以上は勝者8/敗者10にキャップ）。引数は各ボクサーの「そのラウンドの与弾数(RoundHit)」と「ダウン数(DownCount)」。

> 注：RoundHit[i] は「i が与えたヒット数」。DownCount[i] は「i が喫したダウン数」。BANの式に合わせる。

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/JudgmentTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class JudgmentTests
{
    static MatchState State()
        => new MatchState(new Fighter(new BoxerStats{HpHead=1,HpBody=1}),
                          new Fighter(new BoxerStats{HpHead=1,HpBody=1}), 3);

    [Test] public void Round_winner_gets_10_loser_proportional()
    {
        var s = State();
        s.RoundHit[0] = 10; s.RoundHit[1] = 5;   // P1が多く当てた
        s.P1.DownCount = 0;  s.P2.DownCount = 0;
        Judgment.ScoreRound(s);
        // P1=10。P2 = floor((5+20+0)/(10+20+0)*10)= floor(25/30*10)=floor(8.33)=8
        Assert.AreEqual(10, s.JudgeTotal[0]);
        Assert.AreEqual(8,  s.JudgeTotal[1]);
    }

    [Test] public void Accumulates_across_rounds_and_decides_winner()
    {
        var s = State();
        s.RoundHit[0]=10; s.RoundHit[1]=5; Judgment.ScoreRound(s); // R1: P1=10, P2=8 → 累計10,8
        s.RoundHit[0]=8;  s.RoundHit[1]=9; Judgment.ScoreRound(s); // R2: P2=10, P1=floor(28/29*10)=9 → 累計19,18
        var o = Judgment.Decide(s, endRound: 2, ko: 0);
        Assert.AreEqual(1, o.Winner); // 19 > 18 で P1 勝ち
        Assert.AreEqual(WinKind.Decision, o.Kind);
    }
}
```

- [ ] **Step 2: テスト実行（失敗）**

Run All。Expected: `Judgment` 未定義。

- [ ] **Step 3: 実装**

`Assets/BoxingCore/Logic/Judgment.cs`:
```csharp
using System;

namespace BoxingCore
{
    public static class Judgment
    {
        // そのラウンドのスコアを JudgeTotal に加算する
        public static void ScoreRound(MatchState s)
        {
            int p1Hit = s.RoundHit[0], p2Hit = s.RoundHit[1];
            int p1Down = s.P1.DownCount, p2Down = s.P2.DownCount;

            int p1, p2;
            if (p1Hit >= p2Hit)
            {
                p1 = 10;
                p2 = (int)Math.Floor((p2Hit + 20 + p1Down * 5) / (double)(p1Hit + 20 + p2Down * 5) * 10);
                if (p2 >= 10) { p1 = 8; p2 = 10; }
            }
            else
            {
                p2 = 10;
                p1 = (int)Math.Floor((p1Hit + 20 + p2Down * 5) / (double)(p2Hit + 20 + p1Down * 5) * 10);
                if (p1 >= 10) { p2 = 8; p1 = 10; }
            }
            s.JudgeTotal[0] += p1;
            s.JudgeTotal[1] += p2;
        }

        // 試合終了時の勝者。ko: 0=判定, 1=P1のKO勝ち, 2=P2のKO勝ち
        public static MatchOutcome Decide(MatchState s, int endRound, int ko)
        {
            if (ko == 1) return new MatchOutcome { Winner = 1, Kind = WinKind.KO, EndRound = endRound };
            if (ko == 2) return new MatchOutcome { Winner = 2, Kind = WinKind.KO, EndRound = endRound };

            int w;
            if (s.JudgeTotal[0] > s.JudgeTotal[1]) w = 1;
            else if (s.JudgeTotal[0] < s.JudgeTotal[1]) w = 2;
            else w = 0;
            return new MatchOutcome { Winner = w, Kind = w == 0 ? WinKind.Draw : WinKind.Decision, EndRound = endRound };
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功）**

Run All。Expected: JudgmentTests 2件 PASS（全体：Plan1の25 + Plan2Aの MoveOrder4 + Distance5 + Modifier4 + Recovery2 + Judgment2 = 42件）。

- [ ] **Step 5: Commit**

```bash
git -C Unity/BoxingBattlePrototype add Assets/BoxingCore/Logic/Judgment.cs Assets/BoxingCore/Tests/JudgmentTests.cs
git -C Unity/BoxingBattlePrototype commit -m "feat: add Judgment (BAN per-round scoring formula + winner)"
```

---

## 完了の定義（Plan 2A）
- `BoxingCore` がコンパイルでき、EditMode テスト合計 **42件** PASS。
- 試合の状態・行動順・距離/コーナー・命中補正・回復・判定が BAN準拠で揃う。
- **次（Plan 2B）**：`SubturnResolver`（1撃の命中→ダメージ→適用→ダウン/KO→カウント更新）、`IMoveProvider`＋簡易AI＋スクリプト用、`MatchEngine`（ループ）。完成すると**1試合まるごと決定論的にシミュレートして検証**できる。
- 保留：`StaminaHosei`・スキル・コンビ・必殺（Phase 2）、忠実AI（Plan 3）、Unity UI（Plan 4）。
