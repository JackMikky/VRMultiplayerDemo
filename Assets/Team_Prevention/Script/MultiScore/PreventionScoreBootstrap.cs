using Unity.Netcode;
using UnityEngine;

public class PreventionScoreBootstrap : NetworkBehaviour
{
    [SerializeField] private NetworkScoreManager scoreManagerPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // サーバー（Host）だけがスコアマネージャを Spawn
        if (IsServer)
        {
            // 既にどこかに存在しているなら重複生成しない
            if (NetworkScoreManager.Instance != null && NetworkScoreManager.Instance.IsSpawned)
            {
                Debug.Log("[PreventionScoreBootstrap] NetworkScoreManager は既に存在します。");
                return;
            }

            var instance = Instantiate(scoreManagerPrefab);
            var netObj = instance.GetComponent<NetworkObject>();
            netObj.Spawn();

            Debug.Log("[PreventionScoreBootstrap] NetworkScoreManager を Spawn しました。");
        }
    }
}