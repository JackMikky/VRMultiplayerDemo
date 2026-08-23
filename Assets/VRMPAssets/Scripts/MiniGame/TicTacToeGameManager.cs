using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XRMultiplayer
{
    public enum GameState
    {
        Initialize,
        Win,
        Draw,
        End
    }

    public class TicTacToeGameManager : MonoBehaviour
    {
        public static TicTacToeGameManager Instance;

        [SerializeField]
        private TicTacToePanel[] panels;

        private CellState[] board = new CellState[9];

        private bool playerTurn = true;
        private bool gameOver = false;

        [Header("Game UI")]
        [SerializeField] private GameObject frameObject;

        [SerializeField] private GameObject[] gameUIObjects;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip defeatClip;
        [SerializeField] private AudioClip drawClip;

        public CustomEvent<GameState> onGameEnd;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            panels = panels
                    .OrderBy(panel => panel.CellIndex)
                    .ToArray();

            for (int i = 0; i < board.Length; i++)
            {
                board[i] = CellState.Empty;
            }
            this.SetGameEndUI(GameState.Initialize, false);
        }

        public void SelectCell(int index)
        {
            if (gameOver)
                return;

            if (!playerTurn)
                return;

            if (board[index] != CellState.Empty)
                return;

            PlaceMark(index, CellState.Player);

            if (CheckWinner(CellState.Player))
            {
                Debug.Log("Player Win!");
                gameOver = true;
                this.OnGameEnd(GameState.Win, true);
                StartCoroutine(ResetGameAfterDelay());
                return;
            }

            if (IsBoardFull())
            {
                Debug.Log("Draw");
                gameOver = true;
                this.OnGameEnd(GameState.Draw, false);
                StartCoroutine(ResetGameAfterDelay());
                return;
            }

            playerTurn = false;

            StartCoroutine(CPUTurn());
        }

        private IEnumerator CPUTurn()
        {
            yield return new WaitForSeconds(1f);

            int moveIndex = GetCpuMove();

            PlaceMark(moveIndex, CellState.CPU);

            if (CheckWinner(CellState.CPU))
            {
                Debug.Log("CPU Win!");
                gameOver = true;
                this.OnGameEnd(GameState.Win, false);
                StartCoroutine(ResetGameAfterDelay());
                yield break;
            }

            if (IsBoardFull())
            {
                Debug.Log("Draw");
                gameOver = true;
                this.OnGameEnd(GameState.Draw, false);
                StartCoroutine(ResetGameAfterDelay());
                yield break;
            }

            playerTurn = true;
        }

        private int GetCpuMove()
        {
            if (Random.value <= 0.9f)
            {
                int blockIndex = FindBlockingMove();

                if (blockIndex != -1)
                {
                    return blockIndex;
                }
            }

            return GetRandomMove();
        }

        private int FindBlockingMove()
        {
            int[,] wins =
            {
        {0,1,2},
        {3,4,5},
        {6,7,8},

        {0,3,6},
        {1,4,7},
        {2,5,8},

        {0,4,8},
        {2,4,6}
    };

            for (int i = 0; i < 8; i++)
            {
                int a = wins[i, 0];
                int b = wins[i, 1];
                int c = wins[i, 2];

                int playerCount = 0;
                int emptyIndex = -1;

                if (board[a] == CellState.Player)
                    playerCount++;
                else if (board[a] == CellState.Empty)
                    emptyIndex = a;

                if (board[b] == CellState.Player)
                    playerCount++;
                else if (board[b] == CellState.Empty)
                    emptyIndex = b;

                if (board[c] == CellState.Player)
                    playerCount++;
                else if (board[c] == CellState.Empty)
                    emptyIndex = c;

                if (playerCount == 2 && emptyIndex != -1)
                {
                    return emptyIndex;
                }
            }

            return -1;
        }

        private int GetRandomMove()
        {
            List<int> emptyCells = new();

            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == CellState.Empty)
                {
                    emptyCells.Add(i);
                }
            }

            return emptyCells[Random.Range(0, emptyCells.Count)];
        }

        private void PlaceMark(int index, CellState state)
        {
            board[index] = state;
            panels[index].SetState(state);
        }

        private bool IsBoardFull()
        {
            foreach (var cell in board)
            {
                if (cell == CellState.Empty)
                    return false;
            }

            return true;
        }

        private bool CheckWinner(CellState state)
        {
            int[,] wins =
            {
                {0,1,2},
                {3,4,5},
                {6,7,8},

                {0,3,6},
                {1,4,7},
                {2,5,8},

                {0,4,8},
                {2,4,6}
            };

            for (int i = 0; i < 8; i++)
            {
                if (board[wins[i, 0]] == state &&
                    board[wins[i, 1]] == state &&
                    board[wins[i, 2]] == state)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator ResetGameAfterDelay()
        {
            yield return new WaitForSeconds(4f);

            ResetGame();
        }

        private void OnGameEnd(GameState state, bool isPlayerWin)
        {
            this.SetGameEndUI(state, isPlayerWin);
            animator.Play("GameEndUI", 0, 0f);
            onGameEnd.Invoke(state);
        }

        private void SetGameEndUI(GameState state, bool isPlayerWin)
        {
            switch (state)
            {
                case GameState.Initialize:
                    this.frameObject.SetActive(false);
                    foreach (var item in this.gameUIObjects)
                    {
                        item.SetActive(false);
                    }
                    break;

                case GameState.Win:

                    this.frameObject.SetActive(true);

                    if (isPlayerWin)
                    {
                        this.gameUIObjects[0].SetActive(true);
                        this.gameUIObjects[1].SetActive(false);
                        this.gameUIObjects[2].SetActive(false);
                        if (!audioSource.isPlaying) audioSource.PlayOneShot(winClip);
                    }
                    else
                    {
                        this.gameUIObjects[0].SetActive(false);
                        this.gameUIObjects[1].SetActive(true);
                        this.gameUIObjects[2].SetActive(false);
                        if (!audioSource.isPlaying) audioSource.PlayOneShot(defeatClip);
                    }

                    break;

                case GameState.Draw:
                    this.frameObject.SetActive(true);
                    this.gameUIObjects[0].SetActive(false);
                    this.gameUIObjects[1].SetActive(false);
                    this.gameUIObjects[2].SetActive(true);
                    if (!audioSource.isPlaying) audioSource.PlayOneShot(drawClip);
                    break;

                case GameState.End:
                    break;

                default:
                    break;
            }
        }

        private void ResetGame()
        {
            gameOver = false;
            playerTurn = true;

            for (int i = 0; i < board.Length; i++)
            {
                board[i] = CellState.Empty;

                panels[i].ResetCell();
            }
            this.SetGameEndUI(GameState.Initialize, false);
            Debug.Log("Game Reset");
        }
    }
}