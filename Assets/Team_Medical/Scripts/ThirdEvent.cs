using System.Diagnostics;
using UnityEngine;

public class ThirdEvent : MonoBehaviour
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

    // トリガー判定（Is Trigger を ON にしたコライダー用）
    void OnTriggerEnter(Collider other)
    {
        if (allowedPartner == null) return;
        if (other.gameObject == allowedPartner)
        {
            // 許可ペアのみ処理
            DoAction(other.gameObject);
        }
    }

    // 衝突判定（Rigidbody + Collider の組み合わせ用）
    void OnCollisionEnter(Collision collision)
    {
        if (allowedPartner == null) return;
        if (collision.gameObject == allowedPartner)
        {
            DoAction(collision.gameObject);
        }
    }

    void DoAction(GameObject partner)
    {
        // ここに効果音・エフェクト・スコアなどの処理
        Play();
        gameObject.SetActive(false);
        //organs.SetActive(true);
        Vector3 positiony = organs.transform.position;
        positiony.y += 2;
        organs.transform.position = positiony;

    }


    void Play()
    {
        if (sfx != null) _src.PlayOneShot(sfx, volume);
    }
}
