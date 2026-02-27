using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WetDecalFader : MonoBehaviour
{
    public float lifeTime = 1.2f;        // 生存時間（秒）
    public float fadeOutTime = 0.8f;     // フェード時間（秒）
    public string colorProperty = "_Color";

    private Renderer _renderer;
    private Material _matInstance;
    private Color _baseColor;
    private float _timer;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        // マテリアルインスタンス化（他と共有しない）
        _matInstance = _renderer.material;
        _baseColor = _matInstance.HasProperty(colorProperty) ? _matInstance.GetColor(colorProperty) : Color.white;
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer > lifeTime)
        {
            float t = Mathf.InverseLerp(0f, fadeOutTime, _timer - lifeTime);
            float a = Mathf.Lerp(_baseColor.a, 0f, t);
            Color c = _baseColor; c.a = a;
            _matInstance.SetColor(colorProperty, c);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        if (Application.isPlaying && _matInstance != null)
        {
            Destroy(_matInstance);
        }
    }
}
