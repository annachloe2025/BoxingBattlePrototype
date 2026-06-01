# BoxingBattlePrototype

同人ゲーム「BOXING ARENA NEBULA」の**戦闘システムを忠実に再現**する Unity 2D プロトタイプ（個人の学習・検証用）。
コマンド選択式・確率判定型の読み合い（命中／ダメージ／距離／スタミナ／AI）を、解析で得た**数値パラメータのみ**を使って再現する。
※キャラ名・画像・テキスト等の著作物は使用しない。

## 進捗
- ✅ **Plan 1**：`BoxingCore` … 命中・ダメージ・HP評価の純C#計算ライブラリ（Unity非依存）＋ EditModeテスト **25件PASS**
- ⬜ **Plan 2**：`MatchEngine` ＋ CPU `AiController` ＋ `MatchState`（ターン進行・行動順・ダウン/KO・回復・判定・AI）
- ⬜ **Plan 3**：Unityシーン／UI（HPバー・コマンド・ログ等でプレイ可能化）

## 構成
```
Assets/BoxingCore/
├─ Model/    enum・BoxerStats・Fighter
├─ Tables/   CombatTables（命中・距離・HP補正・防御相性などの数値）
├─ Logic/    IRandom・HpEval・HitResolver・DamageCalculator
└─ Tests/    NUnit EditMode テスト（25件）
docs/        設計書・実装計画
```

## 設計の要点
- 命中 = `attackPointVal[clamp(ATTACK − DEFENSE + 10, 0, 20)]`（互角で50%）
- 強打は相手が弱るまで通りにくい（`HpVal` が相手のHP状態を参照）＝「ジャブで崩し強打で仕留める」
- ダメージ = 基礎(ジャブ5/フック・ストレート20/アッパー40) × (1 + (パワー+パンチ攻撃値)/100) + max(0, パワー − タフネス)
- HPは頭/腹を独立管理（パラメータ×10）

## テストの実行
Unity Editor で **Window > General > Test Runner > EditMode > Run All**（25件 PASS）。
`BoxingCore` は UnityEngine 非依存のため、ロジックの正しさをこのテストで担保している。

## 環境
Unity 2022.3 LTS（2022.3.62f3 で作成）
