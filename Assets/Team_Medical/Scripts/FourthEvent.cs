using System.Diagnostics;
using UnityEngine;

public class FourthEvent : MonoBehaviour
{
    [SerializeField] GameObject allowedPartner;  // 衝突フラグ用
    [SerializeField] GameObject Box;  // 臓器出現用

    [SerializeField] GameObject clearsystem;  // clear

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

    bool _stuck_picker = false;
    bool _stuck = false;

    void OnTriggerEnter(Collider other)
    {
        if (_stuck) return;
        if (!allowedPartner) return;
       

        if (other.gameObject == allowedPartner && _stuck_picker != true)
        {
            Play();

            // 親子付け（完全固定）
            transform.SetParent(allowedPartner.transform, true); // world座標維持で一旦親子化
                                                                 //transform.localPosition = localOffset;
                                                                 //transform.localRotation = Quaternion.Euler(localEuler);

            // 物理を止めて完全固定（任意だが推奨）

            var rb = GetComponent<Rigidbody>();
            if (rb && makeKinematicOnStick)
            {
                // 完全凍結
                rb.isKinematic = true;               // 物理を止める
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                //rb.constraints = RigidbodyConstraints.FreezeAll; // 全軸固定
            }
            _stuck_picker = true;
        }

        if (other.gameObject == Box)
        {
            Play();
            allowedPartner.transform.SetParent(Box.transform, true); // world座標維持で一旦親子化
                                                                     //transform.localPosition = localOffset;
                                                                     //transform.localRotation = Quaternion.Euler(localEuler);
                                                                     // 親子付け（完全固定）
            transform.SetParent(allowedPartner.transform, true); // world座標維持で一旦親子化
                                                                 //transform.localPosition = localOffset;
                                                                 //transform.localRotation = Quaternion.Euler(localEuler);


            // 物理を止めて完全固定（任意だが推奨）
            FreezeRigidbodiesCompletely(gameObject);
            FreezeRigidbodiesCompletely(allowedPartner);
            FreezeRigidbodiesCompletely(Box);

            clearsystem.SetActive(true);

            _stuck = true;
        }
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