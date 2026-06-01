# 設計書：BOXING ARENA NEBULA 戦闘シーンの Unity 忠実再現（メカニクス重視プロトタイプ）

- 作成日: 2026-06-01
- 対象: BAN（BOXING ARENA NEBULA）の **1試合の戦闘** を Unity 2D で忠実再現する検証用プロトタイプ
- 元解析: [解析結果_BoxingArenaNebula.md](../../../解析結果_BoxingArenaNebula.md)、抽出データ `_analysis/extracted/`

## 1. 目的とスコープ

### 目的
BAN の戦闘の**挙動（読み合い・命中・ダメージ・AI）を忠実に再現**し、「このシステムが本当に面白いか」を Unity 上でプレイして検証する。将来は自作ゲームへ移植・拡張する土台にする。

### スコープ（含む）
- 1試合：**プレイヤー(P1) vs CPU(P2)**
- ラウンド→ターン(最大12)→サブターン(最大4) の進行
- 攻撃12種・防御6種のコマンド、距離/位置、命中判定、ダメージ、KO/ダウン、ラウンド間回復、判定勝敗
- CPU AI（確率テーブル抽選＋戦術オーバーライド）
- コンビネーション・必殺ブロー・各スキル効果（フェーズ2）
- シンプルUI（HPバー・距離表示・コマンド・実況ログ・必殺ゲージ）

### スコープ外（今回作らない）
- キャラ立ち絵・技絵・ヒットエフェクト等の**視覚層**（将来のフェーズ3）
- 育成/ランキング/興行などのメタゲーム層
- BAN の著作物（キャラ名・画像・テキスト）の使用 → **数値パラメータのみ流用、名前は仮、絵なし**

## 2. アーキテクチャ

純C#のロジック層を Unity から完全分離する（BAN の TJSロジック層／KAG表示層 の分離に対応）。

```
Assets/
├─ BoxingCore/              ★純C#（UnityEngine参照なし／asmdefで強制）
│   ├─ Model/    PunchType, DefenseType, SkillType (enum), BoxerStats, Fighter, MatchState
│   ├─ Tables/   CombatTables（全変換テーブル＋AIテーブルのプレーン配列）
│   └─ Logic/    IRandom, HitResolver, DamageCalculator, DistanceSystem,
│                AiController, CombinationSystem, SpecialBlowSystem, MatchEngine
├─ BoxingUnity/             Unity依存層
│   ├─ Data/     BoxerData(SO), CombatConfig(SO), SO→Core 変換
│   ├─ Scene/    BattleSceneController ＋ UIバインド
│   └─ Resources/Boxers/  仮ボクサーSO（BAN実数値・仮名）
└─ Tests/                  BoxingCore の EditMode テスト
```

- **BoxingCore**：全計算を集約。乱数は `IRandom` で注入（テストで固定可能）。Unity非依存。
- **BoxingUnity**：コアを駆動し、UI描画とプレイヤー入力のみ担当。
- **データは ScriptableObject**：ボクサー(`BoxerData`)と調整テーブル(`CombatConfig`)をエディタで編集可能に。

## 3. コアエンジン設計（BoxingCore）

### 3.1 Model

```csharp
enum PunchType { Jab=1, Hook=2, Straight=3, Upper=4,
                 StepBack=5, StepIn=6, Recover=7, Clinch=8,
                 BodyJab=9, BodyHook=10, BodyStraight=11, BodyUpper=12 }
enum DefenseType { CounterUp=1, GuardUp=2, Sway=3, Duck=4, CounterLow=5, GuardLow=6 }
```

- `BoxerStats`（不変・SO由来）: hpHead, hpBody, power, speed, stamina, tuffness, counter,
  jabAtk, jabDef, hookAtk, hookDef, straightAtk, straightDef, upperAtk, upperDef,
  skill[3], atkLogic, defLogic, spblow(必要HIT/攻撃1/攻撃2/防御/背景/距離/タイプ/ラッシュ)
- `Fighter`（試合中の可変状態）:
  - headHP, bodyHP, headHPMax(=hpHead×10), bodyHPMax(=hpBody×10)
  - hitCount, counterCount, hookCount, straightCount, downCount, spPts
  - tuffnessAvailable(ラウンド毎にtrue), bleeding 等のフラグ
  - stats(BoxerStats 参照)
  - ※距離/位置は両者共有のため `MatchState` 側で一元管理（Fighter には持たせない）
- `MatchState`: fighters[2], distance(1近/2中/3遠), position(P1基準 1〜7), round, turn, subturn,
  judgeScore[2], maxRound

