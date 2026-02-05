using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.UIElements;

public class Tex3DSectionController:MonoBehaviour
{
    [Header("Slider References")]
    public NetworkSimpleXRSlider minXSlider;

    [Header("Slider References")]
    public NetworkSimpleXRSlider maxXSlider;

    [Header("Slider References")]
    public NetworkSimpleXRSlider maxYSlider;

    [Header("MinX (0..1)")]
    [UnityEngine.Range(0f, 1f)]
    public float _minX = 0f;

    [Header("MaxX (0..1)")]
    [UnityEngine.Range(0f, 1f)]
    public float _maxX = 1f;

    [Header("MinY (0..1)")]
    [UnityEngine.Range(0f, 1f)]
    public float _minY = 0f;

    [Header("MaxY (0..1)")]
    [UnityEngine.Range(0f, 1f)]
    public float _maxY = 1f;

    [Header("MinZ (0..1)")]
    [UnityEngine.Range(0f, 1f)]
    public float _minZ = 0f;

    [Header("MaxZ (0..1)")]
    [UnityEngine.Range(0f, 1f)]
    public float _maxZ = 1f;

    [Header("Shader Property Name MinX")]
    public string shaderPropertyName_MinX = "_MinX";
    [Header("Shader Property Name MaxX")]
    public string shaderPropertyName_MaxX = "_MaxX";
    [Header("Shader Property Name MinY")]
    public string shaderPropertyName_MinY = "_MinY";
    [Header("Shader Property Name MaxY")]
    public string shaderPropertyName_MaxY = "_MaxY";
    [Header("Shader Property Name MinZ")]
    public string shaderPropertyName_MinZ = "_MinZ";
    [Header("Shader Property Name MaxZ")]
    public string shaderPropertyName_MaxZ = "_MaxZ";

    [SerializeField]
    private List<Renderer> targets = new(6);

    [Header("Z Segments (mm) - targets と同じ順番で並べる")]
    [Tooltip("各オブジェクトの Z 長 (mm)。例: [234, 450, ...]")]
    public List<float> zLengthsMm = new List<float>()
    {
        234f, 450f, 150f, 400f, 350f, 150f
    };


    // 各Renderer用のMPB（共有マテリアルでも個別値を持てる）
    private MaterialPropertyBlock[] mpbs;


    void OnEnable()
    {
        EnsureTargetsAndBlocks();
        Apply();
    }

    void OnValidate()
    {
        EnsureTargetsAndBlocks();
        Apply();
    }

    private void Update()
    {
        if (maxXSlider != null)
        {
            _maxX = maxXSlider.value.Value;
        }

        if (minXSlider != null)
        {
            _minX = minXSlider.value.Value;
        }

        if (maxYSlider != null)
        {
            _maxY = maxYSlider.value.Value;
        }

        Apply();
    }


    /// <summary>
    /// インスペクタ未設定なら直下の子からRendererを自動収集。
    /// MPB配列をターゲット数に合わせて確保。
    /// </summary>
    void EnsureTargetsAndBlocks()
    {
        if (targets == null) targets = new List<Renderer>();

        // 未設定なら親直下の子から拾う（必要なら手動設定に切り替え可）
        if (targets.Count == 0)
        {
            targets.Clear();
            foreach (Transform child in transform)
            {
                var r = child.GetComponent<Renderer>();
                if (r != null) targets.Add(r);
            }
        }

        // MPBをターゲット数に合わせて確保
        if (mpbs == null || mpbs.Length != targets.Count)
        {
            mpbs = new MaterialPropertyBlock[targets.Count];
            for (int i = 0; i < targets.Count; i++)
                mpbs[i] = new MaterialPropertyBlock();
        }
    }
    

    /// <summary>
    /// すべてのtargetのシェーダーのRangeを反映
    /// </summary>
    private void Apply()
    {
        ApplyXY();
        ApplyZ();
    }

    // 外部から安全にセットしたい場合のヘルパー
    // もし使いそうなら他のminmaxも用意する
    public void SetMinX(float minX)
    {
        _minX = Mathf.Clamp01(minX);
        Apply();
    }

    /// <summary>
    /// XY方向のスライダーの値をシェーダーに適用する
    /// </summary>
    private void ApplyXY()
    {
        if (targets == null || targets.Count == 0) return;

        float minX = Mathf.Clamp01(_minX);
        float maxX = Mathf.Clamp01(_maxX);
        float minY = Mathf.Clamp01(_minY);
        float maxY = Mathf.Clamp01(_maxY);

        for (int i = 0; i < targets.Count; i++)
        {
            var r = targets[i];
            if (r == null) continue;

            // 既存のブロックを取得 → 値を設定 → 反映
            var block = mpbs[i];
            r.GetPropertyBlock(block);
            block.SetFloat(shaderPropertyName_MinX, minX);
            block.SetFloat(shaderPropertyName_MaxX, maxX);
            block.SetFloat(shaderPropertyName_MinY, minY);
            block.SetFloat(shaderPropertyName_MaxY, maxY);

            // 1レンダラーに複数マテリアルがある場合でも
            // MaterialPropertyBlockはレンダラー単位で適用可能
            r.SetPropertyBlock(block);
        }
    }

    /// <summary>
    /// Ｚ方向のスライダーの値をシェーダーに適用する
    /// </summary>
    private void ApplyZ()
    {
        //var totalLength = GetTotalLengthMm();
        //if (totalLength <= 0f)
        //{
        //    _minZ = 0;
        //    _maxZ = 1;
        //    return;
        //}

        //var minZMm = _minZ * totalLength;

        //// 境界を探索
        //var acc = 0f;
        //var targetIndex = 0;
        //var remaindVal = 0f;
        //for(var i = 0; i < zLengthsMm.Count; i++)
        //{
        //    acc += zLengthsMm[i];

        //    if(minZMm <= acc)
        //    {
        //        targetIndex = i;
        //        remaindVal
        //        break;
        //    }
        //}


        // 0-1の値を合計値の該当する区間を求める。
        // 区間に応じて各targetのZをセットする（minZ、maxZを6つずつ定義すればよいはず。
        // 全身出すのがスペック的に苦しくなるなら1つ1つでいいかもな。
    }



    private float GetTotalLengthMm()
    {
        var sum = 0f;
        foreach(var length in zLengthsMm)
        {
            sum += length;
        }

        return sum;
    }

}
