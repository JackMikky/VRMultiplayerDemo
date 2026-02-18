using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Manufacturing
{
    /// <summary>
    /// 組み立て処理管理用クラス
    /// </summary>
    public class AssemblyManager : NetworkBehaviour
    {
        /// <summary>
        /// 組み立て部品情報群
        /// </summary>
        [SerializeField]
        private List<AssemblyParts> AssemblyPartsList;

        /// <summary>
        /// ガイド用部品情報群
        /// </summary>
        [SerializeField]
        private List<GuideParts> GuidePartsList;

        /// <summary>
        /// オブジェクトどうしをくっつける距離の閾値
        /// </summary>
        private readonly float _connectDistance = 0.2f;

        /// <summary>
        /// 完成した組み立て対象IDのセット
        /// </summary>
        private HashSet<int> completedAssemblyTargets = new HashSet<int>();

        /// <summary>
        /// 組み立てが完了したかどうかのフラグ
        /// </summary>
        private bool isCompleted = false;

        /// <summary>
        /// 経過時間（秒）
        /// </summary>
        private float elapsedTime = 0f;

        /// <summary>
        /// 時間計測を開始したかどうか
        /// </summary>
        private bool isTimerStarted = false;

        /// <summary>
        /// メッセージエリアのテキスト
        /// </summary>
        [SerializeField]
        private TMP_Text MessageText;

        /// <summary>
        /// 時間表示するテキスト
        /// </summary>
        [SerializeField]
        private TMP_Text TimeText;

        /// <summary>
        /// 完成したときに出す音声
        /// </summary>
        [SerializeField]
        private AudioSource FinishAudio;

        /// <summary>
        /// 完成したときに出すパーティクル
        /// </summary>
        [SerializeField]
        private ParticleSystem ParticleSystem;

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Init()
        {
            // ユーザーが掴むことのできるオブジェクト群にイベントを登録
            {
                foreach (var part in AssemblyPartsList)
                {
                    // イベントに登録
                    part.OnReleaseEvent.AddListener(OnRelease);
                    part.OnGrabEvent.AddListener(OnGrab);
                }
            }

            // シーン移動時のメッセージ初期化
            MessageText.text = "Begin assembly.\nAssemble the table parts.";
            MessageText.color = Color.black;

            // 時間計測開始
            isTimerStarted = true;
            elapsedTime = 0f;
            UpdateTimeDisplay();
        }

        private void Update()
        {
            // 時間計測処理
            if (isTimerStarted && !isCompleted)
            {
                elapsedTime += Time.deltaTime;
                UpdateTimeDisplay();
            }
        }

        /// <summary>
        /// TimeTextの表示を更新します
        /// </summary>
        private void UpdateTimeDisplay()
        {
            if (TimeText == null) return;

            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            TimeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        /// <summary>
        /// オブジェクトを離した時に呼び出されます
        /// </summary>
        /// <param name="assemblyParts"></param>
        private void OnRelease(AssemblyParts assemblyParts)
        {
            Debug.Log($"離された組み立て部品種別：{assemblyParts.Type}");
            int index = AssemblyPartsList.FindIndex(x => x == assemblyParts);
            if (AssemblyPartsList[index] == null)
            {
                Debug.LogError("離されたオブジェクトが見つかりません。");
                return;
            }

            // サーバー側で処理を実行
            OnReleaseServerRpc(index);
        }

        /// <summary>
        /// オブジェクトを離した時にサーバー側で呼び出されます
        /// </summary>
        [Rpc(SendTo.Server)]
        void OnReleaseServerRpc(int releasePartsIndex)
        {
            // 離されたオブジェクト情報を取得
            var releaseParts = AssemblyPartsList[releasePartsIndex];

            // 接続先のガイド用オブジェクトの接続情報をクリア
            if (releaseParts.ConnectGuidePartsIndex >= 0)
            {
                GuidePartsList[releaseParts.ConnectGuidePartsIndex].IsConnected = false;

                releaseParts.ConnectGuidePartsIndex = -1;
                releaseParts.ConnectGuidePartsIndexNetwork.Value = -1;
            }

            // 離されたオブジェクトのGameObjectとそのRigidbodyを取得
            GameObject releaseObject = releaseParts.gameObject;
            Rigidbody rigidbody = releaseObject.GetComponent<Rigidbody>();

            // 一定距離以内のオブジェクトを入れとくリスト
            List<NearObjectInfo> nearObjects = new List<NearObjectInfo>();

            Vector3 releaseObjectPosition = releaseObject.transform.position;

            // 離されたオブジェクトと一定以内の距離のガイド用オブジェクトを列挙
            foreach (var guideParts in GuidePartsList)
            {
                // 種別とサイズが一致しなければスキップ
                if (guideParts.Type != releaseParts.Type || guideParts.Size != releaseParts.Size)
                {
                    continue;
                }

                // 既に接続されているガイド用オブジェクトはスキップ
                if (guideParts.IsConnected)
                {
                    continue;
                }

                // 距離を計算して一定距離以内であればリストに追加
                float distance = Vector3.Distance(releaseObjectPosition, guideParts.transform.position);
                if (distance < _connectDistance)
                {
                    NearObjectInfo nearObject = new NearObjectInfo(guideParts, distance);
                    nearObjects.Add(nearObject);
                }
            }

            // 無ければ物理演算を適用
            if (nearObjects.Count <= 0)
            {
                rigidbody.isKinematic = false;

                releaseParts.IsKinematicNetwork.Value = false;

                // サーバー以外に通知
                OnReleaseRpc(releasePartsIndex, -1, false);

                return;
            }

            // 一番近いオブジェクト情報を取得
            NearObjectInfo objectInfo = nearObjects.OrderBy(obj => obj.Distance).First();

            // 一番近いガイド用オブジェクトにはめる
            releaseObject.transform.position = objectInfo.NearObject.transform.position;
            releaseObject.transform.rotation = objectInfo.NearObject.transform.rotation;
            rigidbody.isKinematic = true;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            // ネットワーク変数も更新
            releaseParts.IsKinematicNetwork.Value = true;

            // 接続されているフラグをtrueに
            objectInfo.NearObject.IsConnected = true;

            // 接続されているガイドのインデックスを取得
            int nearPartsIndex = GuidePartsList.FindIndex(x => x == objectInfo.NearObject);

            // 接続先のガイド用オブジェクト情報を保存
            releaseParts.ConnectGuidePartsIndex = nearPartsIndex;
            releaseParts.ConnectGuidePartsIndexNetwork.Value = nearPartsIndex;

            // コライダーを破棄
            Destroy(releaseParts.Collider);

            Debug.Log($"組み立て部品(Server): {releaseParts.gameObject.name}");

            // サーバー以外に通知
            OnReleaseRpc(releasePartsIndex, nearPartsIndex, true);

            // 接続されたガイドパーツの組み立て対象IDを取得して完成判定をチェック
            int assemblyTargetId = objectInfo.NearObject.AssemblyTargetId;
            CheckAssemblyCompletion(assemblyTargetId);
        }

        /// <summary>
        /// オブジェクトを離した時にサーバー以外で呼び出されます
        /// </summary>
        [Rpc(SendTo.Everyone)]
        void OnReleaseRpc(int releasePartsIndex, int nearPartsIndex, bool isKinematic)
        {
            // 組み立て部品・ガイドの情報を取得
            var releaseParts = AssemblyPartsList[releasePartsIndex];
            var nearParts = nearPartsIndex >= 0 ? GuidePartsList[nearPartsIndex] : null;

            releaseParts.ConnectGuidePartsIndex = nearPartsIndex;
            Rigidbody rigidbody = releaseParts.GetComponent<Rigidbody>();
            rigidbody.isKinematic = isKinematic;

            if (nearParts != null)
            {
                nearParts.IsConnected = true;

                // コライダーを破棄
                Destroy(releaseParts.Collider);
            }

            Debug.Log($"組み立て部品(Client): {releaseParts.gameObject.name}");
        }

        /// <summary>
        /// 指定された組み立て対象の完成判定をチェックします
        /// </summary>
        /// <param name="assemblyTargetId">チェックする組み立て対象ID</param>
        private void CheckAssemblyCompletion(int assemblyTargetId)
        {
            // 既に完成している場合は処理しない
            if (completedAssemblyTargets.Contains(assemblyTargetId))
            {
                return;
            }

            // 指定された組み立て対象IDのガイドパーツを取得
            var targetGuideParts = GuidePartsList.Where(g => g.AssemblyTargetId == assemblyTargetId).ToList();

            // 対象のガイドパーツが存在しない場合は完成判定を行わない
            if (targetGuideParts.Count == 0)
            {
                return;
            }

            // 指定された組み立て対象のすべてのガイドパーツが接続されているかチェック
            bool allConnected = targetGuideParts.All(guideParts => guideParts.IsConnected);

            if (allConnected)
            {
                completedAssemblyTargets.Add(assemblyTargetId);
                Debug.Log($"組み立て完成！組み立て対象ID: {assemblyTargetId} のすべてのパーツが正しく配置されました。");

                // 全クライアントに完成を通知
                OnAssemblyCompletedRpc(assemblyTargetId);

                // すべての組み立て対象が完成したかチェック
                CheckAllAssembliesCompleted();
            }
        }

        /// <summary>
        /// すべての組み立て対象が完成したかチェックします
        /// </summary>
        private void CheckAllAssembliesCompleted()
        {
            // 全組み立て対象IDを取得
            var allAssemblyTargetIds = GuidePartsList.Select(g => g.AssemblyTargetId).Distinct().ToList();

            // すべての組み立て対象が完成しているかチェック
            bool allCompleted = allAssemblyTargetIds.All(id => completedAssemblyTargets.Contains(id));

            if (allCompleted)
            {
                Debug.Log("すべての組み立て対象が完成しました！");
                OnAllAssembliesCompletedRpc();
            }
        }

        /// <summary>
        /// 組み立て完成時に全クライアントで呼び出されます
        /// </summary>
        /// <param name="assemblyTargetId">完成した組み立て対象ID</param>
        [Rpc(SendTo.Everyone)]
        void OnAssemblyCompletedRpc(int assemblyTargetId)
        {
            completedAssemblyTargets.Add(assemblyTargetId);
            Debug.Log($"組み立て完成通知を受信しました。組み立て対象ID: {assemblyTargetId}");

            if (isCompleted)
            {
                return;
            }

            // 個別の組み立て対象完成時の処理
            OnAssemblyCompleted(assemblyTargetId);
        }

        /// <summary>
        /// すべての組み立て対象が完成した時に全クライアントで呼び出されます
        /// </summary>
        [Rpc(SendTo.Everyone)]
        void OnAllAssembliesCompletedRpc()
        {
            Debug.Log("すべての組み立て対象の完成通知を受信しました。");

            // すべて完成時の処理
            OnAllAssembliesCompleted();
        }

        /// <summary>
        /// 組み立て完成時の処理
        /// </summary>
        /// <param name="assemblyTargetId">完成した組み立て対象ID</param>
        /// <remarks>
        /// 派生クラスでオーバーライドするか、ここに完成時の処理を実装してください
        /// </remarks>
        protected virtual void OnAssemblyCompleted(int assemblyTargetId)
        {
            // 個別の組み立て対象完成時の処理をここに実装
            // 例: 特定のロボットに関するエフェクト表示など
            Debug.Log($"組み立て対象ID {assemblyTargetId} が完成しました。");

            MessageText.text = $"Congratulations!!\n Table{assemblyTargetId} Finished!!";
            MessageText.color = Color.red;
            FinishAudio.Play();
            ParticleSystem.Play();
            isCompleted = true;

            // 時間計測を停止
            isTimerStarted = false;

            // 完了時間を表示
            if (TimeText != null)
            {
                int minutes = Mathf.FloorToInt(elapsedTime / 60f);
                int seconds = Mathf.FloorToInt(elapsedTime % 60f);
                TimeText.text = $"Completion Time: {minutes:00}:{seconds:00}";
            }
        }

        /// <summary>
        /// すべての組み立て対象が完成した時の処理
        /// </summary>
        /// <remarks>
        /// 派生クラスでオーバーライドするか、ここに完成時の処理を実装してください
        /// </remarks>
        protected virtual void OnAllAssembliesCompleted()
        {
            // すべての組み立て対象完成時の処理をここに実装
            Debug.Log($"全組み立て完了！ 完了時間: {elapsedTime:F2}秒");
        }

        /// <summary>
        /// オブジェクトを掴んだ時に呼び出されます
        /// </summary>
        /// <param name="type"></param>
        private void OnGrab(AssemblyParts assemblyParts)
        {
            Debug.Log($"掴んだ組み立て部品種別：{assemblyParts.Type}");
        }
    }


    /// <summary>
    /// 一定距離以内の物体を保存するインナークラス
    /// </summary>
    class NearObjectInfo
    {
        public GuideParts NearObject;

        public float Distance;

        public NearObjectInfo(GuideParts nearObject, float distance)
        {
            NearObject = nearObject;
            Distance = distance;
        }
    }
}
