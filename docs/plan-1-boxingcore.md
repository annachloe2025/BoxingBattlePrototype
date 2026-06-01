# BoxingCore 戦闘計算ライブラリ 実装計画（Phase 1 / Plan 1）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** BAN の戦闘の「命中・ダメージ・HP評価」を忠実に符号化した、Unity非依存の純C#計算ライブラリ `BoxingCore` を、NUnitテスト付きで作る。

**Architecture:** `BoxingCore` は UnityEngine 参照なしの asmdef（`noEngineReferences:true`）。純粋関数＋データテーブル＋乱数インターフェイスのみ。状態を持つ MatchEngine・AI・Unity層は後続プランで実装する。テストは Unity の EditMode（Test Runner）で実行。

**Tech Stack:** Unity (2022.3 LTS 想定), C#, Unity Test Framework (NUnit)。

**前提・運用メモ:**
- この計画の `.cs`/`.asmdef` ファイル作成は、Unityの無い環境でも実施できる。**テスト実行（「Run」ステップ）は Unityエディタの Test Runner（Window > General > Test Runner > EditMode > Run All）でユーザーが行う。**
- コミット手順はGit利用を推奨（Unityプロジェクト直下で `git init` 済み前提）。Git未使用なら各コミットは「保存チェックポイント」と読み替える。
- 出典の数値は [解析結果](../../../解析結果_BoxingArenaNebula.md) と `_analysis/extracted/data/scenario_data.ks`／`scenario_macro.ks`／`scenario_siai_sub.ks`。

---

## ファイル構成（この計画で作るもの）

```
<Unityプロジェクト>/Assets/BoxingCore/
├─ BoxingCore.asmdef                 純C#アセンブリ定義（Unity参照なし）
├─ Model/
│   ├─ PunchType.cs                  攻撃技enum
│   ├─ DefenseType.cs                防御enum
│   ├─ Distance.cs                   距離enum
│   ├─ AttackKind.cs                 パンチ→攻撃ステータス種別
│   ├─ Punches.cs                    技の分類・対応ヘルパー
│   ├─ BoxerStats.cs                 ボクサー能力（不変）
│   └─ Fighter.cs                    試合中の可変状態
├─ Tables/
│   └─ CombatTables.cs               全変換テーブル（BAN実数値）
├─ Logic/
│   ├─ IRandom.cs                    乱数インターフェイス＋実装
│   ├─ HpEval.cs                     頭/腹HP→1〜10評価
│   ├─ HitResolver.cs                ATK_DEF・命中率
│   └─ DamageCalculator.cs           通常/カウンターダメージ
└─ Tests/
    ├─ BoxingCore.Tests.asmdef       EditModeテストアセンブリ
    ├─ PunchesTests.cs
    ├─ FighterTests.cs
    ├─ CombatTablesTests.cs
    ├─ FakeRandomTests.cs
    ├─ HpEvalTests.cs
    ├─ HitResolverTests.cs
    └─ DamageCalculatorTests.cs
```

各ファイルは単一責務。`BoxingCore` は純粋関数中心で、後続の MatchEngine から呼ばれる。

---

## Task 1: プロジェクトとアセンブリ定義のセットアップ

**Files:**
- Create: `Assets/BoxingCore/BoxingCore.asmdef`
- Create: `Assets/BoxingCore/Tests/BoxingCore.Tests.asmdef`

- [ ] **Step 1: Unityプロジェクトを用意**

Unity Hub で 2022.3 LTS の 2D(URP不要) プロジェクトを新規作成（例名 `BoxingArenaClone`）。以降のパスはその `Assets/` 配下。

- [ ] **Step 2: コア用 asmdef を作成**

`Assets/BoxingCore/BoxingCore.asmdef`:
```json
{
    "name": "BoxingCore",
    "rootNamespace": "BoxingCore",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "autoReferenced": true,
    "noEngineReferences": true
}
```
`noEngineReferences:true` により UnityEngine に依存しない純C#になる。

- [ ] **Step 3: テスト用 asmdef を作成**

`Assets/BoxingCore/Tests/BoxingCore.Tests.asmdef`:
```json
{
    "name": "BoxingCore.Tests",
    "rootNamespace": "BoxingCore.Tests",
    "references": ["BoxingCore"],
    "includePlatforms": ["Editor"],
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "optionalUnityReferences": ["TestAssemblies"],
    "autoReferenced": false
}
```

- [ ] **Step 4: Test Runner を開いて空の状態を確認**

Unityで Window > General > Test Runner > EditMode タブ。まだテストは無い（空）。コンパイルエラーが無いことを確認。

- [ ] **Step 5: Commit**

