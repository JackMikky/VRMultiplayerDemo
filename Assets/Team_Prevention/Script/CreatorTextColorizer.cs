using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

[RequireComponent(typeof(TMP_Text))]
public class TMPResultTextColorizer : MonoBehaviour
{
    TMP_Text _tmp;

    // スクリプトから入ってくる「元のテキスト」（色なし）
    string _rawText = "";
    string _prevRawText = "";

    // 現在自分でセットしている「色付きテキスト」
    string _currentColoredText = "";

    void Awake()
    {
        _tmp = GetComponent<TMP_Text>();

        // 起動時に何か入っていたら、それを元テキストにする
        _rawText = _tmp.text;
        _prevRawText = _rawText;

        UpdateColoredText();
    }

    void Update()
    {
        // 1. 他のスクリプトが .text を書き換えたかチェック
        if (_tmp.text != _currentColoredText)
        {
            // → これは「外部が生テキストを書き込んだ」とみなす
            _rawText = _tmp.text;
        }

        // 2. 生テキストに変化があれば、色を付け直す
        if (_rawText != _prevRawText)
        {
            UpdateColoredText();
            _prevRawText = _rawText;
        }
    }

    void UpdateColoredText()
    {
        if (string.IsNullOrEmpty(_rawText))
        {
            _currentColoredText = "";
            _tmp.text = "";
            return;
        }

        string colored = _rawText;

        // ① 「: 数字」の「数字」に色を付ける
        colored = ColorizeColonValues(colored);

        // ② @Spot1, @Spot2, @Spot3 に色を付ける
        colored = ColorizeSpots(colored);

        _currentColoredText = colored;
        _tmp.text = _currentColoredText;
    }

    /// <summary>
    /// 「: 10」みたいな「コロンの右の数字」を 0=赤,100=青 のグラデで色付け
    /// 例: "Unity Creator : 10" → "Unity Creator : <color=#xxxxxx>10</color>"
    /// </summary>
    string ColorizeColonValues(string input)
    {
        // 説明:
        // (:) コロン本体
        // (\s*) コロンの後ろのスペース（0文字でもOK）
        // (\d+) その後ろに続く1桁以上の数字
        return Regex.Replace(
            input,
            @"(:\s*)(\d+)",
            match =>
            {
                string prefix = match.Groups[1].Value; // ": " など
                string numStr = match.Groups[2].Value; // "10" など

                if (!int.TryParse(numStr, out int value))
                    return match.Value;

                value = Mathf.Clamp(value, 0, 100);

                float t = value / 100f;
                Color c = Color.Lerp(Color.red, Color.blue, t);
                string hex = ColorUtility.ToHtmlStringRGB(c);

                // ": " + "<color=#xxxxxx>10</color>"
                return prefix + $"<color=#{hex}>{numStr}</color>";
            }
        );
    }

    /// <summary>
    /// @Spot1 / @Spot2 / @Spot3 に固定の色を付ける
    /// </summary>
    string ColorizeSpots(string input)
    {
        // 好きな色に変えちゃってOK
        input = input.Replace("@Spot1", "<color=#FFFF00>@Spot1</color>"); // 黄色
        input = input.Replace("@Spot2", "<color=#00FF00>@Spot2</color>"); // 緑
        input = input.Replace("@Spot3", "<color=#FF00FF>@Spot3</color>"); // マゼンタ

        return input;
    }
}