> ※BAN準拠：**実HP = パラメータ×10**。頭HP・腹HPは独立管理。

### 3.2 Tables（CombatConfig SO 由来。index 0 は未使用、1..4=ジャブ/フック/ストレート/アッパー、9..12=ボディ各種）

```
attackPointVal[0..20] = {0,.05,.1,.15,.2,.25,.3,.35,.4,.45,.5,.6,.7,.8,.9,1,1,1,1,1,1}
distanceVal[1(近)]  = {0,  0, -5,-10, -3,0,0,0,0,  1, -3, -8, -2}
distanceVal[2(中)]  = {0,  0,  0, -5, -7,0,0,0,0,  0,  0, -4,-10}
distanceVal[3(遠)]  = {0, -1, -5,  2,-15,0,0,0,0, -2, -7,  0,-20}
hpVal[1]  = {0,0,0,0,0,0,0,0,0,0,0,0,-3}      (相手HP状態1=頭ピンチ)
hpVal[2]  = {0,0,0,0,-3,0,0,0,0,0,0,0,0}
hpVal[3]  = {0,0,0,0,0,0,0,0,0,0,0,0,0}        (頭腹ピンチ)
hpVal[4]  = {0,0,0,0,-5,0,0,0,0,0,-3,-3,-5}
hpVal[5]  = {0,0,-3,-3,-5,0,0,0,0,0,0,0,-5}
hpVal[6]  = {0,0,0,0,-5,0,0,0,0,0,0,0,-5}
hpVal[7]  = {0,0,-5,-5,-10,0,0,0,0,0,-4,-4,-12}
hpVal[8]  = {0,0,-4,-4,-12,0,0,0,0,0,-5,-5,-10}
hpVal[9]  = {0,0,-5,-5,-10,0,0,0,0,0,-5,-5,-10}
hpVal[10] = {0,0,-10,-10,-15,0,0,0,0,0,-10,-10,-15}  (相手ほぼ無傷)
defenseVal[1(上ｶｳﾝﾀｰ)] = {0, 0, 1, 1, 1,0,0,0,0,-5,-5,-5,-5}
defenseVal[2(上ｶﾞｰﾄﾞ)] = {0, 5, 5, 5, 5,0,0,0,0,-3,-3,-3,-3}
defenseVal[3(ｽｳｪｰ)]    = {0, 0, 0,-3, 0,0,0,0,0, 0, 0,-2, 0}
defenseVal[4(ﾀﾞｯｷﾝｸﾞ)] = {0,-1,-1, 0, 0,0,0,0,0,-2,-2,-2, 0}
defenseVal[5(下ｶｳﾝﾀｰ)] = {0,-5,-5,-5,-5,0,0,0,0, 0, 1, 1, 1}
defenseVal[6(下ｶﾞｰﾄﾞ)] = {0,-3,-3,-3,-3,0,0,0,0, 5, 5, 5, 5}
counterPenaltyVal[1(通常)] = {0,0,0,0,0,-3,-6,-9,-12,-15,-18,-18,-18}  // index=counterCount
counterPenaltyVal[2(ｽｷﾙ)]  = {0,0,0,0,0,0,0,0,-3,-6,-9,-12,-15}
onePaternPenaltyVal[1] = {0×11, -2,-4,-6,-8,-10×10}   // index=同パンチ連打数(hook/straight)
onePaternPenaltyVal[2] = {0×17, -2,-4,-6,-8,-10×4}    // コンビスキル時
staminaVal = {1, 0.8, 0.6, 0.4, 0.2, 0}               // index=stamina_pts(0..5)
downRecover[0] = {0,0.1,0.1,0.25,0.25,0.3,0.3}        // index=downCount
downRecover[1] = {0,0.025,0.05,0.1,0.25,0.3,0.3}
```
- AIテーブル `atkLogic[style][hpEval(1..10)][dist(1..3)][候補技]`、`defLogic[...]` は大きいため
  CombatConfig に **BAN実数値で初期投入**（出典：`_analysis/extracted/data/scenario_data.ks` 1058-1239行）。
- パンチ→攻撃ステータス対応：1/9→jabAtk, 2/10→hookAtk, 3/11→straightAtk, 4/12→upperAtk。
  防御ステータス：jabDef/hookDef/straightDef/upperDef。

### 3.3 HitResolver（命中判定）