```bash
git add Assets/BoxingCore/BoxingCore.asmdef Assets/BoxingCore/Tests/BoxingCore.Tests.asmdef
git commit -m "chore: set up BoxingCore asmdefs (pure C# core + EditMode tests)"
```

---

## Task 2: enum と技分類ヘルパー（Punches）

**Files:**
- Create: `Assets/BoxingCore/Model/PunchType.cs`, `DefenseType.cs`, `Distance.cs`, `AttackKind.cs`, `Punches.cs`
- Test: `Assets/BoxingCore/Tests/PunchesTests.cs`

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/PunchesTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class PunchesTests
{
    [Test] public void Kind_maps_body_and_head_to_same_stat()
    {
        Assert.AreEqual(AttackKind.Jab,      Punches.Kind(PunchType.Jab));
        Assert.AreEqual(AttackKind.Jab,      Punches.Kind(PunchType.BodyJab));
        Assert.AreEqual(AttackKind.Hook,     Punches.Kind(PunchType.BodyHook));
        Assert.AreEqual(AttackKind.Straight, Punches.Kind(PunchType.Straight));
        Assert.AreEqual(AttackKind.Upper,    Punches.Kind(PunchType.BodyUpper));
    }

    [Test] public void Head_and_body_classification()
    {
        Assert.IsTrue(Punches.IsHead(PunchType.Hook));
        Assert.IsFalse(Punches.IsHead(PunchType.BodyHook));
        Assert.IsTrue(Punches.IsBody(PunchType.BodyStraight));
        Assert.IsFalse(Punches.IsBody(PunchType.Straight));
        Assert.IsTrue(Punches.IsAttack(PunchType.Upper));
        Assert.IsFalse(Punches.IsAttack(PunchType.Clinch));
    }
}
```

- [ ] **Step 2: テスト実行（失敗を確認）**

Test Runner > EditMode > Run All。
Expected: コンパイルエラー（`PunchType`/`Punches` 未定義）。

- [ ] **Step 3: 最小実装を書く**

`Assets/BoxingCore/Model/PunchType.cs`:
```csharp
namespace BoxingCore
{
    public enum PunchType
    {
        Jab = 1, Hook = 2, Straight = 3, Upper = 4,
        StepBack = 5, StepIn = 6, Recover = 7, Clinch = 8,
        BodyJab = 9, BodyHook = 10, BodyStraight = 11, BodyUpper = 12
    }
}
```

`Assets/BoxingCore/Model/DefenseType.cs`:
```csharp
namespace BoxingCore
{
    public enum DefenseType
    {
        CounterUp = 1, GuardUp = 2, Sway = 3, Duck = 4, CounterLow = 5, GuardLow = 6
    }
}
```

`Assets/BoxingCore/Model/Distance.cs`:
```csharp
namespace BoxingCore
{
    public enum Distance { Near = 1, Mid = 2, Far = 3 }
}
```

`Assets/BoxingCore/Model/AttackKind.cs`:
```csharp
namespace BoxingCore
{
    public enum AttackKind { Jab, Hook, Straight, Upper }
}
```

`Assets/BoxingCore/Model/Punches.cs`:
```csharp
namespace BoxingCore
{
    public static class Punches
    {
        public static bool IsHead(PunchType p) => (int)p >= 1 && (int)p <= 4;
        public static bool IsBody(PunchType p) => (int)p >= 9 && (int)p <= 12;
        public static bool IsAttack(PunchType p) => IsHead(p) || IsBody(p);

