using System.Diagnostics;
using UnityEngine;

public class FirstEvent : MonoBehaviour
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
   

    void DoAction(GameObject partner)
    {
        // ここに効果音・エフェクト・スコアなどの処理
        Play();
        organs.SetActive(true);

    }


    void Play()
    {
        if (sfx != null) _src.PlayOneShot(sfx, volume);
    }
}
