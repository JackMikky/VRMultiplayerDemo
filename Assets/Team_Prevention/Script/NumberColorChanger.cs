using UnityEngine;
using TMPro;

public class NumberColorChanger : MonoBehaviour
{
    public TMP_Text text; // 対象の TextMeshPro

    void Update()
    {
        if (text == null) return;

        // Text に書かれている文字列を数値として読む
        // 例: text.text が "80" とか "120" とか
        if (!int.TryParse(text.text, out int valueFromText))
        {
            // 数字として読めなかった場合は何もしない or デフォルト値で処理
            return;
        }

        // 値を 0～100 に制限
        int v = Mathf.Clamp(valueFromText, 0, 100);

        // 0 → 赤, 100 → 青 のグラデーション
        float t = v / 100f; // 0～1 に正規化
        Color c = Color.Lerp(Color.red, Color.blue, t);

        // カラーコードに変換
        string hex = ColorUtility.ToHtmlStringRGB(c);

        // 色付きで表示（TMP のリッチテキスト）
        text.text = $"<color=#{hex}>{v}</color>";
    }
}