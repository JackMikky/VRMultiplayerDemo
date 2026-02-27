using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkScoreManager : NetworkBehaviour
{
    [Serializable]
    public struct PlayerScore : INetworkSerializable
    {
        public ulong clientId;
        public string playerName;
        public float score;
        public string usedInfo;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref score);
            serializer.SerializeValue(ref usedInfo);
        }
    }

    // シングルトン的アクセス
    public static NetworkScoreManager Instance { get; private set; }


    // サーバーが管理するスコア一覧
    private readonly List<PlayerScore> _scores = new List<PlayerScore>();

    // クライアント表示用のスコア一覧（ClientRpcで更新される）
    private readonly List<PlayerScore> _clientScores = new List<PlayerScore>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {

        base.OnNetworkSpawn();
        Debug.Log($"[NetworkScoreManager] OnNetworkSpawn. IsServer={IsServer}, IsClient={IsClient}");
    }




    public IReadOnlyList<PlayerScore> GetAllScores()
    {
        // クライアント側 → _clientScores を表示
        // サーバー側 → _scores を表示（どちらでもOKだが一応分ける）
        if (IsServer)
            return _scores.AsReadOnly();
        else
            return _clientScores.AsReadOnly();
    }



    // ====== クライアントから呼ぶ入口 ======

    /// <summary>
    /// ローカルプレイヤーのスコア報告ヘルパー
    /// </summary>
    public void ReportLocalScore(float score, string usedInfo = null, string playerName = null)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[NetworkScoreManager] NetworkObject がまだ Spawn されていません。");
            return;
        }

        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("[NetworkScoreManager] Network が有効ではありません。");
            return;
        }

        ulong clientId = NetworkManager.Singleton.LocalClientId;

        if (string.IsNullOrEmpty(playerName))
        {
            try
            {
                playerName = XRMultiplayer.XRINetworkGameManager.LocalPlayerName.Value;
            }
            catch
            {
                playerName = $"Player {clientId}";
            }
        }

        var ps = new PlayerScore
        {
            clientId = clientId,
            playerName = playerName,
            score = score,
            usedInfo = usedInfo ?? ""    // ★ ここでセット
        };

        SendScoreToServerRpc(ps);
    }

    // ====== RPC 部分 ======

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SendScoreToServerRpc(PlayerScore ps, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ps.clientId = rpcParams.Receive.SenderClientId;

        bool found = false;
        for (int i = 0; i < _scores.Count; i++)
        {
            if (_scores[i].clientId == ps.clientId)
            {
                ps.playerName = MakeUniqueName(ps.playerName, ps.clientId);
                _scores[i] = ps;
                found = true;
                break;
            }
        }

        if (!found)
        {
            ps.playerName = MakeUniqueName(ps.playerName, ps.clientId);
            _scores.Add(ps);
        }

        // ★ トッププレイヤー更新
        UpdateTopAndBroadcast();

        // ★ 全員分のスコア一覧もクライアントへ送る
        SyncAllScoresClientRpc(_scores.ToArray());
    }


    // スコア一覧が更新されたときにクライアント側に通知するイベント
    public event Action OnScoresUpdated;

    [Rpc(SendTo.Everyone)]
    private void SyncAllScoresClientRpc(PlayerScore[] allScores)
    {
        _clientScores.Clear();
        if (allScores != null)
        {
            _clientScores.AddRange(allScores);
        }

        // ★ ここで「更新されたよ」を通知
        OnScoresUpdated?.Invoke();
    }

    private void UpdateTopAndBroadcast()
    {
        if (_scores.Count == 0) return;

        PlayerScore top = _scores[0];
        bool hasValid = false;

        foreach (var p in _scores)
        {
            if (p.clientId == 0 && string.IsNullOrEmpty(p.playerName))
                continue;

            if (!hasValid)
            {
                top = p;
                hasValid = true;
            }
            else if (p.score > top.score)
            {
                top = p;
            }
        }
    }

    /// <summary>
    /// 既存のスコア一覧(_scores)を見て、
    /// 同じ clientId 以外で同じ名前があれば
    /// 「Name (2)」「Name (3)…」のようにユニークな名前を返す。
    /// </summary>
    private string MakeUniqueName(string baseName, ulong clientId)
    {
        if (string.IsNullOrEmpty(baseName))
            baseName = $"Player {clientId}";

        // すでに完全一致で同じclientIdのものがある場合 → そのままでOK
        foreach (var s in _scores)
        {
            if (s.clientId == clientId && s.playerName == baseName)
            {
                return baseName;
            }
        }

        // baseName を使っている別クライアントがいるかチェック
        bool sameNameUsedByOthers = false;
        List<int> usedIndices = new List<int>();  // "(n)" の n を集める

        foreach (var s in _scores)
        {
            if (s.clientId == clientId) continue; // 自分は無視

            if (s.playerName == baseName)
            {
                sameNameUsedByOthers = true;
            }
            else if (s.playerName.StartsWith(baseName + " ("))
            {
                // 例: "Taro (2)" → 2 を取り出したい
                string suffixPart = s.playerName.Substring(baseName.Length).Trim(); // " (2)"
                if (suffixPart.StartsWith("(") && suffixPart.EndsWith(")"))
                {
                    string numberStr = suffixPart.Substring(1, suffixPart.Length - 2);
                    if (int.TryParse(numberStr, out int n))
                    {
                        usedIndices.Add(n);
                    }
                }
            }
        }

        // そもそも誰も同じベース名を使っていないなら、そのままでOK
        if (!sameNameUsedByOthers && usedIndices.Count == 0)
        {
            return baseName;
        }

        // すでに使われているインデックスの中で "使われていない最小の数字" を探す
        int index = 2;  // 「(2)」からスタート
        while (usedIndices.Contains(index))
        {
            index++;
        }

        return $"{baseName} ({index})";
    }
}

