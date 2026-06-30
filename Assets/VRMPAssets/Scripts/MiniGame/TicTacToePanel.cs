using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    public enum CellState
    {
        Empty,
        Player,
        CPU
    }

    public class TicTacToePanel : MonoBehaviour

    {
        [SerializeField] private GameObject playerObject;
        [SerializeField] private Color playerColor;

        [Space(10)]
        [SerializeField] private GameObject cpuObject;

        [SerializeField] private Color cpuColor;

        public int CellIndex;

        private CellState currentState = CellState.Empty;

        private Renderer playerRenderer;
        private Renderer cpuRenderer;

        public CellState CurrentState => currentState;

        private void Start()
        {
            this.name = gameObject.name + this.CellIndex;

            playerRenderer = playerObject.GetComponent<Renderer>();
            cpuRenderer = cpuObject.GetComponent<Renderer>();

            this.playerObject.SetActive(false);
            this.cpuObject.SetActive(false);
        }

        public void OnClicked()
        {
            TicTacToeGameManager.Instance.SelectCell(CellIndex - 1);
        }

        public void SetState(CellState state)
        {
            currentState = state;

            switch (state)
            {
                case CellState.Player:

                    playerObject.SetActive(true);
                    cpuObject.SetActive(false);

                    if (playerRenderer != null)
                    {
                        playerRenderer.material.color = playerColor;
                    }

                    Debug.Log($"Cell {CellIndex} = X");
                    break;

                case CellState.CPU:

                    playerObject.SetActive(false);
                    cpuObject.SetActive(true);

                    if (cpuRenderer != null)
                    {
                        cpuRenderer.material.color = cpuColor;
                    }

                    Debug.Log($"Cell {CellIndex} = O");
                    break;
            }
        }

        public void ResetCell()
        {
            currentState = CellState.Empty;
            this.playerObject.SetActive(false);
            this.cpuObject.SetActive(false);
        }
    }
}