        public static AttackKind Kind(PunchType p)
        {
            int n = (int)p;
            if (n == 1 || n == 9) return AttackKind.Jab;
            if (n == 2 || n == 10) return AttackKind.Hook;
            if (n == 3 || n == 11) return AttackKind.Straight;
            return AttackKind.Upper; // 4 or 12
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功を確認）**

Test Runner > Run All。Expected: PunchesTests 2件 PASS。

- [ ] **Step 5: Commit**

```bash
git add Assets/BoxingCore/Model Assets/BoxingCore/Tests/PunchesTests.cs
git commit -m "feat: add punch/defense/distance enums and Punches classifier"
```

---

## Task 3: BoxerStats と Fighter

**Files:**
- Create: `Assets/BoxingCore/Model/BoxerStats.cs`, `Assets/BoxingCore/Model/Fighter.cs`
- Test: `Assets/BoxingCore/Tests/FighterTests.cs`

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/FighterTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class FighterTests
{
    static BoxerStats Sample()
    {
        return new BoxerStats {
            Name = "TestA", HpHead = 16, HpBody = 15, Power = 16, Speed = 16,
            Stamina = 15, Tuffness = 15, Counter = 17,
            JabAtk = 18, JabDef = 17, HookAtk = 17, HookDef = 13,
            StraightAtk = 18, StraightDef = 17, UpperAtk = 13, UpperDef = 13
        };
    }

    [Test] public void Hp_is_stat_times_ten_and_starts_full()
    {
        var f = new Fighter(Sample());
        Assert.AreEqual(160, f.HeadHPMax);
        Assert.AreEqual(150, f.BodyHPMax);
        Assert.AreEqual(160, f.HeadHP);
        Assert.AreEqual(150, f.BodyHP);
        Assert.IsTrue(f.TuffnessAvailable);
    }

    [Test] public void Atk_and_def_selectors_by_kind()
    {
        var s = Sample();
        Assert.AreEqual(18, s.Atk(AttackKind.Jab));
        Assert.AreEqual(13, s.Atk(AttackKind.Upper));
        Assert.AreEqual(13, s.Def(AttackKind.Hook));
        Assert.AreEqual(17, s.Def(AttackKind.Straight));
    }
}
```

- [ ] **Step 2: テスト実行（失敗を確認）**

Run All。Expected: コンパイルエラー（`BoxerStats`/`Fighter` 未定義）。

- [ ] **Step 3: 最小実装を書く**

`Assets/BoxingCore/Model/BoxerStats.cs`:
```csharp
namespace BoxingCore
{
    public sealed class BoxerStats
    {
        public string Name = "";
        public int HpHead, HpBody, Power, Speed, Stamina, Tuffness, Counter;
        public int JabAtk, JabDef, HookAtk, HookDef, StraightAtk, StraightDef, UpperAtk, UpperDef;
        public int AtkLogic = 1, DefLogic = 1;
        public int[] Skills = new int[3];

        public int Atk(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Jab: return JabAtk;
                case AttackKind.Hook: return HookAtk;
                case AttackKind.Straight: return StraightAtk;
                default: return UpperAtk;
            }
        }

        public int Def(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Jab: return JabDef;
                case AttackKind.Hook: return HookDef;
                case AttackKind.Straight: return StraightDef;
                default: return UpperDef;
            }
        }
    }
}
```

`Assets/BoxingCore/Model/Fighter.cs`:
```csharp
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
```

- [ ] **Step 4: テスト実行（成功を確認）**

Run All。Expected: FighterTests 2件 PASS。

- [ ] **Step 5: Commit**

```bash
git add Assets/BoxingCore/Model/BoxerStats.cs Assets/BoxingCore/Model/Fighter.cs Assets/BoxingCore/Tests/FighterTests.cs
git commit -m "feat: add BoxerStats and Fighter (HP = stat x10)"
```

---

## Task 4: CombatTables（BAN実数値の変換テーブル）

**Files:**
- Create: `Assets/BoxingCore/Tables/CombatTables.cs`
- Test: `Assets/BoxingCore/Tests/CombatTablesTests.cs`

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/CombatTablesTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class CombatTablesTests
{
    [Test] public void AttackPointVal_curve_key_points()
    {
        Assert.AreEqual(0.0,  CombatTables.AttackPointVal[0],  1e-9);
        Assert.AreEqual(0.5,  CombatTables.AttackPointVal[10], 1e-9); // 互角=50%
        Assert.AreEqual(1.0,  CombatTables.AttackPointVal[15], 1e-9); // 差+5で必中
        Assert.AreEqual(21,   CombatTables.AttackPointVal.Length);
    }

    [Test] public void Distance_table_key_values()
    {
        // 遠距離(3)：ストレート(3)=+2, アッパー(4)=-15
        Assert.AreEqual(2,   CombatTables.DistanceVal[(int)Distance.Far][(int)PunchType.Straight]);
        Assert.AreEqual(-15, CombatTables.DistanceVal[(int)Distance.Far][(int)PunchType.Upper]);
        // 近距離(1)：ストレート=-10
        Assert.AreEqual(-10, CombatTables.DistanceVal[(int)Distance.Near][(int)PunchType.Straight]);
    }

    [Test] public void HpVal_healthy_opponent_penalizes_power_punches_but_not_jab()
    {
        Assert.AreEqual(0,   CombatTables.HpVal[10][(int)PunchType.Jab]);      // ジャブは常に0
        Assert.AreEqual(-10, CombatTables.HpVal[10][(int)PunchType.Straight]); // 健全相手に強打-10
        Assert.AreEqual(-15, CombatTables.HpVal[10][(int)PunchType.Upper]);
    }

    [Test] public void DefenseVal_guard_strong_on_matching_height()
    {
        Assert.AreEqual(5,  CombatTables.DefenseVal[(int)DefenseType.GuardUp][(int)PunchType.Hook]);   // 上ガード×頭+5
        Assert.AreEqual(-3, CombatTables.DefenseVal[(int)DefenseType.GuardUp][(int)PunchType.BodyHook]); // 上ガードはボディに甘い
    }
}
```

- [ ] **Step 2: テスト実行（失敗を確認）**

Run All。Expected: コンパイルエラー（`CombatTables` 未定義）。

- [ ] **Step 3: 最小実装を書く**

`Assets/BoxingCore/Tables/CombatTables.cs`（配列 index は技No対応。0=未使用、1-4=ジャブ/フック/ストレート/アッパー、9-12=ボディ各種。距離/防御も BAN の index に合わせて先頭ダミーを置く）:
```csharp
namespace BoxingCore
{
    public static class CombatTables
    {
        // index = ATK_DEF (0..20) → 命中率
        public static readonly double[] AttackPointVal =
        { 0,0.05,0.1,0.15,0.2,0.25,0.3,0.35,0.4,0.45,0.5,0.6,0.7,0.8,0.9,1,1,1,1,1,1 };

        // [distance(1..3)][punchNo(0..12)]
        public static readonly int[][] DistanceVal =
        {
            null, // index 0 未使用
            new[]{0,  0, -5,-10, -3, 0,0,0,0,  1, -3, -8, -2}, // 近
            new[]{0,  0,  0, -5, -7, 0,0,0,0,  0,  0, -4,-10}, // 中
            new[]{0, -1, -5,  2,-15, 0,0,0,0, -2, -7,  0,-20}, // 遠
        };

        // [hpEval(1..10)][punchNo(0..12)]  ※相手のHP状態を参照
        public static readonly int[][] HpVal =
        {
            null,
            new[]{0,0,0,0,0,0,0,0,0,0,0,0,-3},
            new[]{0,0,0,0,-3,0,0,0,0,0,0,0,0},
            new[]{0,0,0,0,0,0,0,0,0,0,0,0,0},
            new[]{0,0,0,0,-5,0,0,0,0,0,-3,-3,-5},
            new[]{0,0,-3,-3,-5,0,0,0,0,0,0,0,-5},
            new[]{0,0,0,0,-5,0,0,0,0,0,0,0,-5},
            new[]{0,0,-5,-5,-10,0,0,0,0,0,-4,-4,-12},
            new[]{0,0,-4,-4,-12,0,0,0,0,0,-5,-5,-10},
            new[]{0,0,-5,-5,-10,0,0,0,0,0,-5,-5,-10},
            new[]{0,0,-10,-10,-15,0,0,0,0,0,-10,-10,-15},
        };

        // [defenseNo(1..6)][punchNo(0..12)]
        public static readonly int[][] DefenseVal =
        {
            null,
            new[]{0, 0, 1, 1, 1,0,0,0,0,-5,-5,-5,-5}, // 上カウンター
            new[]{0, 5, 5, 5, 5,0,0,0,0,-3,-3,-3,-3}, // 上ガード
            new[]{0, 0, 0,-3, 0,0,0,0,0, 0, 0,-2, 0}, // スウェー
            new[]{0,-1,-1, 0, 0,0,0,0,0,-2,-2,-2, 0}, // ダッキング
            new[]{0,-5,-5,-5,-5,0,0,0,0, 0, 1, 1, 1}, // 下カウンター
            new[]{0,-3,-3,-3,-3,0,0,0,0, 5, 5, 5, 5}, // 下ガード
        };

        // [variant(1=通常,2=スキル)][count] カウンター連発ペナルティ（DEFENSEへ加算）
        public static readonly int[][] CounterPenaltyVal =
        {
            null,
            new[]{0,0,0,0,0,-3,-6,-9,-12,-15,-18,-18,-18},
            new[]{0,0,0,0,0,0,0,0,-3,-6,-9,-12,-15},
        };

        // [variant(1=通常,2=コンビスキル)][count] 同パンチ連打ペナルティ（ATTACKへ加算）
        public static readonly int[][] OnePaternPenaltyVal =
        {
            null,
            new[]{0,0,0,0,0,0,0,0,0,0,0,-2,-4,-6,-8,-10,-10,-10,-10,-10,-10,-10,-10,-10,-10},
            new[]{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-2,-4,-6,-8,-10,-10,-10,-10},
        };

        // index = stamina_pts(0..5) スタミナ補正係数
        public static readonly double[] StaminaVal = { 1, 0.8, 0.6, 0.4, 0.2, 0 };

        // [variant][downCount] ダウン復帰確率
        public static readonly double[][] DownRecover =
        {
            new[]{0.0,0.1,0.1,0.25,0.25,0.3,0.3},
            new[]{0.0,0.025,0.05,0.1,0.25,0.3,0.3},
        };
    }
}
```

- [ ] **Step 4: テスト実行（成功を確認）**

Run All。Expected: CombatTablesTests 4件 PASS。

- [ ] **Step 5: Commit**

```bash
git add Assets/BoxingCore/Tables/CombatTables.cs Assets/BoxingCore/Tests/CombatTablesTests.cs
git commit -m "feat: add CombatTables with BAN-extracted values"
```

---

## Task 5: IRandom（乱数インターフェイスと実装）

**Files:**
- Create: `Assets/BoxingCore/Logic/IRandom.cs`
- Test: `Assets/BoxingCore/Tests/FakeRandomTests.cs`

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/FakeRandomTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class FakeRandomTests
{
    [Test] public void FakeRandom_returns_queued_doubles_in_order()
    {
        var r = new FakeRandom(new double[] { 0.1, 0.9 }, new int[] { 2 });
        Assert.AreEqual(0.1, r.NextDouble(), 1e-9);
        Assert.AreEqual(0.9, r.NextDouble(), 1e-9);
        Assert.AreEqual(2, r.Next(10));
    }

    [Test] public void FakeRandom_loops_when_exhausted()
    {
        var r = new FakeRandom(new double[] { 0.5 }, new int[] { 0 });
        Assert.AreEqual(0.5, r.NextDouble(), 1e-9);
        Assert.AreEqual(0.5, r.NextDouble(), 1e-9); // 巡回
    }
}
```

- [ ] **Step 2: テスト実行（失敗を確認）**

Run All。Expected: コンパイルエラー（`FakeRandom` 未定義）。

- [ ] **Step 3: 最小実装を書く**

`Assets/BoxingCore/Logic/IRandom.cs`:
```csharp
using System;

namespace BoxingCore
{
    public interface IRandom
    {
        double NextDouble();        // [0,1)
        int Next(int maxExclusive); // [0,maxExclusive)
    }

