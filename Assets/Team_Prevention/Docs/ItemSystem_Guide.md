# アイテムシステム使用ガイド

## ?? 目次

1. [概要](#概要)
2. [システム構成](#システム構成)
3. [初回セットアップ](#初回セットアップ)
4. [新しいアイテムの追加方法](#新しいアイテムの追加方法)
5. [トラブルシューティング](#トラブルシューティング)
6. [ベストプラクティス](#ベストプラクティス)

---

## 概要

このアイテムシステムは、VR環境でプレイヤーがアイテムを選択・使用できる仕組みを提供します。

### 主な機能

- ? コントローラー/手でアイテムに触れると自動的にインベントリに追加
- ? ScriptableObjectによる拡張性の高いアイテムデータ管理
- ? 最大5個までのアイテム所持
- ? 正解/不正解スポットでの使用判定
- ? スコア加減算システム

---

## システム構成

### ?? 主要コンポーネント

| コンポーネント | 役割 | アタッチ先 |
|---------------|------|-----------|
| `ItemSelectionPhaseManager` | アイテム選択フェーズの管理 | シーンのManager GameObject |
| `PlayerInfo` | プレイヤーのインベントリ管理 | Player GameObject |
| `ItemBoxComponent` | UI表示とアイテム操作 | UI GameObject |

### ?? データクラス

| クラス | 種類 | 定義場所 | 説明 |
|--------|------|---------|------|
| `ItemData` | ScriptableObject | ItemData.cs | アイテムのマスターデータ |
| `ItemDataHolder` | MonoBehaviour | ItemData.cs | GameObjectとItemDataの紐付け |
| `ItemInfo` | class | ItemData.cs | プレイヤーが所持するアイテムの状態 |
| `ItemProperty` | MonoBehaviour | ItemProperty.cs | アイテムの物理属性（重さなど） |

---

## 初回セットアップ

### 1?? フォルダ構造の作成

以下のフォルダを作成してください：

```
Assets/Team_Prevention/
└── Items/
    ├── Data/       # ItemData ScriptableObjectを保存
    ├── Prefabs/    # アイテムPrefabを保存
    └── Icons/      # UIアイコン画像を保存
```

### 2?? ItemSelectionPhaseManager の設定

1. シーンに空のGameObjectを作成（名前: `ItemSelectionManager`）
2. `ItemSelectionPhaseManager`コンポーネントをアタッチ
3. Inspectorで以下を設定：

   | フィールド | 設定内容 |
   |-----------|---------|
   | Left Controller | 左コントローラーのGameObject |
   | Right Controller | 右コントローラーのGameObject |
   | Left Hand | 左手のGameObject |
   | Right Hand | 右手のGameObject |
   | Items Container | アイテムを格納する親GameObject |
   | Target Player | PlayerInfoコンポーネントを持つGameObject |
   | Item Catalog | （オプション）フォールバック用カタログ |

### 3?? PlayerInfo の確認

`PlayerInfo`コンポーネントがPlayer GameObjectにアタッチされていることを確認してください。

---

## 新しいアイテムの追加方法

### ステップ1: アイコン画像の準備

1. アイテムのアイコン画像（PNG/JPG）を用意
2. `Assets/Team_Prevention/Items/Icons/` にインポート
3. Inspectorで`Texture Type`を`Sprite (2D and UI)`に設定

### ステップ2: ItemData ScriptableObject の作成

1. Projectウィンドウで `Assets/Team_Prevention/Items/Data/` を右クリック
2. **Create > Team_Prevention > ItemData** を選択
3. ファイル名を変更（例: `HelmetData`）
4. Inspectorで設定：

   ```
   Name: ヘルメット
   Icon: helmet_icon.png をドラッグ&ドロップ
   Correct Use Spot Id: 2
   Point: 50
   ```

### ステップ3: アイテムPrefab/GameObjectの作成

#### 方法A: 新規Prefab作成

1. Hierarchyで空のGameObjectを作成（名前: `Helmet`）
2. 3Dモデルやスプライトを子として追加
3. `ItemDataHolder`コンポーネントをアタッチ
4. `Item Data`フィールドに`HelmetData`をアサイン
5. （オプション）`ItemProperty`コンポーネントも追加
6. Prefab化して `Assets/Team_Prevention/Items/Prefabs/` に保存

#### 方法B: 既存GameObjectに追加

1. シーン内の既存アイテムGameObjectを選択
2. `ItemDataHolder`コンポーネントをアタッチ
3. `Item Data`フィールドに作成した`ItemData`をアサイン

### ステップ4: シーンへの配置

1. 作成したPrefabまたはGameObjectを`Items Container`の子として配置
2. 位置を調整

### ステップ5: UI カタログへの追加（オプション）

`ItemBoxComponent`でUI表示する場合：

1. `ItemBoxComponent`がアタッチされているGameObjectを選択
2. Inspector の `Item Catalog` リストに新しい`ItemData`を追加

---

## トラブルシューティング

### ? アイテムが取得できない

**症状:** コントローラー/手で触れてもアイテムが消えない

**原因と対処法:**

1. **ItemDataHolderが設定されていない**
   - アイテムGameObjectに`ItemDataHolder`がアタッチされているか確認
   - `Item Data`フィールドが空でないか確認

2. **距離閾値の問題**
   - `ItemSelectionPhaseManager`の`IsColliding()`メソッド内
   - `distanceThreshold`の値を調整（デフォルト: 0.1f）

3. **Items Containerの設定ミス**
   - アイテムが`itemsContainer`の子として配置されているか確認

### ? コンソールに警告が表示される

**警告: "ItemData not found for item: XXX"**

- **対処法:** アイテムGameObjectに`ItemDataHolder`を追加するか、`itemCatalog`に該当の`ItemData`を追加

**警告: "Inventory full (max 5)."**

- **対処法:** プレイヤーが既に5個のアイテムを所持しています。アイテムを使用または削除してください。

**警告: "Already obtained: XXX"**

- **対処法:** 同じアイテムを重複して取得しようとしています。`PlayerInfo.GetItem()`の重複チェック仕様です。

### ? UIに表示されない

**症状:** アイテムは取得できるがUIに表示されない

**対処法:**

1. `ItemBoxComponent`の`Item Catalog`に`ItemData`が登録されているか確認
2. `ItemBoxComponent.RefreshFromPlayer()`を呼び出してUI更新
3. アイテムの`Name`プロパティが設定されているか確認

---

## ベストプラクティス

### ? 推奨事項

1. **ItemDataHolderの使用**
   - 全てのアイテムGameObjectに`ItemDataHolder`をアタッチすることを推奨
   - カタログ検索はフォールバックとして保持

2. **命名規則**
   - ItemData: `{アイテム名}Data.asset`（例: `HelmetData.asset`）
   - Prefab: `{アイテム名}.prefab`（例: `Helmet.prefab`）
   - アイコン: `{アイテム名}_icon.png`（例: `helmet_icon.png`）

3. **フォルダ整理**
   - ScriptableObjectは`Items/Data/`
   - Prefabは`Items/Prefabs/`
   - アイコンは`Items/Icons/`

4. **バージョン管理**
   - `.asset`ファイルはテキストシリアライズ推奨
   - Edit > Project Settings > Editor > Asset Serialization Mode: `Force Text`

### ?? 注意事項

1. **ItemDataのNameフィールド**
   - UI表示やログに使用されるため、必ず設定してください

2. **Correct Use Spot Id**
   - スポットIDは事前に設計し、ドキュメント化してください

3. **パフォーマンス**
   - `Update()`内で衝突判定を行うため、アイテム数が多い場合は最適化を検討

4. **重複取得**
   - 現在の仕様では、同名で未使用のアイテムは重複して取得できません
   - 仕様変更が必要な場合は`PlayerInfo.GetItem()`を修正

---

## ?? サンプルコード

### ItemDataの動的生成（スクリプトから）

```csharp
// Editor専用（ランタイムでは不可）
#if UNITY_EDITOR
using UnityEditor;
using Assets.Team_Prevention.Script;

ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
// プロパティは private なので、Reflectionまたはpublic setterが必要
// 推奨: Editorで手動作成

AssetDatabase.CreateAsset(newItem, "Assets/Team_Prevention/Items/Data/NewItem.asset");
AssetDatabase.SaveAssets();
#endif
```

### プログラムからアイテムを追加

```csharp
using Assets.Team_Prevention.Script;

// ItemData を取得
ItemData helmData = Resources.Load<ItemData>("Items/Data/HelmetData");

// PlayerInfo に追加
PlayerInfo player = FindObjectOfType<PlayerInfo>();
bool success = player.GetItem(helmData);

if (success)
{
    Debug.Log("アイテムを追加しました");
}
```

### ItemBoxComponent のリフレッシュ

```csharp
using Assets.Team_Prevention.Script.UI;

ItemBoxComponent itemBox = FindObjectOfType<ItemBoxComponent>();
itemBox.RefreshFromPlayer();
```

---

## ?? 関連ファイル

| ファイル | 説明 |
|---------|------|
| `ItemData.cs` | アイテムデータの定義（ItemData, ItemDataHolder, ItemInfoを含む） |
| `ItemSelectionPhaseManager.cs` | アイテム選択フェーズの管理 |
| `PlayerInfo.cs` | プレイヤー情報とインベントリ管理 |
| `ItemBoxComponent.cs` | アイテムUIの制御 |
| `ItemProperty.cs` | アイテムの物理属性 |

---

## ?? サポート

問題が解決しない場合は、以下を確認してください：

1. Unity Console のエラーメッセージ
2. `ItemSelectionPhaseManager` のログ出力（`[ItemSelectionPhaseManager]`で検索）
3. 各GameObjectのInspectorでコンポーネントの設定

---

**最終更新日:** 2024
**対応Unityバージョン:** Unity 6.2
**対応.NET Framework:** 4.7.1
