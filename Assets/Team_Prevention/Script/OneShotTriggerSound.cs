using System.Collections.Generic;
using UnityEngine;

public class OneShotTriggerSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource; // 再生に使う AudioSource
    [SerializeField] private AudioClip clip;          // 再生する音

    // 今接触中の Collider を記録
    private readonly HashSet<Collider> currentlyOverlapping = new HashSet<Collider>();
    // 接触済みで音を鳴らした Collider を記録（同一相手で何度も鳴らしたくない場合）
    private readonly HashSet<Collider> alreadyAnnounced = new HashSet<Collider>();

    private void Start()
    {
        var myCollider = GetComponent<Collider>();
        if (myCollider == null)
        {
            Debug.LogWarning("[OneShotTriggerSound] Collider is required on the GameObject.");
            return;
        }
        if (!myCollider.isTrigger)
        {
            Debug.LogWarning("[OneShotTriggerSound] Set Collider.isTrigger = true for trigger events.");
        }

        // すでに重なっているコライダーを初期登録（この時点では音は鳴らさない）
        Collider[] overlaps = Physics.OverlapBox(
            myCollider.bounds.center,
            myCollider.bounds.extents,
            Quaternion.identity,
            ~0 // 全レイヤー対象
        );

        foreach (var c in overlaps)
        {
            if (c == myCollider) continue;
            currentlyOverlapping.Add(c);
            alreadyAnnounced.Add(c); // 初期重なりは鳴らさない
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // レイヤー制限なし。何かしら当たったら検知
        currentlyOverlapping.Add(other);

        // まだこの相手に対して音を鳴らしていなければ再生
        if (!alreadyAnnounced.Contains(other))
        {
            PlayOnce();
            alreadyAnnounced.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        currentlyOverlapping.Remove(other);

        // 再入場でも鳴らしたくない場合は何もしない
        // 再入場で再生したいなら、以下の行を有効化
        // alreadyAnnounced.Remove(other);
    }

    private void PlayOnce()
    {
        if (audioSource == null || clip == null) return;

        audioSource.Stop();      // 多重再生防止
        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.Play();
    }
}