```
para  = 攻撃側の該当パンチ攻撃値、defp = 防御側の該当パンチ防御値
// ジャブ(1/9)は (para-defp) を ±4 に丸める
ATTACK  = para + distanceVal[dist][p] + hpVal[相手HPeval][p]
        + onePaternPenalty + combiAdd + skillBody(+1) + aisyou(+1)
DEFENSE = defp + defenseVal[def][p] + cornerVal(-5) + staminaHosei
        + gyakkyou(+2) + counterPenalty + sioPenalty + skillDodge(+1,動体視力) + skillGuard(+1,鉄壁)
ATK_DEF = clamp(ATTACK - DEFENSE + 10, 0, 20)
hitProb = attackPointVal[ATK_DEF]
return rng.NextDouble() < hitProb ? Hit : DefenseSuccess
```
- combiAdd：コンビ成功時 攻撃1段目+4 / 2段目+8
- staminaHosei：`stamina_pts = clamp(防御側stamina+5,0,5)`, `level = min(0, 防御側stamina-攻撃側stamina)`,
  `hosei = round(staminaVal[stamina_pts] * level)`（疲れている側は被弾しやすい）

### 3.4 DamageCalculator

```
通常: base = (1,9)→5 / (2,3,10,11)→20 / (4,12)→40
      add  = max(0, 攻撃側power - 防御側tuffness)
      dmg  = floor(base × (1 + (攻撃側power + 該当パンチ攻撃値)/100) + add)
      防御側がカウンター選択(def 1/5)で被弾 → dmg = round(dmg × 1.3)
カウンター成功: base = (1,6,8,9)→5 / (2,3,10,11)→15 / (4,12)→25
      dmg = floor(base × (1 + (反撃側power + 反撃側counter)/100) + max(0, power - 相手tuffness))
      def1=頭, def5=腹 に適用
適用: 頭技(1-4)→headHP, ボディ技(9-12)→bodyHP を減算
KO例外(HP=1で止める): ジャブ(1)/ボディジャブ(9)・ガード削り・ステップイン阻止(6)・タフネススキル発動
```

### 3.5 DistanceSystem
- position(1〜7, P1基準)とdistance(1近/2中/3遠)を更新。
- ステップイン：距離1なら position++（コーナーへ押す）、それ以外は position++ かつ distance--。
- ステップバック：自分コーナーなら不可、距離3なら position--、それ以外は distance++ かつ position--。
- コーナー判定：position 5&dist3 / 6&dist2 / 7 → 詰められ側 cornerVal=-5。スキル「コーナー」で無効。

### 3.6 AiController
- **基本抽選（inverse-CDF サンプリング）**：
  ```
  table = (攻撃) atkLogic[style][相手HPeval][dist] / (防御) defLogic[style][自分HPeval][dist]
  T = rng.NextDouble();  loop=1..N: if table[loop] >= T → 採用(=その技No)
  ```
  攻撃技2手・防御1手をこの方式で決定。
- **オーバーライド（優先順）**：コーナーで後退→前進化／瀕死(HPeval≤3)で10%クリンチ／50%でコンビネーション採用／必殺発動可能(pt&距離)時25%で必殺。
- スタイル：1=標準, 2=インファイト, 3=アウトレンジ（テーブルが距離操作を内包）。

### 3.7 MatchEngine（ターン進行）
- 構造：ラウンド(1..maxRound) → ターン(1..12) → サブターン(1..4)。
- 行動順：脚力(speed)で `move_No`（4サブターンへP1攻撃2・P2攻撃2を配列）を決定。
- HP評価：頭/腹をそれぞれ4段階(≤25/≤50/≤75/>75%)に量子化し、(head,body)→1〜10 へ合成
  （両ピンチ→3, 頭のみ→1, 腹のみ→2, 両≤50→6, 頭≤50→4, 腹≤50→5, 両≤75→9, 頭≤75→7, 腹≤75→8, それ以外→10）。
- ラウンド間回復：`reduce = R1:4/R2:6/R3:8/R4:10/R5+:12`、`HP += (max-HP)/reduce + stamina`（最大で頭打ち）。
- コンビ発生率：T1=0 / T2-4=0.3 / T5-7=0.4 / T8-10=0.5 / T11-12=1.0（コンビスキルでT3,T7=1.0）。
- 判定：全ラウンド終了で judgeScore[0] vs [1]、高い方勝ち・同点ドロー。
- KO：頭HPか腹HPが0でダウン。ダウン規定回数(例:3)で TKO。