    public sealed class SystemRandom : IRandom
    {
        private readonly Random _r;
        public SystemRandom(int seed) { _r = new Random(seed); }
        public double NextDouble() => _r.NextDouble();
        public int Next(int maxExclusive) => _r.Next(maxExclusive);
    }

    // テスト用：与えた値を順に返す（尽きたら先頭へ巡回）
    public sealed class FakeRandom : IRandom
    {
        private readonly double[] _d; private readonly int[] _i;
        private int _di, _ii;
        public FakeRandom(double[] doubles, int[] ints = null)
        {
            _d = (doubles != null && doubles.Length > 0) ? doubles : new double[] { 0.0 };
            _i = (ints != null && ints.Length > 0) ? ints : new int[] { 0 };
        }
        public double NextDouble() { var v = _d[_di % _d.Length]; _di++; return v; }
        public int Next(int maxExclusive) { var v = _i[_ii % _i.Length]; _ii++; return v % maxExclusive; }
    }
}
```

- [ ] **Step 4: テスト実行（成功を確認）**

Run All。Expected: FakeRandomTests 2件 PASS。

- [ ] **Step 5: Commit**

```bash
git add Assets/BoxingCore/Logic/IRandom.cs Assets/BoxingCore/Tests/FakeRandomTests.cs
git commit -m "feat: add IRandom with SystemRandom and FakeRandom"
```

---

## Task 6: HpEval（頭/腹HP → 1〜10 評価）

**Files:**
- Create: `Assets/BoxingCore/Logic/HpEval.cs`
- Test: `Assets/BoxingCore/Tests/HpEvalTests.cs`

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/HpEvalTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class HpEvalTests
{
    [Test] public void Full_health_is_10()
    {
        Assert.AreEqual(10, HpEval.Evaluate(160, 160, 150, 150));
    }
    [Test] public void Head_pinch_only_is_1()
    {
        Assert.AreEqual(1, HpEval.Evaluate(30, 160, 150, 150)); // 頭18.7%≤25%, 腹満タン
    }
    [Test] public void Both_pinch_is_3()
    {
        Assert.AreEqual(3, HpEval.Evaluate(30, 160, 30, 150));
    }
    [Test] public void Body_half_only_is_5()
    {
        Assert.AreEqual(5, HpEval.Evaluate(160, 160, 70, 150)); // 腹46.6%≤50%
    }
    [Test] public void Head_75_only_is_7()
    {
        Assert.AreEqual(7, HpEval.Evaluate(110, 160, 150, 150)); // 頭68.7%≤75%
    }
}
```

- [ ] **Step 2: テスト実行（失敗を確認）**

Run All。Expected: コンパイルエラー（`HpEval` 未定義）。

- [ ] **Step 3: 最小実装を書く**

`Assets/BoxingCore/Logic/HpEval.cs`:
```csharp
namespace BoxingCore
{
    public static class HpEval
    {
        // 0:1(≤25%) 2(≤50%) 3(≤75%) 4(>75%)
        private static int Quart(int hp, int max)
        {
            double r = max <= 0 ? 0.0 : (double)hp / max;
            if (r <= 0.25) return 1;
            if (r <= 0.5) return 2;
            if (r <= 0.75) return 3;
            return 4;
        }

        // 頭/腹の象限を BAN の合成規則で 1..10 に
        public static int Evaluate(int headHP, int headMax, int bodyHP, int bodyMax)
        {
            int h = Quart(headHP, headMax);
            int b = Quart(bodyHP, bodyMax);
            if (h == 1 && b == 1) return 3;
            if (h == 1) return 1;
            if (b == 1) return 2;
            if (h == 2 && b == 2) return 6;
            if (h == 2) return 4;
            if (b == 2) return 5;
            if (h == 3 && b == 3) return 9;
            if (h == 3) return 7;
            if (b == 3) return 8;
            return 10;
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功を確認）**

Run All。Expected: HpEvalTests 5件 PASS。

- [ ] **Step 5: Commit**

```bash
git add Assets/BoxingCore/Logic/HpEval.cs Assets/BoxingCore/Tests/HpEvalTests.cs
git commit -m "feat: add HpEval (head/body quartiles -> 1..10)"
```

---

## Task 7: HitResolver（命中判定の核心）

**Files:**
- Create: `Assets/BoxingCore/Logic/HitResolver.cs`
- Test: `Assets/BoxingCore/Tests/HitResolverTests.cs`

**設計メモ:** 各種補正は `HitModifiers`（既定0）で外部注入。MatchEngine（次プラン）が連打/コンビ/スキル/コーナー/スタミナ補正を計算して渡す。ここでは BAN の基本式とテーブル参照、ジャブ±4丸めを実装する。

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/HitResolverTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class HitResolverTests
{
    static BoxerStats Stat(int jabA=15,int jabD=15,int strA=15,int strD=15,int upA=15,int upD=15)
        => new BoxerStats { JabAtk=jabA, JabDef=jabD, StraightAtk=strA, StraightDef=strD,
                            UpperAtk=upA, UpperDef=upD, HookAtk=15, HookDef=15 };

    [Test] public void Straight_far_vs_healthy_upperguard_is_zero()
    {
        // ATTACK=15+distance(+2)+hp(-10)=7  DEFENSE=15+defense(+5)=20  ATK_DEF=clamp(-3)=0 -> 0%
        var p = HitResolver.HitProbability(Stat(), Stat(), PunchType.Straight, DefenseType.GuardUp,
                                           Distance.Far, opponentHpEval: 10, default);
        Assert.AreEqual(0.0, p, 1e-9);
    }

    [Test] public void Jab_mid_vs_healthy_upperguard_is_25pct()
    {
        // ATTACK=15  DEFENSE=15+5=20  ATK_DEF=5 -> 0.25
        var p = HitResolver.HitProbability(Stat(), Stat(), PunchType.Jab, DefenseType.GuardUp,
                                           Distance.Mid, 10, default);
        Assert.AreEqual(0.25, p, 1e-9);
    }

    [Test] public void Jab_difference_is_clamped_to_plus_minus_4()
    {
        // jabAtk20 vs jabDef10 → 差10だが±4に丸め: ATK_p=4,DEF_p=0
        // ATTACK=4+dist(0)+hp(0)=4  DEFENSE=0+defense(Sway,jab=0)=0  ATK_DEF=clamp(14)=14 -> 0.9
        var atk = Stat(jabA:20); var def = Stat(jabD:10);
        var p = HitResolver.HitProbability(atk, def, PunchType.Jab, DefenseType.Sway,
                                           Distance.Mid, 10, default);
        Assert.AreEqual(0.9, p, 1e-9);
    }

    [Test] public void Upper_near_vs_hurt_wrongguard_is_50pct()
    {
        // ATTACK=15+dist(-3)+hp(0)=12  DEFENSE=15+defense(GuardLow,upper=-3)=12  ATK_DEF=10 -> 0.5
        var p = HitResolver.HitProbability(Stat(), Stat(), PunchType.Upper, DefenseType.GuardLow,
                                           Distance.Near, opponentHpEval: 1, default);
        Assert.AreEqual(0.5, p, 1e-9);
    }

    [Test] public void ResolveHit_uses_rng_threshold()
    {
        // 命中率0.5。乱数0.4<0.5→命中、0.6→防御成功
        var hit = HitResolver.ResolveHit(Stat(), Stat(), PunchType.Upper, DefenseType.GuardLow,
                                         Distance.Near, 1, default, new FakeRandom(new[]{0.4}));
        Assert.IsTrue(hit);
        var miss = HitResolver.ResolveHit(Stat(), Stat(), PunchType.Upper, DefenseType.GuardLow,
                                          Distance.Near, 1, default, new FakeRandom(new[]{0.6}));
        Assert.IsFalse(miss);
    }
}
```

- [ ] **Step 2: テスト実行（失敗を確認）**

Run All。Expected: コンパイルエラー（`HitResolver`/`HitModifiers` 未定義）。

- [ ] **Step 3: 最小実装を書く**

`Assets/BoxingCore/Logic/HitResolver.cs`:
```csharp
using System;

namespace BoxingCore
{
    public struct HitModifiers
    {
        // ATTACK 側
        public int OnePaternPenalty, CombiAdd, SkillBody, Aisyou;
        // DEFENSE 側
        public int CornerVal, StaminaHosei, Gyakkyou, CounterPenalty, SioPenalty, SkillDodge, SkillGuard;
    }

    public static class HitResolver
    {
        // ATTACK_point - DEFENSE_point + 10 を 0..20 にクランプ
        public static int ComputeAtkDef(BoxerStats atk, BoxerStats def, PunchType punch,
            DefenseType defense, Distance dist, int opponentHpEval, in HitModifiers m)
        {
            int p = (int)punch;
            var kind = Punches.Kind(punch);
            int atkP = atk.Atk(kind);
            int defP = def.Def(kind);

            // ジャブ(1/9)は攻防差を±4に丸める
            if (p == 1 || p == 9)
            {
                int diff = atkP - defP;
                if (diff >= 4) { atkP = 4; defP = 0; }
                else if (diff <= -4) { atkP = 0; defP = 4; }
            }

            int attack = atkP
                + CombatTables.DistanceVal[(int)dist][p]
                + CombatTables.HpVal[opponentHpEval][p]
                + m.OnePaternPenalty + m.CombiAdd + m.SkillBody + m.Aisyou;

            int defensePoint = defP
                + CombatTables.DefenseVal[(int)defense][p]
                + m.CornerVal + m.StaminaHosei + m.Gyakkyou + m.CounterPenalty
                + m.SioPenalty + m.SkillDodge + m.SkillGuard;

            int atkDef = attack - defensePoint + 10;
            if (atkDef < 0) atkDef = 0;
            if (atkDef > 20) atkDef = 20;
            return atkDef;
        }

        public static double HitProbability(BoxerStats atk, BoxerStats def, PunchType punch,
            DefenseType defense, Distance dist, int opponentHpEval, in HitModifiers m)
        {
            return CombatTables.AttackPointVal[
                ComputeAtkDef(atk, def, punch, defense, dist, opponentHpEval, in m)];
        }

        // 攻撃成功なら true（=offence_success）
        public static bool ResolveHit(BoxerStats atk, BoxerStats def, PunchType punch,
            DefenseType defense, Distance dist, int opponentHpEval, in HitModifiers m, IRandom rng)
        {
            return rng.NextDouble() < HitProbability(atk, def, punch, defense, dist, opponentHpEval, in m);
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功を確認）**

Run All。Expected: HitResolverTests 5件 PASS。

- [ ] **Step 5: Commit**

```bash
git add Assets/BoxingCore/Logic/HitResolver.cs Assets/BoxingCore/Tests/HitResolverTests.cs
git commit -m "feat: add HitResolver (ATK_DEF, hit probability, jab clamp)"
```

---

## Task 8: DamageCalculator（通常／カウンターダメージ）

**Files:**
- Create: `Assets/BoxingCore/Logic/DamageCalculator.cs`
- Test: `Assets/BoxingCore/Tests/DamageCalculatorTests.cs`

- [ ] **Step 1: 失敗するテストを書く**

`Assets/BoxingCore/Tests/DamageCalculatorTests.cs`:
```csharp
using NUnit.Framework;
using BoxingCore;

public class DamageCalculatorTests
{
    static BoxerStats Atk() => new BoxerStats { Power=16, JabAtk=18, UpperAtk=13, StraightAtk=18, HookAtk=17, Counter=17 };
    static BoxerStats Def() => new BoxerStats { Tuffness=15 };

