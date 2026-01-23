using System.Collections;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Controller for managing background music playback with fade in/out effects.
    /// </summary>
    public class BGMController : MonoBehaviour
    {
        [Header("Audio Source")]
        [SerializeField] private AudioSource bgmAudioSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] bgmAudioClips;

        [Header("Audio Settings")]
        [SerializeField] private float fadeDuration = 1.5f;

        [SerializeField] private float maxVolume = 0.5f;

        private Coroutine fadeCoroutine;

        private void Awake()
        {
            if (bgmAudioSource == null)
            {
                bgmAudioSource = GetComponent<AudioSource>();
            }

            if (bgmAudioSource != null)
            {
                bgmAudioSource.volume = 0f;
                bgmAudioSource.loop = true;
            }
        }

        /// <summary>
        /// Plays a random BGM clip with fade in effect.
        /// </summary>
        /// <param name="customFadeDuration">Optional custom fade in duration.</param>
        public void PlayWithFadeIn(float? customFadeDuration = null)
        {
            AudioClip clip = bgmAudioClips.Length > 0 ? bgmAudioClips[Random.Range(0, bgmAudioClips.Length)] : null;
            if (bgmAudioSource == null || clip == null) return;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            bgmAudioSource.clip = clip;
            bgmAudioSource.Play();
            fadeCoroutine = StartCoroutine(FadeVolume(0f, maxVolume, customFadeDuration ?? fadeDuration));
        }

        /// <summary>
        /// Stops the BGM with fade out effect.
        /// </summary>
        /// <param name="customFadeDuration">Optional custom fade out duration.</param>
        /// <param name="onComplete">Callback invoked when fade out completes.</param>
        public void StopWithFadeOut(float? customFadeDuration = null, System.Action onComplete = null)
        {
            if (bgmAudioSource == null) return;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeVolume(bgmAudioSource.volume, 0f, customFadeDuration ?? fadeDuration, () =>
            {
                bgmAudioSource.Stop();
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Immediately stops the BGM without fade out.
        /// </summary>
        public void StopImmediate()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            if (bgmAudioSource != null)
            {
                bgmAudioSource.Stop();
                bgmAudioSource.volume = 0f;
            }
        }

        /// <summary>
        /// Sets the maximum volume for BGM playback.
        /// </summary>
        /// <param name="volume">Volume value clamped between 0 and 1.</param>
        public void SetMaxVolume(float volume)
        {
            maxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Coroutine that fades the audio volume over a specified duration.
        /// </summary>
        /// <param name="startVolume">Starting volume.</param>
        /// <param name="targetVolume">Target volume.</param>
        /// <param name="duration">Fade duration in seconds.</param>
        /// <param name="onComplete">Optional callback invoked when fade completes.</param>
        private IEnumerator FadeVolume(float startVolume, float targetVolume, float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (bgmAudioSource != null)
                {
                    bgmAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                }
                yield return null;
            }

            if (bgmAudioSource != null)
            {
                bgmAudioSource.volume = targetVolume;
            }

            onComplete?.Invoke();
        }
    }
}