### 3.8 IRandom とイベント
```csharp
interface IRandom { double NextDouble(); int Next(int maxExclusive); }
// 本番: UnityRandom / System.Random、テスト: 固定列を返すFakeRandom
```
- MatchEngine はプレイヤー入力を**コールバック/状態**で要求し、解決結果を**イベント列**で返す
  （`ActionResolved`, `DamageDealt`, `Down`, `RoundEnded`, `MatchEnded`）。同期的な状態機械として実装しテスト可能に保つ。

## 4. Unity 層（BoxingUnity）

### 4.1 シーン/UI（uGUI・2D・プレースホルダ）
- HPバー×4（P1/P2 × 頭/腹、Image fill）
- 距離/位置インジケータ（近中遠＋ring位置）
- Round/Turn 表示、判定スコア表示
- コマンドパネル：攻撃1・攻撃2・防御の選択（Dropdown/ボタン）＋[決定]
- 実況ログ（Scroll Text）、必殺ゲージ
- 操作：プレイヤー=P1、CPU=P2。決定後にCPUの手を表示するオプション。

### 4.2 BattleSceneController（MonoBehaviour）
- `MatchEngine` を所有。UI参照を保持。
- コルーチンで進行：ターン開始→AIがP2決定→コマンドパネル有効化→プレイヤーが[決定]→
  両者コマンドをエンジンへ→返ったイベント列を**1つずつ遅延付きで再生**（HPバーアニメ／ログ追記／待ち）→
  ダウン/KO判定→次ターン→…→インターバル回復→次ラウンド→判定。
- 速度/オート/ログ表示の簡易設定。

## 5. ScriptableObject データ設計

- `BoxerData : ScriptableObject`：能力15＋skill[3]＋atk/defLogic＋必殺設定。`ToBoxerStats()` でコアへ変換。
  仮ボクサーをBAN実数値で数体（例 `BoxerA`=バランス/チャンピオン型, `BoxerB`=防御型）。名前は仮・絵なし。
- `CombatConfig : ScriptableObject`：3.2 の全テーブル＋AIテーブルをBAN実数値で初期投入。バランス/AI調整用。
- `enum`（PunchType/DefenseType/SkillType）でインスペクタを読みやすく。

## 6. 実装フェーズ

- **フェーズ1（最初のプラン・通しプレイ可能）**：Model／CombatConfig／HitResolver／DamageCalculator／
  DistanceSystem／MatchEngine（進行・行動順・HP評価・回復・判定）／基本AI（確率テーブルのみ）／KO・ダウン
  ＋ 最小UI ＋ コアのEditModeテスト。
- **フェーズ2（作り込み）**：AIオーバーライド／CombinationSystem／SpecialBlowSystem／各スキル効果(約20種)／
  コーナー・流血等の細部 ＋ UI演出（必殺ゲージ・CPU手表示・速度/オート）。
- **フェーズ3（対象外・将来）**：立ち絵・技絵・ヒットエフェクト等の視覚層。

## 7. テスト方針
- BoxingCore に EditMode（Unity Test Framework / NUnit）テスト。`IRandom` 固定で決定論的に検証：
  - HitResolver：与えた ATTACK/DEFENSE → ATK_DEF → attackPointVal の通りの命中率/分岐になるか
  - DamageCalculator：既知ステータスでの基礎/倍率/タフネス/カウンター×1.3 の数値一致
  - KO例外：ジャブ・削り・ステップイン阻止・タフネスでダウンしない
  - AiController：CDF抽選が `table[i]-table[i-1]` の分布になるか
  - HP評価：頭/腹の象限→1〜10 の写像が正しいか

## 8. データ／著作権の注意
- 使用するのは BAN から解析した**数値パラメータのみ**。キャラ名は仮、画像・テキスト等の著作物は使用しない。
- このプロトタイプは個人の学習・検証目的。再配布や BAN 資産の流用は行わない。

## 9. 前提・未決事項
- 私の環境では Unity をビルド/実行できないため、生成物（C#スクリプト＋セットアップ手順＋SO定義）を
  ユーザーが Unity で開いて動作確認する。対象 Unity バージョンは LTS（例：2022.3 LTS）を想定。
- maxRound・ダウンTKO回数・各スキルの厳密効果は、フェーズ進行に合わせて BAN 解析値で確定する。
- 判定スコア(judgeScore)の集計式は BAN で完全解読できていない（ラウンド毎に優劣を加算する方式）。
  フェーズ1では暫定として「ラウンド終了時の残HP割合の優劣＋そのラウンドの命中数差」で加算し、後で解析値に合わせて差し替える。
- 本フォルダは Git 管理外のため、本仕様書のコミットは行わない（必要なら `git init` を別途提案）。