    [Test] public void Base_damage_by_punch()
    {
        Assert.AreEqual(5,  DamageCalculator.BaseDamage(PunchType.Jab));
        Assert.AreEqual(5,  DamageCalculator.BaseDamage(PunchType.BodyJab));
        Assert.AreEqual(20, DamageCalculator.BaseDamage(PunchType.Hook));
        Assert.AreEqual(20, DamageCalculator.BaseDamage(PunchType.Straight));
        Assert.AreEqual(40, DamageCalculator.BaseDamage(PunchType.Upper));
        Assert.AreEqual(40, DamageCalculator.BaseDamage(PunchType.BodyUpper));
    }

    [Test] public void Jab_normal_damage()
    {
        // 5*(1+(16+18)/100)+max(0,16-15)=5*1.34+1=7.7 -> floor 7
        Assert.AreEqual(7, DamageCalculator.Normal(Atk(), Def(), PunchType.Jab, defenderCountered:false));
    }

    [Test] public void Upper_normal_damage()
    {
        // 40*(1+(16+13)/100)+1 = 40*1.29+1 = 51.6+1 = 52.6 -> floor 52
        Assert.AreEqual(52, DamageCalculator.Normal(Atk(), Def(), PunchType.Upper, false));
    }

    [Test] public void Counter_failure_multiplies_by_1_3_round_half_up()
    {
        // 通常7 → ×1.3 = 9.1 → round 9
        Assert.AreEqual(9, DamageCalculator.Normal(Atk(), Def(), PunchType.Jab, defenderCountered:true));
    }

