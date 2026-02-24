# アイテムエフェクトシステム 使い方ガイド

このガイドでは、アイテム使用時にSpot固有のエフェクト（炎の縮小、煙の発生など）を設定する方法  を説明します。

---

## 概要

できること

- アイテムごとに異なるエフェクトを設定できる
- 炎を小さくする、煙を発生させるなどの視覚効果をSpotに連動して発動できる
- 正しいSpotでのみエフェクトが発動する
- 複数のエフェクト対象を個別に制御できる

システム構成

```
ItemData (ScriptableObject) -- エフェクト種別を定義
    ↓
ItemEffectTrigger (アイテムに付与) -- トリガー入力を検知
    ↓
SpotEffectController (Spotに配置) -- エフェクトを実行
    ↓
実際のエフェクト（ParticleSystem, Transform 等）
```

---

## クイックスタート（概略）

必要なもの

- エフェクト対象オブジェクト（炎、帆など）
- パーティクルシステム（煙、水など）
- アイテムのItemData（ScriptableObject）

ステップ1: ItemData の設定

1. `Assets/Team_Prevention/Items/Data/` から対象の ItemData を選択
2. Inspector の Effect Settings で以下を設定する：
   - Effect Type: ReduceFlame / GenerateSmoke 等
   - Effect Target Name: 対象の識別名（例: Flame01）
   - Effect Intensity: 0.0 ～ 1.0

Effect Type の一例

- None: エフェクトなし
- ReduceFlame: 炎を小さくする
- GenerateSmoke: 煙を発生させる
- ReduceSail: 帆を小さくする
- SprayWater: 水を放出する
- HideObject / ShowObject: 表示/非表示切替

設定例

- 消火器
  - Effect Type: ReduceFlame
  - Effect Target Name: Flame01
  - Effect Intensity: 0.3

- 発煙筒
  - Effect Type: GenerateSmoke
  - Effect Target Name: Smoke01
  - Effect Intensity: 1.0

ステップ2: SpotEffectController の設定

1. Spot（Phase1Object など）に `SpotEffectController` を追加
2. Spot Id を設定（Phase1=1, Phase2=2, Phase3=3）
3. 対応するエフェクト対象リストを設定する
   - Flame Targets: Target に炎の GameObject、Target Name を ItemData の Effect Target Name と一致させる
   - Smoke Targets: ParticleSystem を割り当てる
   - Sail Targets / Water Targets / Toggle Targets も同様に設定

注意点: Effect Target Name が空だとそのセクション内の全ての対象に対して処理が実行されます。個別制御したい場合は必ず名称を設定してください。

ステップ3: 動作確認

1. Play モードでゲームを開始
2. アイテムを掴んで正しいSpotに移動
3. トリガーボタンを押す

期待される挙動

- 正しいSpot: 指定のエフェクトが発動（炎縮小、煙発生等）し、PlayerInfo による HP/スコア処理が行われる
- 間違ったSpot: エフェクトは発動せず  、HP減少・不正解音などの処理が行われる

---

## 設定パターン例

1. 特定の炎だけを縮小する
   - ItemData: Effect Type = ReduceFlame, Effect Target Name = Kitchen_Flame_01, Effect Intensity = 0.1
   - SpotEffectController の Flame Targets に Kitchen_Flame_01 を追加

2. 全ての煙を発生させる
   - ItemData: Effect Type = GenerateSmoke, Effect Target Name = (空)
   - SpotEffectController の Smoke Targets に複数の ParticleSystem を登録

3. 段階的に消火する
   - 小型消火器: Effect Intensity = 0.7
   - 中型消火器: Effect Intensity = 0.4
   - 大型消火器: Effect Intensity = 0.1

---

## 詳細設定

- Transform Duration（変形時間）: SpotEffectController の Transform Duration を調整（例: 2.0 秒）
- Transform Curve（イージング）: AnimationCurve で加減速を設定
- Effect Intensity: 縮小率やパーティクル放出量の倍率（0.0 = 完全 / 1.0 = 変更なし）

---

## トラブルシューティング

エフェクトが動作しない場合は以下を確認してください。

1. Spot ID の不一致
   - ItemData の Correct Use Spot Id と SpotEffectController の Spot Id を確認
2. Effect Target Name の不一致
   - ItemData と SpotEffectController の Target Name を完全一致させる（大文字小文字区別）
3. 対象が未設定
   - SpotEffectController の該当セクションに Target / ParticleSystem を設定する
4. パーティクル未再生
   - ParticleSystem が有効で、Emission Rate が 0 でないことを確認する

---

## デバッグ

- SpotEffectController の Debug Mode を有効化すると Console に発火ログが出力されます。
- エフェクトのリセットは SpotEffectController のコンテキストメニューから実行できます（Reset All Effects）。

---

## ファイル構成（参考）

```
Assets/Team_Prevention/
├── Script/
│   ├── ItemEffectType.cs
│   ├── ItemData.cs
│   ├── SpotEffectController.cs
│   └── ItemEffectTrigger.cs
└── Docs/
    └── ItemEffectSystem_Guide.md
```

---
