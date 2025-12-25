# アイテムシステム クイックスタート

このガイドでは、最短5分で新しいアイテムを追加する手順を説明します。

---

## ?? 5分でアイテムを追加

### 準備するもの

- ? アイテムの3Dモデル（またはスプライト）
- ? UIアイコン画像（PNG/JPG）

---

## 手順

### 1. アイコン画像をインポート（30秒）

1. `Assets/Team_Prevention/Items/Icons/` フォルダを開く
2. アイコン画像をドラッグ&ドロップ
3. 画像を選択 → Inspector で `Texture Type: Sprite (2D and UI)` に変更

### 2. ItemData を作成（1分）

1. `Assets/Team_Prevention/Items/Data/` フォルダを右クリック
2. **Create > Team_Prevention > ItemData**
3. 名前を変更（例: `FireExtinguisher`）
4. Inspector で設定：
   ```
   Name: 消火器
   Icon: （アイコン画像をドラッグ）
   Correct Use Spot Id: 1
   Point: 100
   ```

### 3. アイテムGameObject を作成（2分）

1. Hierarchy で `Items` オブジェクトを右クリック
2. **Create Empty** → 名前を `FireExtinguisher` に変更
3. 3Dモデルを子として追加
4. **Add Component** → `ItemDataHolder` を追加
5. `Item Data` に先ほど作成した `ItemData` をドラッグ

### 4. 配置と確認（1分）

1. アイテムの位置を調整
2. **Play** ボタンでゲーム開始
3. VRコントローラーでアイテムに触れる
4. アイテムが消えればOK！

---

## ? チェックリスト

アイテム追加前に確認：

- [ ] `Items/Data/` フォルダが存在する
- [ ] `Items/Icons/` フォルダが存在する
- [ ] シーンに `ItemSelectionPhaseManager` がある
- [ ] `ItemSelectionPhaseManager` の `Items Container` が設定されている
- [ ] `PlayerInfo` コンポーネントがシーンに存在する

---

## ?? 設定値の例

### よく使う Correct Use Spot Id

| スポットID | 場所 | 説明 |
|-----------|------|------|
| 0 | 避難経路 | 避難経路マーク設置など |
| 1 | 火災現場 | 消火器、消火栓など |
| 2 | 危険エリア | ヘルメット、保護具など |
| 3 | 集合場所 | 誘導灯、拡声器など |

### 推奨ポイント設定

| アイテム重要度 | ポイント |
|--------------|---------|
| 必須アイテム | 100 |
| 重要アイテム | 50 |
| 補助アイテム | 20 |

---

## ?? よくある失敗

### ? アイテムが取得できない

**原因:** ItemDataHolder が設定されていない  
**解決:** アイテムGameObject に **Add Component > ItemDataHolder** を追加

### ? UIに表示されない

**原因:** ItemData の Name が空  
**解決:** ItemData の `Name` フィールドを設定

### ? "Inventory full" エラー

**原因:** 既に5個のアイテムを所持  
**解決:** アイテムを使用または削除

---

## ?? Tips

### ItemDataHolder とは？

- アイテムGameObjectと、ItemData（ScriptableObject）を紐付けるコンポーネント
- シーン内の各アイテムにアタッチして使用します
- `ItemData.cs` に定義されています

### コンポーネントの追加方法

1. アイテムGameObjectを選択
2. Inspector で **Add Component** をクリック
3. 検索欄に「ItemDataHolder」と入力
4. 表示されたコンポーネントをクリック

---

## ?? 次のステップ

- ?? 詳細ガイド: `ItemSystem_Guide.md`
- ?? トラブルシューティング: 上記ドキュメントの該当セクション
- ?? 応用例: アイテムのカテゴリー分けやレアリティシステム

---

## ??? ファイル構造（推奨）

```
Assets/Team_Prevention/
├── Items/
│   ├── Data/                    # ScriptableObject
│   │   ├── FireExtinguisher.asset
│   │   ├── Helmet.asset
│   │   └── ...
│   ├── Prefabs/                 # アイテムPrefab
│   │   ├── FireExtinguisher.prefab
│   │   ├── Helmet.prefab
│   │   └── ...
│   └── Icons/                   # アイコン画像
│       ├── fire_extinguisher_icon.png
│       ├── helmet_icon.png
│       └── ...
└── Script/
    ├── ItemData.cs              # データクラス定義
    ├── ItemSelectionPhaseManager.cs
    ├── PlayerInfo.cs
    └── ...
```

---

**所要時間:** 約5分  
**難易度:** ?☆☆☆☆（初心者向け）  
**対応Unity:** 6.2  
**対応.NET:** Framework 4.7.1
