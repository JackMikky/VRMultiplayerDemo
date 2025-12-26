using System.Diagnostics;
using UnityEngine;

public class SecondEvent : MonoBehaviour
{
    [SerializeField] GameObject allowedPartner;  // 衝突フラグ用
    [SerializeField] GameObject organs;  // 臓器出現用

    public AudioClip sfx;
    [Range(0f, 1f)] public float volume = 1.0f;

    AudioSource _src;

    void Awake()
    {
        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
    }


 
    public bool makeKinematicOnStick = true;
    public Vector3 localOffset = Vector3.zero;
    public Vector3 localEuler = Vector3.zero;

    bool _stuck;

    void OnTriggerEnter(Collider other)
    {
        if (_stuck) return;
        if (!allowedPartner) return;
        if (other.gameObject != allowedPartner) return; // 特定相手のみ

        Play();

        // 親子付け（完全固定）
        transform.SetParent(allowedPartner.transform, true); // world座標維持で一旦親子化
        //transform.localPosition = localOffset;
        //transform.localRotation = Quaternion.Euler(localEuler);

        // 物理を止めて完全固定（任意だが推奨）
        

        FreezeRigidbodiesCompletely(gameObject); 
        FreezeRigidbodiesCompletely(allowedPartner);

        _stuck = true;
    }


    void Play()
    {
        if (sfx != null) _src.PlayOneShot(sfx, volume);
    }

    void FreezeRigidbodiesCompletely(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        // 移動系コンポーネントを止めたい場合はここで無効化（任意）
         DisableMovers(go);
    }

    void DisableMovers(GameObject go)
    {
        foreach (var m in go.GetComponents<MonoBehaviour>())
        {
            if (m == this) continue;
            m.enabled = false;
        }
    }

}
