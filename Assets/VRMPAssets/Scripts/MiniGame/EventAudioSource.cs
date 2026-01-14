using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EventAudioSource : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    public CustomEvent playStartEvent;

    public CustomEvent playEndEvent;

    public void PlayOneShot(AudioClip clip, UnityAction callback = null)
    {
        audioSource.PlayOneShot(clip);
        playStartEvent?.Invoke();

        if (clip != null)
        {
            StartCoroutine(WaitForClipEnd(clip.length, callback));
        }
    }

    private IEnumerator WaitForClipEnd(float clipLength, UnityAction callback = null)
    {
        yield return new WaitForSeconds(clipLength);
        playEndEvent?.Invoke();
        callback?.Invoke();
    }
}