    [Test] public void Counter_success_damage_uses_counter_stat()
    {
        // 相手がストレート(3)を打ってきたのをカウンター成功: base=15
        // 15*(1+(16+17)/100)+max(0,16-15)=15*1.33+1=19.95+1=20.95 -> floor 20
        Assert.AreEqual(20, DamageCalculator.Counter(Atk(), Def(), attackerPunch: PunchType.Straight));
    }
}
```

- [ ] **Step 2: テスト実行（失敗を確認）**

Run All。Expected: コンパイルエラー（`DamageCalculator` 未定義）。

- [ ] **Step 3: 最小実装を書く**

`Assets/BoxingCore/Logic/DamageCalculator.cs`:
```csharp
using System;

namespace BoxingCore
{
    public static class DamageCalculator
    {
        public static int BaseDamage(PunchType p)
        {
            int n = (int)p;
            if (n == 1 || n == 9) return 5;     // ジャブ／ボディジャブ
            if (n == 4 || n == 12) return 40;   // アッパー／ボディアッパー
            return 20;                          // フック/ストレート（＋ボディ）
        }

        // 通常ダメージ。defenderCountered=防御側がカウンター選択して被弾→×1.3
        public static int Normal(BoxerStats atk, BoxerStats def, PunchType punch, bool defenderCountered)
        {
            int baseD = BaseDamage(punch);
            int add = Math.Max(0, atk.Power - def.Tuffness);
            var kind = Punches.Kind(punch);
            double math = baseD * (1.0 + ((atk.Power + atk.Atk(kind)) / 100.0)) + add;
            int dmg = (int)Math.Floor(math);
            if (defenderCountered) dmg = (int)Math.Floor(dmg * 1.3 + 0.5); // round half up
            return dmg;
        }

