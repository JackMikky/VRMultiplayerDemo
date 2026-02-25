using Unity.XR.CoreUtils;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.Android;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents the offline player avatar.
    /// </summary>
    public class OfflinePlayerAvatar : MonoBehaviour
    {
        public static BindableVariable<float> voiceAmp = new BindableVariable<float>();

        /// <summary>
        /// Gets or sets a value indicating whether the player is muted.
        /// </summary>
        public static bool muted
        {
            get => s_Muted;
            set
            {
                if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    s_Muted = value;
            }
        }

        /// <summary>
        /// A value indicating whether the player is muted.
        /// </summary>
        private static bool s_Muted;

        public bool micMuted
        {
            get => s_Muted;
            set
            {
                if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    s_Muted = value;
            }
        }

        /// <summary>
        /// The input volume gain multiplier, range [0, 1]. Controlled by the input volume slider.
        /// </summary>
        private static float s_InputVolumeGain = 25f;

        /// <summary>
        /// The minimum and maximum input volume range used for remapping.
        /// </summary>
        private static readonly Vector2 s_MinMaxInputVolume = new Vector2(-10.0f, 10.0f);

        [SerializeField]
        private GameObject offlineAvatar;

        /// <summary>
        /// The head transform.
        /// </summary>
        [SerializeField] private Transform m_HeadTransform;

        /// <summary>
        /// The head renderer.
        /// </summary>
        [SerializeField] private SkinnedMeshRenderer m_HeadRend;

        /// <summary>
        /// The voice amplitude curve.
        /// </summary>
        [SerializeField] private AnimationCurve m_VoiceCurve;

        /// <summary>
        /// The head origin.
        /// </summary>
        private Transform m_HeadOrigin;

        /// <summary>
        /// The mouth blend smoothing.
        /// </summary>
        [SerializeField] private float m_MouthBlendSmoothing = 5.0f;

        /// <summary>
        /// The microphone loudness.
        /// </summary>
        private float m_MicLoudness;

        /// <summary>
        /// The microphone device name.
        /// </summary>
        private string m_Device;

        /// <summary>
        /// The sample window.
        /// </summary>
        private int m_SampleWindow = 128;

        /// <summary>
        /// The clip record.
        /// </summary>
        private AudioClip m_ClipRecord;

        /// <summary>
        /// The voice destination volume.
        /// </summary>
        private float m_VoiceDestinationVolume;

        private bool m_MicInitialized = false;

        /// <summary>
        /// Sets the input volume for the microphone.
        /// Volume is expected in the range [-10, 10], which is remapped to a gain multiplier [0, 1].
        /// If the volume is at or near the minimum, the microphone will be muted.
        /// </summary>
        /// <param name="volume">The input volume in the range [-10, 10].</param>
        public static void SetInputVolume(float volume)
        {
            volume = Mathf.Clamp(volume, s_MinMaxInputVolume.x, s_MinMaxInputVolume.y);

#if UNITY_EDITOR
            // Remap from [-10, 10] to [0, 1]
            s_InputVolumeGain = Mathf.InverseLerp(s_MinMaxInputVolume.x, s_MinMaxInputVolume.y, volume);
#elif UNITY_ANDROID
            s_InputVolumeGain = volume;
#endif

            if (volume <= (s_MinMaxInputVolume.x + 0.05f))
            {
                s_Muted = true;
            }
            else
            {
                s_Muted = false;
            }
        }

        /// <inheritdoc/>
        private void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            m_HeadOrigin = rig.Camera.transform;
        }

        private void OnEnable()
        {
            XRINetworkGameManager.LocalPlayerColor.Subscribe(UpdatePlayerColor);
            MicrophonePermissionGranted(true);
            XRINetworkGameManager.Connected.Subscribe(connected =>
            {
                this.gameObject.GetComponent<XRAvatarIK>().enabled = !connected;
                offlineAvatar.SetActive(!connected);
            });
        }

        private void OnDisable()
        {
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(UpdatePlayerColor);
            MicrophonePermissionGranted(false);
            StopMicrophone();
            XRINetworkGameManager.Connected.Unsubscribe(connected =>
            {
                this.gameObject.GetComponent<XRAvatarIK>().enabled = !connected;
                offlineAvatar.SetActive(!connected);
            });
        }

        /// <inheritdoc/>
        private void LateUpdate()
        {
            m_HeadTransform.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
        }

        /// <inheritdoc/>
        private void Update()
        {
            if (!s_Muted)
            {
                m_MicLoudness = LevelMax() * s_InputVolumeGain;

                m_VoiceDestinationVolume = Mathf.Clamp01(Mathf.Lerp(m_VoiceDestinationVolume, m_MicLoudness, Time.deltaTime * m_MouthBlendSmoothing));

                float appliedCurve = m_VoiceCurve.Evaluate(m_VoiceDestinationVolume);
                voiceAmp.Value = appliedCurve;
                m_HeadRend.SetBlendShapeWeight(0, 100 - appliedCurve * 100);
            }
            else
            {
                voiceAmp.Value = 0.0f;
            }
        }

        private void MicrophonePermissionGranted(bool granted)
        {
            if (granted)
            {
                InitMic();
            }
        }

        private void UpdatePlayerColor(Color color)
        {
            m_HeadRend.materials[2].color = color;
        }

        /// <summary>
        /// Initializes the microphone, called from <see cref="VoiceChatManager.s_HasMicrophonePermission" callback/>.
        /// </summary>
        private void InitMic()
        {
            if (Microphone.devices.Length == 0) return;
            m_MicInitialized = true;
            m_Device ??= Microphone.devices[0];
            m_ClipRecord = Microphone.Start(m_Device, true, 999, 44100);
        }

        /// <summary>
        /// Stops the microphone.
        /// </summary>
        private void StopMicrophone()
        {
            m_MicInitialized = false;
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Microphone.End(m_Device);
            }
            else
            {
                s_Muted = true;
            }
        }

        /// <summary>
        /// Gets the maximum level of the microphone input.
        /// </summary>
        /// <returns>The maximum level of the microphone input.</returns>
        private float LevelMax()
        {
            if (!m_MicInitialized) return 0;
            float levelMax = 0;
            float[] waveData = new float[m_SampleWindow];
            int micPosition = Microphone.GetPosition(null) - (m_SampleWindow + 1); // null means the first microphone
            if (micPosition < 0) return 0;
            m_ClipRecord.GetData(waveData, micPosition);
            // Getting a peak on the last 128 samples
            for (int i = 0; i < m_SampleWindow; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }
            return levelMax;
        }
    }
}