using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("Game UI")]
    [SerializeField] private GameObject gameStartUI;

    [SerializeField] private GameObject gameEndUI;

    [SerializeField] private GameObject gameWinUI;

    [Space(10)]
    [Header("Events")]
    public UnityEvent onGameStart;

    public UnityEvent onGameEnd;

    public UnityEvent onPlayerWin;

    public bool IsGameRunning { get; private set; } = false;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Time.timeScale = 0f; // Pause the game at the start
        if (gameStartUI != null)
        {
            gameStartUI.SetActive(true);
        }
        if (gameEndUI != null)
        {
            gameEndUI.SetActive(false);
        }
    }

    public void StartGame()
    {
        if (gameStartUI != null)
        {
            gameStartUI.SetActive(false);
        }
        onGameStart?.Invoke();
        Time.timeScale = 1f;
        IsGameRunning = true;
    }

    public void EndGame()
    {
        if (!IsGameRunning) return;
        if (gameEndUI != null)
        {
            gameEndUI.SetActive(true);
        }
        onGameEnd?.Invoke();
        Time.timeScale = 0.25f;
        IsGameRunning = false;
    }

    public void PlayerWin()
    {
        if (!IsGameRunning) return;
        if (gameWinUI != null)
        {
            gameWinUI.SetActive(true);
        }
        Time.timeScale = 0.25f;
        IsGameRunning = false;
        onPlayerWin?.Invoke();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}