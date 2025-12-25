using TMPro;
using UnityEngine;

public class ResultTextEffect : MonoBehaviour
{
    // 別スクリプトで 0〜100 の値が設定される前提
    // このコンポーネントでは「0〜100の間の値が表示されている」と分かるよう色を赤→青で変化させる
    private TMP_Text tmp;

    // 外部スクリプトから直接 tmp.text に数値が書き込まれているケースにも対応
    // 毎フレーム、現在のテキストを読んで色だけ更新します
    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (tmp == null) return;

        // 現在のテキストから数値を取得（失敗したら何もしない）
        if (!string.IsNullOrEmpty(tmp.text) && float.TryParse(tmp.text, out float value))
        {
            // 0〜100にクランプ
            value = Mathf.Clamp(value, 0f, 100f);

            // 0→赤, 100→青 になるよう補間（中間は紫系）
            float t = Mathf.InverseLerp(0f, 100f, value);
            tmp.color = Color.Lerp(Color.red, Color.blue, t);
        }
    }
}