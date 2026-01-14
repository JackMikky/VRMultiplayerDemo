using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer;
using XRMultiplayer.MiniGames;

public class ShootingGameManager : NetworkBehaviour
{
    [System.Serializable]
    public struct PlayerStats : INetworkSerializable
    {
        public ulong playerId;
        public string name;
        public int score;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerId);
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref score);
        }
    }

    [Header("Player Stats")]
    [SerializeField] private PlayerStats[] players;

    [SerializeField] private PlayerStats localPlayer;

    [Header("Game Settings")]
    [Tooltip("Seconds")]
    [SerializeField] private int maxTime = 90;

    [SerializeField] private float time;

    [SerializeField] private ItemSpawner spawner;

    [SerializeField] private List<NetworkProjectileLauncher> launchers;

    [Header("UI")]
    [Header("Board UI")]
    [SerializeField] private CanvasGroup boardUIGroup;

    [SerializeField] private GameObject subUI;

    [SerializeField] private TMP_Text topPlayerNameUI;

    [SerializeField] private TMP_Text topScoreUI;

    [Header("In-Game UI")]
    [SerializeField] private CanvasGroup gameUIGroup;

    [SerializeField] private TMP_Text scoreUI;

    [SerializeField] private TMP_Text timeUI;

    [Header("Audios")]
    [SerializeField] private EventAudioSource eventAudio;

    [SerializeField] private BGMController bgmController;

    [SerializeField] private AudioClip gameOverAudioClip;
    [SerializeField] private AudioClip gameStartAudioClip;

    [Header("Game State")]
    public bool isStarted = false;

    public GameObject displayObject;

    private void Start()
    {
        timeUI.text = maxTime.ToString();
        scoreUI.text = "0";
        gameUIGroup.alpha = 0;
        time = maxTime;
        foreach (var launcher in launchers)
        {
            launcher.HitTargetAction = this.SetScore;
        }

        localPlayer = new PlayerStats { name = XRINetworkGameManager.LocalPlayerName.Value, score = 0, playerId = NetworkManager.Singleton.LocalClientId };
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        if (!isStarted) return;
        time -= Time.fixedDeltaTime;
        timeUI.text = Mathf.CeilToInt(time).ToString();
        UpdateGameTimeToClient(time);
        if (time <= 0)
        {
            isStarted = false;
            GameOverRpc();
        }
    }

    public void UpdateGameTimeToClient(float newTime)
    {
        UpdateGameTimeToClientRpc(newTime);
    }

    [ClientRpc]
    public void UpdateGameTimeToClientRpc(float newTime)
    {
        time = newTime;
        timeUI.text = Mathf.CeilToInt(time).ToString();
    }

    [Rpc(SendTo.Everyone)]
    public void GameReadyRpc()
    {
        if (!IsServer) return;
        gameUIGroup.alpha = 1;
        boardUIGroup.alpha = 0;
        subUI.SetActive(false);
        time = maxTime;
        localPlayer.score = 0;

        this.eventAudio.PlayOneShot(gameStartAudioClip, () =>
        {
            isStarted = true;
            spawner.gameObject.SetActive(true);
            spawner.readyForSpawn = true;

            if (bgmController != null)
            {
                bgmController.PlayWithFadeIn();
            }
        });
    }

    [Rpc(SendTo.Everyone)]
    public void GameOverRpc()
    {
        spawner.readyForSpawn = false;

        if (bgmController != null)
        {
            bgmController.StopWithFadeOut();
        }

        this.eventAudio.PlayOneShot(gameOverAudioClip, () =>
        {
            gameUIGroup.alpha = 0;
            boardUIGroup.alpha = 1;
            SendPlayerDataToServerRpc(localPlayer);
            spawner.ClearAllSpawnedInstances();
            spawner.gameObject.SetActive(false);
            displayObject.SetActive(true);
            subUI.SetActive(true);
        });
    }

    [Rpc(SendTo.Server)]
    public void SendPlayerDataToServerRpc(PlayerStats playerData)
    {
        bool playerExists = false;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].playerId == playerData.playerId)
            {
                players[i] = playerData;
                playerExists = true;
                break;
            }
        }

        if (!playerExists)
        {
            System.Array.Resize(ref players, players.Length + 1);
            players[players.Length - 1] = playerData;
        }

        PlayerStats topPlayer = players[0];
        foreach (var player in players)
        {
            if (player.score > topPlayer.score)
            {
                topPlayer = player;
            }
        }

        UpdateTopPlayerRpc(topPlayer);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateTopPlayerRpc(PlayerStats topPlayer)
    {
        topPlayerNameUI.text = topPlayer.name;
        topScoreUI.text = topPlayer.score.ToString();
    }

    private void SetScore(int score, bool isLocalPlayer)
    {
        if (isLocalPlayer)
        {
            this.localPlayer.score += score;
            scoreUI.text = this.localPlayer.score.ToString();
        }
    }
}