        // カウンター成功時。base は「相手が打ってきた技」で決まる
        public static int Counter(BoxerStats counterer, BoxerStats attacker, PunchType attackerPunch)
        {
            int n = (int)attackerPunch;
            int baseD = (n == 1 || n == 6 || n == 8 || n == 9) ? 5
                      : (n == 4 || n == 12) ? 25
                      : 15;
            int add = Math.Max(0, counterer.Power - attacker.Tuffness);
            double math = baseD * (1.0 + ((counterer.Power + counterer.Counter) / 100.0)) + add;
            return (int)Math.Floor(math);
        }
    }
}
```

- [ ] **Step 4: テスト実行（成功を確認）**

Run All。Expected: DamageCalculatorTests 5件 PASS（全テスト合計：Punches2＋Fighter2＋CombatTables4＋FakeRandom2＋HpEval5＋HitResolver5＋Damage5 = 25件）。

- [ ] **Step 5: Commit**

```bash
git add Assets/BoxingCore/Logic/DamageCalculator.cs Assets/BoxingCore/Tests/DamageCalculatorTests.cs
git commit -m "feat: add DamageCalculator (normal/counter, KO-exception handling is in MatchEngine)"
```

---

## 完了の定義（Plan 1）
- `BoxingCore` が UnityEngine 非依存でコンパイルでき、EditMode テスト **25件** が全て PASS。
- 命中率・ダメージ・HP評価が BAN の数値と一致（具体値で検証済）。
- ダウン/KO 例外・連打/スタミナ等の補正計算・ターン進行・AI は **次プラン（Plan 2: MatchEngine + AiController）** で、状態(`MatchState`)とともに実装する。Unity層（SO/シーン/UI）は **Plan 3**。
