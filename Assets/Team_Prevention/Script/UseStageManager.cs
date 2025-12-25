using TMPro;
using UnityEngine;
using System.Collections.Generic; // Listを使用するため

public class UseStageManager : MonoBehaviour
{
    #region <設定可能パラメータ>
    [Tooltip("最大体力")]
    public float maxHealth;
    #endregion <設定可能パラメータ>

    [Tooltip("体力表示Text")]
    public TMP_Text healthText;

    [Tooltip("シーンオブジェクト")]
    public GameObject sceneObject;

    #region <変数>
    public float currentHealth;

    // 処理済みアイテムを記録するリスト
    public HashSet<GameObject> processedItems;
    #endregion <変数>

    private void Start()
    {
        Debug.Log($"Start");
        currentHealth = maxHealth;

        healthText.text = maxHealth.ToString();

        // 処理済みアイテムのリストを初期化
        processedItems = new HashSet<GameObject>();
    }

    // 属性判定結果を表す列挙型
    public enum AttributeMatchingResult
    {
        Matching,    // 合致している場合
        NotMatching, // 合致していない場合
        Entertainment // エンタメの場合
    }

    // アイテム属性とシーン属性が合致しているか判定するメソッド
    public AttributeMatchingResult CheckAttributeMatching(ItemProperty.ItemAttribute itemAttribute, SceneProperty.SceneAttribute sceneAttribute)
    {
        // エンタメの場合は特別に処理
        if (itemAttribute == ItemProperty.ItemAttribute.Entertainment)
        {
            Debug.Log("アイテム属性がEntertainmentのため、スコア変動なし");
            return AttributeMatchingResult.Entertainment; // エンタメの場合を返す
        }

        // 属性の一致判定ロジック
        switch (sceneAttribute)
        {
            case SceneProperty.SceneAttribute.Fire:
                if (itemAttribute == ItemProperty.ItemAttribute.Erase)
                {
                    return AttributeMatchingResult.Matching; // 属性が合致している場合
                }
                return AttributeMatchingResult.NotMatching; // 合致していない場合

            case SceneProperty.SceneAttribute.Escape:
                if (itemAttribute == ItemProperty.ItemAttribute.RunAway)
                {
                    return AttributeMatchingResult.Matching; // 属性が合致している場合
                }
                return AttributeMatchingResult.NotMatching; // 合致していない場合

            default:
                return AttributeMatchingResult.NotMatching; // その他は合致していないとする
        }
    }

    // 衝突判定の処理を追加
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"OnCollisionEnter！");

        // 衝突したオブジェクトのTagが"Item"なら処理を実行
        if (collision.gameObject.CompareTag("Item"))
        {
            // すでに処理済みのアイテムなら処理をスキップ
            if (processedItems.Contains(collision.gameObject))
            {
                Debug.Log($"このアイテムはすでに処理済みです: {collision.gameObject.name}");
                return;
            }

            // 衝突したオブジェクトのItemPropertyコンポーネントを取得
            ItemProperty itemProperty = collision.gameObject.GetComponent<ItemProperty>();
            Debug.Log($"After ItemProperty");

            // 現在のシーン属性を取得（sceneObjectにScenePropertyがアタッチされている前提）
            SceneProperty sceneProperty = sceneObject.GetComponent<SceneProperty>();
            Debug.Log($"After SceneProperty");

            // 安全チェック：アイテムとシーンのプロパティが存在するか確認
            if (itemProperty != null && sceneProperty != null)
            {
                // アイテム属性とシーン属性の判定結果を取得
                AttributeMatchingResult matchingResult = CheckAttributeMatching(itemProperty.itemAttribute, sceneProperty.sceneAttribute);

                // 判定結果に応じた処理
                switch (matchingResult)
                {
                    case AttributeMatchingResult.Matching:
                        // 属性が合致している場合、currentHealthからeffectiveGetPointを引く
                        currentHealth -= itemProperty.effectiveGetPoint;
                        Debug.Log($"属性が一致しました！currentHealthを減少: {currentHealth}");
                        break;

                    case AttributeMatchingResult.NotMatching:
                        // 属性が合致していない場合、nothingLosePointをcurrentHealthに足す
                        currentHealth += itemProperty.noEffectiveLosePoint;
                        Debug.Log($"属性が一致しませんでした。currentHealthを増加: {currentHealth}");
                        break;

                    case AttributeMatchingResult.Entertainment:
                        // エンタメの場合はスコア変動なし
                        Debug.Log($"エンタメアイテムのため、スコア変動なし: {currentHealth}");
                        break;
                }

                // healthTextを更新
                healthText.text = currentHealth.ToString();

                // 処理済みアイテムとして記録
                processedItems.Add(collision.gameObject);
            }
            else
            {
                Debug.LogWarning("ItemPropertyまたはScenePropertyが見つかりませんでした。処理をスキップします。");
            }
        }
    }

    // 今の状況(Scene)に対して使用するアイテムを参照し、スコア設定
    public float JudgementItem(ItemProperty targetItem, SceneProperty currentScene)
    {
        // アイテムとシーン属性が合致しているか判定
        AttributeMatchingResult matchingResult = CheckAttributeMatching(targetItem.itemAttribute, currentScene.sceneAttribute);

        switch (matchingResult)
        {
            case AttributeMatchingResult.Matching:
                return currentHealth + targetItem.effectiveGetPoint; // 合致している場合ポイント加算

            case AttributeMatchingResult.NotMatching:
                return currentHealth - targetItem.noEffectiveLosePoint; // 合致していない場合ポイント減算

            case AttributeMatchingResult.Entertainment:
            default:
                return currentHealth; // エンタメの場合はスコア変動なし
        }
    }
}