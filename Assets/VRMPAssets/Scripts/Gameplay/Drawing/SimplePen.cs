using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    [RequireComponent(typeof(NetworkPhysicsInteractable))]
    public class SimplePen : NetworkBehaviour
    {
        [SerializeField] private PenTrail m_TrailRendererPrefab;
        [SerializeField] private Transform m_PenTipTransform;
        [SerializeField] private Renderer m_PenTipRenderer;
        [SerializeField] private PenTip penTip;

        private PenTrail m_CurrentTrailRenderer;

        private NetworkVariable<Color> m_CurrentColor = new NetworkVariable<Color>(
            Color.red,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<float> m_LineWidth = new NetworkVariable<float>(
            0.001f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        [SerializeField] private Slider lineWidthSlider;

        private List<PenTrail> m_PenTrails = new List<PenTrail>();

        private void Awake()
        {
            m_CurrentColor.OnValueChanged += OnColorChanged;
            m_LineWidth.OnValueChanged += OnLineWidthChanged;
        }

        private void Start()
        {
            if (lineWidthSlider != null)
            {
                lineWidthSlider.onValueChanged.AddListener(RequestSetLineWidth);
            }
            XRINetworkGameManager.Connected.Subscribe(ConnectedToNetworkGame);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            ApplyColor(m_CurrentColor.Value);
            ApplyLineWidth(m_LineWidth.Value);
        }

        private void OnDestroy()
        {
            m_CurrentColor.OnValueChanged -= OnColorChanged;
            m_LineWidth.OnValueChanged -= OnLineWidthChanged;

            if (lineWidthSlider != null)
            {
                lineWidthSlider.onValueChanged.RemoveListener(RequestSetLineWidth);
            }

            XRINetworkGameManager.Connected.Unsubscribe(ConnectedToNetworkGame);
        }

        private void OnColorChanged(Color previousValue, Color newValue)
        {
            ApplyColor(newValue);
        }

        private void OnLineWidthChanged(float previousValue, float newValue)
        {
            ApplyLineWidth(newValue);
        }

        private void ApplyColor(Color color)
        {
            if (m_PenTipRenderer != null)
            {
                m_PenTipRenderer.material.color = color;
            }

            if (m_CurrentTrailRenderer != null)
            {
                m_CurrentTrailRenderer.SetColor(color);
            }
        }

        private void ApplyLineWidth(float width)
        {
            if (m_CurrentTrailRenderer != null)
            {
                m_CurrentTrailRenderer.SetLineWidth(width);
            }
        }

        private void ConnectedToNetworkGame(bool connected)
        {
            if (!connected)
            {
                foreach (var trail in m_PenTrails)
                {
                    if (trail != null)
                    {
                        Destroy(trail.gameObject);
                    }
                }
                m_PenTrails.Clear();
            }
        }

        public void ToggleDrawing(bool toggle)
        {
            if (toggle && m_CurrentTrailRenderer == null)
            {
                m_CurrentTrailRenderer = Instantiate(m_TrailRendererPrefab, m_PenTipTransform.position, m_PenTipTransform.rotation, m_PenTipTransform);
                m_CurrentTrailRenderer.SetColor(m_CurrentColor.Value);
                m_CurrentTrailRenderer.SetLineWidth(m_LineWidth.Value);
            }
            else if (!toggle && m_CurrentTrailRenderer != null)
            {
                m_CurrentTrailRenderer.transform.SetParent(null);
                m_CurrentTrailRenderer.CreateInteractableTrail();

                m_PenTrails.Add(m_CurrentTrailRenderer);

                m_CurrentTrailRenderer = null;
            }
        }

        public void RequestSetColor(Color color)
        {
            if (IsServer)
            {
                m_CurrentColor.Value = color;
            }
            else
            {
                RequestSetColorServerRpc(color);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestSetColorServerRpc(Color color)
        {
            m_CurrentColor.Value = color;
        }

        public void RequestSetLineWidth(float width)
        {
            if (IsServer)
            {
                m_LineWidth.Value = width;
            }
            else
            {
                RequestSetLineWidthServerRpc(width);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestSetLineWidthServerRpc(float width)
        {
            m_LineWidth.Value = width;
        }
    }
}