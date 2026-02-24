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

    [Header("Z Segments (mm) - targets �Ɠ������Ԃŕ��ׂ�")]
    [Tooltip("�e�I�u�W�F�N�g�� Z �� (mm)�B��: [234, 450, ...]")]
    public List<float> zLengthsMm = new List<float>()
    {
        234f, 450f, 150f, 400f, 350f, 150f
    };

 // 前回値キャッシュ
    private float prevMinX, prevMaxX, prevMinY, prevMaxY;

    // �eRenderer�p��MPB�i���L�}�e���A���ł��ʒl�����Ă�j
    private MaterialPropertyBlock[] mpbs;


    void OnEnable()
    {
        EnsureTargetsAndBlocks();
        ForceApplyXY();
        prevMinX = _minX; prevMaxX = _maxX; prevMinY = _minY; prevMaxY = _maxY;
    }

    void OnValidate()
    {
        EnsureTargetsAndBlocks();
        ForceApplyXY();
        prevMinX = _minX; prevMaxX = _maxX; prevMinY = _minY; prevMaxY = _maxY;
    }

    private void Update()
    {
        bool changed = false;
        if (maxXSlider != null)
        {
            var v = maxXSlider.value.Value;
            if (!Mathf.Approximately(v, _maxX)) { _maxX = v; changed = true; }
        }

        if (minXSlider != null)
        {
            var v  = minXSlider.value.Value;
            if (!Mathf.Approximately(v, _minX)) { _minX = v; changed = true; }
        }

        if (maxYSlider != null)
        {
            var v  = maxYSlider.value.Value;
            if (!Mathf.Approximately(v, _maxY)) { _maxY = v; changed = true; }
        }

        if (changed)
        {
            ApplyXYIfChanged();
        }
    }


    /// <summary>
    /// �C���X�y�N�^���ݒ�Ȃ璼���̎q����Renderer���������W�B
    /// MPB�z����^�[�Q�b�g���ɍ��킹�Ċm�ہB
    /// </summary>
    void EnsureTargetsAndBlocks()
    {
        if (targets == null) targets = new List<Renderer>();

        // ���ݒ�Ȃ�e�����̎q����E���i�K�v�Ȃ�蓮�ݒ�ɐ؂�ւ��j
        if (targets.Count == 0)
        {
            targets.Clear();
            foreach (Transform child in transform)
            {
                var r = child.GetComponent<Renderer>();
                if (r != null) targets.Add(r);
            }
        }

        // MPB���^�[�Q�b�g���ɍ��킹�Ċm��
        if (mpbs == null || mpbs.Length != targets.Count)
        {
            mpbs = new MaterialPropertyBlock[targets.Count];
            for (int i = 0; i < targets.Count; i++)
                mpbs[i] = new MaterialPropertyBlock();
        }
    }
    

    /// <summary>
    /// ���ׂĂ�target�̃V�F�[�_�[��Range�𔽉f
    /// </summary>
    // private void Apply()
    // {
    //     ApplyXY();
    //    // ApplyZ();
    // }

    // �O��������S�ɃZ�b�g�������ꍇ�̃w���p�[
    // �����g�������Ȃ瑼��minmax���p�ӂ���
    public void SetMinX(float minX)
    {
        _minX = Mathf.Clamp01(minX);
        ApplyXYIfChanged();
    }



    private void ApplyXYIfChanged()
    {
        if (Mathf.Approximately(prevMinX, _minX) &&
            Mathf.Approximately(prevMaxX, _maxX) &&
            Mathf.Approximately(prevMinY, _minY) &&
            Mathf.Approximately(prevMaxY, _maxY))
        {
            return;
        }

        ForceApplyXY();
        prevMinX = _minX; prevMaxX = _maxX; prevMinY = _minY; prevMaxY = _maxY;
    }

    private void ForceApplyXY()
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

            var block = mpbs[i];
            // r.GetPropertyBlock(block); // 毎回取得は不要。キャッシュを直接設定。
            block.SetFloat(shaderPropertyName_MinX, minX);
            block.SetFloat(shaderPropertyName_MaxX, maxX);
            block.SetFloat(shaderPropertyName_MinY, minY);
            block.SetFloat(shaderPropertyName_MaxY, maxY);

            r.SetPropertyBlock(block);
        }
    }

/*
    /// <summary>
    /// XY�����̃X���C�_�[�̒l���V�F�[�_�[�ɓK�p����
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

            // �����̃u���b�N���擾 �� �l��ݒ� �� ���f
            var block = mpbs[i];
            r.GetPropertyBlock(block);
            block.SetFloat(shaderPropertyName_MinX, minX);
            block.SetFloat(shaderPropertyName_MaxX, maxX);
            block.SetFloat(shaderPropertyName_MinY, minY);
            block.SetFloat(shaderPropertyName_MaxY, maxY);

            // 1�����_���[�ɕ����}�e���A��������ꍇ�ł�
            // MaterialPropertyBlock�̓����_���[�P�ʂœK�p�\
            r.SetPropertyBlock(block);
        }
    }

    /// <summary>
    /// �y�����̃X���C�_�[�̒l���V�F�[�_�[�ɓK�p����
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

        //// ���E��T��
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


        // 0-1�̒l�����v�l�̊Y�������Ԃ����߂�B
        // ��Ԃɉ����Ċetarget��Z���Z�b�g����iminZ�AmaxZ��6����`����΂悢�͂��B
        // �S�g�o���̂��X�y�b�N�I�ɋꂵ���Ȃ�Ȃ�1��1�ł��������ȁB
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
*/
}
