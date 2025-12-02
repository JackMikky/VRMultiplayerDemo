using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace XRMultiplayer
{
    [RequireComponent(typeof(NetworkPhysicsInteractable))]
    public class SimplePen : NetworkBehaviour
    {
        [SerializeField] private PenTrail m_TrailRendererPrefab;
        [SerializeField] private Transform m_PenTipTransform;
        [SerializeField] private Renderer m_PenTipRenderer;

        private NetworkPhysicsInteractable m_NetworkInteractable;

        private PenTrail m_CurrentTrailRenderer;

        private NetworkVariable<Color> m_CurrentColor = new NetworkVariable<Color>(Color.red);

        private NetworkVariable<float> m_LineWidth = new NetworkVariable<float>(0.001f);

        [SerializeField] private Slider lineWidthSlider;

        public Color CurrentColor
        {
            set => m_CurrentColor.Value = value;
        }

        private List<PenTrail> m_PenTrails = new();

        private Renderer penRenderer;

        private void Awake()
        {
            TryGetComponent(out m_NetworkInteractable);
            SetColor(m_CurrentColor.Value);
            m_CurrentColor.OnValueChanged += (previousValue, newValue) => { SetColor(newValue); };
        }

        private void Start()
        {
            lineWidthSlider.onValueChanged.AddListener(SetLineWidth);
            XRINetworkGameManager.Connected.Subscribe(ConnectedToNetworkGame);
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(ConnectedToNetworkGame);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SetColor(m_CurrentColor.Value);
        }

        private void ConnectedToNetworkGame(bool connected)
        {
            if (!connected)
            {
                foreach (var trail in m_PenTrails)
                {
                    Destroy(trail.gameObject);
                }
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
                m_CurrentTrailRenderer = null;
            }
        }

        public void SetColor(Color color)
        {
            if (!IsServer) return;
            this.m_CurrentColor.Value = color;
            if (m_PenTipRenderer != null)
            {
                m_PenTipRenderer.material.color = color;
            }
        }

        public void SetLineWidth(float width)
        {
            if (!IsServer) return;
            m_LineWidth.Value = width;
            if (m_CurrentTrailRenderer != null)
            {
                m_CurrentTrailRenderer.SetLineWidth(width);
            }
        }
    }
}