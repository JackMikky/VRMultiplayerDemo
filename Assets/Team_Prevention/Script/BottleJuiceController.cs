using UnityEngine;

// Controller variant for Approach A: no explicit liquidSurface needed.
[DisallowMultipleComponent]
public class BottleJuiceController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;                   // ボトル剛体
    public Transform mouth;                // ボトル口（発射基準）
    public ParticleSystem splashParticles; // スプラッシュ粒子
    public Renderer liquidRenderer;        // 液体一体メッシュのRenderer（このマテリアルに _FillHeight を渡す）
    public GameObject wetDecalPrefab;      // 濡れ跡Prefab（任意）

    [Header("Amount")]
    [Range(0, 1)] public float juiceAmount = 1f; // 0..1 残量
    public float juiceLossPerSplash = 0.02f;

    [Header("Bottle Inner (Local Y)")]
    public float innerBottomY = 0.00f;          // ボトル内底（ローカル）
    public float innerTopY = 0.20f;             // ボトル内上端（ローカル）

    [Header("Splash Conditions")]
    public float minAngularVelocity = 2.2f;     // 振りのしきい値
    [Range(0, 1)] public float mouthDownDot = 0.5f; // mouth.up と世界Downの近さ
    public float cooldown = 0.23f;
    public int particlesPerBurst = 25;

    [Header("Decal Spawn (optional)")]
    public LayerMask decalHitMask;
    public float decalRayDistance = 3.0f;
    public int decalRaysPerSplash = 6;
    public Vector2 decalRandomRadius = new Vector2(0.02f, 0.12f);

    private float timer;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb == null) return;
        timer += Time.deltaTime;

        UpdateMaterialParams();
        TryEmitSplash();
    }

    void UpdateMaterialParams()
    {
        if (liquidRenderer == null || liquidRenderer.sharedMaterial == null) return;

        // FillHeight をワールド座標で算出
        float fillHeight = Mathf.Lerp(
            transform.TransformPoint(new Vector3(0, innerBottomY, 0)).y,
            transform.TransformPoint(new Vector3(0, innerTopY, 0)).y,
            Mathf.Clamp01(juiceAmount)
        );

        var mat = liquidRenderer.sharedMaterial;
        mat.SetFloat("_FillHeight", fillHeight);
        mat.SetVector("_GravityUp", -Physics.gravity.normalized);
    }

    void TryEmitSplash()
    {
        if (splashParticles == null || juiceAmount <= 0f) return;
        if (timer < cooldown) return;

        float angular = rb.angularVelocity.magnitude;
        bool isMouthDown = mouth && Vector3.Dot(mouth.up, Vector3.down) > mouthDownDot;

        if (angular > minAngularVelocity && isMouthDown)
        {
            splashParticles.Emit(particlesPerBurst);
            SpawnDecals();

            juiceAmount = Mathf.Clamp01(juiceAmount - juiceLossPerSplash);
            timer = 0f;
        }
    }

    void SpawnDecals()
    {
        if (wetDecalPrefab == null || mouth == null) return;

        for (int i = 0; i < decalRaysPerSplash; i++)
        {
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(decalRandomRadius.x, decalRandomRadius.y);
            Vector3 start = mouth.position + mouth.right * r.x + mouth.forward * r.y;
            Vector3 dir = (Vector3.down + new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.08f, 0.08f))).normalized;

            if (Physics.Raycast(start, dir, out RaycastHit hit, decalRayDistance, decalHitMask, QueryTriggerInteraction.Ignore))
            {
                Quaternion rot = Quaternion.LookRotation(hit.normal, Vector3.forward) * Quaternion.Euler(90, 0, 0);
                Vector3 pos = hit.point + hit.normal * 0.001f;
                var go = Instantiate(wetDecalPrefab, pos, rot);
                float s = Random.Range(0.07f, 0.14f);
                go.transform.localScale = new Vector3(s, s, s);
            }
        }
    }
}
