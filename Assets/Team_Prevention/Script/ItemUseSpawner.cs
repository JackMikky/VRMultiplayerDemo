using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Assets.Team_Prevention.Script
{
    public class ItemUseSpawner : MonoBehaviour
    {
        /// <summary>
        /// アイテムの生成・配置モード
        /// </summary>
        public enum SpawnMode
        {
            /// <summary>手元（leftController等）に親子付けして追従</summary>
            FollowHand = 0,

            /// <summary>身体の指定Transformに親子付けして追従（腰・肩など）</summary>
            FollowBody = 1,

            /// <summary>指定座標にワールド固定（追従しない）</summary>
            WorldFixed = 2,
        }

        [Tooltip("掴ませたい XRGrabInteractable を含むプレハブ (Project 資産)")]
        [SerializeField] private GameObject _itemPrefab;

        [Tooltip("Hierarchy 内のアイテム群ルート（例: 'Items' オブジェクト）を指定すると、その子をコピーして生成します")]
        [SerializeField] private Transform _itemsRoot;

        [Tooltip("生成位置のオフセット（spawnBase に対するローカルオフセット）")]
        [SerializeField] private Vector3 _localOffset = Vector3.zero;

        [Header("生成位置の上書き（手元基準）")]
        [Tooltip("未設定時は interactor.attachTransform を使用します。設定すると常にこの Transform を手元として生成します。")]
        [SerializeField] private Transform _forceSpawnPoint;

        [Header("身体固定用 Transform（デフォルト / フォールバック）")]
        [Tooltip("FollowBody モード時のデフォルトアタッチ先。ItemData にタグが指定されている場合はそちらが優先されます。")]
        [SerializeField] private Transform _bodyAttachPoint;

        [Header("掴み開始時の姿勢補正")]
        [Tooltip("true の場合、掴み開始時にインタラクタの回転へスナップする挙動を無効化し、生成時の向きを維持します。")]
        [SerializeField] private bool _disableMatchAttachRotationOnSpawn = true;

        [Header("追従方式の設定")]
        [Tooltip("追従モード時、生成物の Rigidbody を kinematic にして重力を無効化します。")]
        [SerializeField] private bool _followDisablePhysics = true;

        [Tooltip("追従モード時、生成物の Collider を無効化します（手や環境との衝突で暴れないようにする）。")]
        [SerializeField] private bool _followDisableColliders = false;

        [Header("デバッグ")]
        [SerializeField, Tooltip("true のとき診断ログを詳細出力します。")]
        private bool _debugLogManualGrab = true;

        /// <summary>
        /// 外部から「手元基準Transform」を注入します（マルチプレイヤー対応）。
        /// </summary>
        public void SetForceSpawnPoint(Transform spawnPoint)
        {
            _forceSpawnPoint = spawnPoint;
        }

        /// <summary>
        /// 外部から「身体固定用Transform（デフォルト）」を注入します。
        /// </summary>
        public void SetBodyAttachPoint(Transform bodyAttachPoint)
        {
            _bodyAttachPoint = bodyAttachPoint;
        }

        private void Awake()
        {
            // ItemUseSpawner が Items に付いている場合、_itemsRoot を自動設定
            if (_itemsRoot == null && gameObject.name == "Items")
            {
                _itemsRoot = transform;
            }
        }

        /// <summary>
        /// アイテム名を指定して生成します。
        /// ItemData に設定された SpawnMode / オフセットを自動的に適用します。
        /// spawnModeOverride / spawnPointOverride を明示した場合はそちらが優先されます。
        /// </summary>
        public XRGrabInteractable SpawnAndAttachToInteractor(
            XRBaseInteractor interactor,
            string sceneObjectName,
            SpawnMode? spawnModeOverride = null,
            Transform spawnPointOverride = null)
        {
            if (interactor == null)
            {
                Debug.LogWarning("[ItemUseSpawner] interactor が null です。", this);
                return null;
            }

            if (string.IsNullOrEmpty(sceneObjectName))
            {
                Debug.LogWarning("[ItemUseSpawner] sceneObjectName が空です。", this);
                return null;
            }

            // アイテム生成（ItemData も同時に取得）
            ItemData itemData = null;
            var instance = CreateInstanceByName(sceneObjectName, out itemData);
            if (instance == null)
            {
                return null;
            }

            // SpawnMode: 呼び出し側指定 > ItemData設定 > デフォルト(FollowHand)
            SpawnMode resolvedMode = spawnModeOverride.HasValue
                ? spawnModeOverride.Value
                : (itemData != null ? itemData.SpawnMode : SpawnMode.FollowHand);

            // オフセット: ItemData から SpawnMode に対応するオフセットを取得
            Vector3 posOffset = _localOffset;
            Vector3 rotOffsetEuler = Vector3.zero;

            if (itemData != null)
            {
                var poseOffset = itemData.GetSpawnPoseOffset(resolvedMode);
                posOffset = _localOffset + poseOffset.localPositionOffset;
                rotOffsetEuler = poseOffset.localRotationOffsetEuler;
            }

            // ItemData のタグ情報も含めて spawnBase を解決
            Transform spawnBase = ResolveSpawnBase(interactor, resolvedMode, spawnPointOverride, itemData);
            if (spawnBase != null)
            {
                instance.transform.SetPositionAndRotation(
                    spawnBase.TransformPoint(posOffset),
                    spawnBase.rotation * Quaternion.Euler(rotOffsetEuler));
            }

            return SpawnInternal(interactor, instance, spawnBase, resolvedMode, posOffset, rotOffsetEuler);
        }

        /// <summary>
        /// プレハブを指定して生成します。spawnMode で配置方法を指定してください。
        /// </summary>
        public XRGrabInteractable SpawnAndAttachToInteractor(
            XRBaseInteractor interactor,
            SpawnMode spawnMode = SpawnMode.FollowHand,
            Transform spawnPointOverride = null)
        {
            if (_itemPrefab == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] _itemPrefab が未設定です。: {gameObject.name}", this);
                return null;
            }

            var instance = Instantiate(_itemPrefab);
            // プレハブ直接指定の場合は ItemData なし（デフォルト動作）
            Transform spawnBase = ResolveSpawnBase(interactor, spawnMode, spawnPointOverride, itemData: null);
            if (spawnBase != null)
            {
                instance.transform.SetPositionAndRotation(
                    spawnBase.TransformPoint(_localOffset), spawnBase.rotation);
            }

            return SpawnInternal(interactor, instance, spawnBase, spawnMode);
        }

        /// <summary>
        /// spawnMode と spawnPointOverride に応じて生成基準 Transform を解決します。
        /// itemData が指定されている場合、FollowBody 時に以下の優先順で検索します：
        ///   1. BodyAttachPointTag（タグ検索）
        ///   2. BodyAttachPointName（interactor のルートから子孫を名前で再帰検索）
        ///   3. _bodyAttachPoint（フォールバック）
        /// </summary>
        /// <summary>
        /// spawnMode に応じて生成基準 Transform を解決します。
        ///
        /// spawnPointOverride は <see cref="SpawnMode.FollowHand"/> のときのみ有効です。
        /// - FollowHand  : spawnPointOverride → _forceSpawnPoint → interactor の順
        /// - FollowBody  : BodyAttachPointName → _bodyAttachPoint → FollowHand にフォールバック
        /// - WorldFixed  : _forceSpawnPoint → interactor の順（spawnPointOverride は無視）
        /// </summary>
        private Transform ResolveSpawnBase(
            XRBaseInteractor interactor,
            SpawnMode spawnMode,
            Transform spawnPointOverride,
            ItemData itemData)
        {
            switch (spawnMode)
            {
                case SpawnMode.FollowHand:
                default:
                    // spawnPointOverride は FollowHand のみ有効
                    if (spawnPointOverride != null)
                    {
                        return spawnPointOverride;
                    }

                    if (_forceSpawnPoint != null)
                    {
                        return _forceSpawnPoint;
                    }

                    if (interactor == null)
                    {
                        return null;
                    }

                    return interactor.attachTransform != null ? interactor.attachTransform : interactor.transform;

                case SpawnMode.FollowBody:
                    // FollowBody は spawnPointOverride を無視し、専用ロジックで解決する

                    // ① GameObject 名で interactor のルートから子孫を再帰検索
                    if (itemData != null && !string.IsNullOrEmpty(itemData.BodyAttachPointName))
                    {
                        var searchRoot = spawnPointOverride != null ? spawnPointOverride : interactor?.transform;
                        var found = FindTransformByNameFromRoot(searchRoot, itemData.BodyAttachPointName);
                        if (found != null)
                        {
                            Debug.Log($"[ItemUseSpawner] BodyAttachPointName='{itemData.BodyAttachPointName}' 発見: " +
                                      $"path={GetTransformPath(found)}", this);
                            return found;
                        }

                        Debug.LogWarning(
                            $"[ItemUseSpawner] BodyAttachPointName='{itemData.BodyAttachPointName}' が" +
                            "XR Rig ルート配下に見つかりません。_bodyAttachPoint にフォールバックします。", this);
                    }

                    // ② デフォルトの _bodyAttachPoint を使用
                    if (_bodyAttachPoint != null)
                    {
                        Debug.LogWarning($"[ItemUseSpawner] _bodyAttachPoint フォールバック: " +
                                         $"path={GetTransformPath(_bodyAttachPoint)}", this);
                        return _bodyAttachPoint;
                    }

                    Debug.LogWarning(
                        "[ItemUseSpawner] FollowBody が指定されましたが _bodyAttachPoint が未設定です。" +
                        "FollowHand にフォールバックします。", this);

                    goto case SpawnMode.FollowHand;

                case SpawnMode.WorldFixed:
                    // WorldFixed は spawnPointOverride を無視する
                    // （呼び出し元から leftController.transform 等が渡ってきても手元固定にならないようにする）
                    if (_forceSpawnPoint != null)
                    {
                        return _forceSpawnPoint;
                    }

                    if (interactor == null)
                    {
                        return null;
                    }

                    return interactor.attachTransform != null ? interactor.attachTransform : interactor.transform;
            }
        }
        /// <summary>Transform のシーン上フルパスを返すデバッグ用ユーティリティ</summary>
        private static string GetTransformPath(Transform t)
        {
            if (t == null) { return "(null)"; }
            var path = t.name;
            var parent = t.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// interactor の transform.root（XR Rig のルート）から、
        /// 親方向 → ルート → 子孫全体の順で GameObject 名が一致する Transform を検索します。
        /// </summary>
        /// <param name="interactor">起点となる Interactor</param>
        /// <param name="targetName">検索する GameObject 名</param>
        /// <returns>見つかった Transform。見つからない場合は null。</returns>
        private static Transform FindTransformByNameFromRoot(Transform searchOrigin, string targetName)
        {
            if (searchOrigin == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            // ① 親方向を直接辿る（高速・浅い階層向け）
            var current = searchOrigin.parent;
            while (current != null)
            {
                if (current.name == targetName)
                {
                    return current;
                }

                current = current.parent;
            }

            // ② 見つからなければ root 配下を再帰検索（兄弟ブランチ含む）
            return FindDeepChildByName(searchOrigin.root, targetName);
        }

        /// <summary>
        /// 指定した Transform の子孫から、名前が一致する最初の Transform を再帰的に返します。
        /// </summary>
        private static Transform FindDeepChildByName(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                var result = FindDeepChildByName(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// アイテム名に一致する GameObject を複製して返します。
        /// ItemData も同時に取得します。
        /// </summary>
        private GameObject CreateInstanceByName(string sceneObjectName, out ItemData itemData)
        {
            itemData = null;
            ItemDataHolder sourceHolder = null;

            // _itemsRoot 配下から検索（優先）
            if (_itemsRoot != null)
            {
                var all = _itemsRoot.GetComponentsInChildren<ItemDataHolder>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null)
                    {
                        continue;
                    }

                    var data = all[i].ItemData;
                    if (data == null)
                    {
                        continue;
                    }

                    if (data.Name == sceneObjectName)
                    {
                        sourceHolder = all[i];
                        itemData = data;
                        break;
                    }
                }
            }

            // 見つからなければシーン全体から検索（フォールバック）
            if (sourceHolder == null)
            {
                var all = FindObjectsByType<ItemDataHolder>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null)
                    {
                        continue;
                    }

                    var data = all[i].ItemData;
                    if (data == null)
                    {
                        continue;
                    }

                    if (data.Name == sceneObjectName)
                    {
                        sourceHolder = all[i];
                        itemData = data;
                        break;
                    }
                }
            }

            if (sourceHolder == null)
            {
                Debug.LogWarning(
                    $"[ItemUseSpawner] ItemData.Name='{sceneObjectName}' に一致する ItemDataHolder が見つかりません。", this);
                return null;
            }

            var instance = Instantiate(sourceHolder.gameObject);
            if (instance == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] 複製に失敗しました: {sceneObjectName}", this);
            }

            return instance;
        }

        /// <summary>
        /// 生成処理の共通実装。spawnMode に応じて追従/固定を切り替えます。
        /// </summary>
        private XRGrabInteractable SpawnInternal(
            XRBaseInteractor interactor,
            GameObject instance,
            Transform spawnBase,
            SpawnMode spawnMode,
            Vector3 posOffset,
            Vector3 rotOffsetEuler)
        {
            if (instance == null)
            {
                Debug.LogWarning("[ItemUseSpawner] SpawnInternal: instance が null です。", this);
                return null;
            }

            var grab = instance.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                Debug.LogWarning(
                    $"[ItemUseSpawner] XRGrabInteractable がありません: {instance.name}", instance);
                Destroy(instance);
                return null;
            }

            if (!instance.activeInHierarchy)
            {
                instance.SetActive(true);
            }

            switch (spawnMode)
            {
                case SpawnMode.FollowHand:
                case SpawnMode.FollowBody:
                    SpawnAsFollower(interactor, instance, grab, spawnBase, spawnMode, posOffset, rotOffsetEuler);
                    break;

                case SpawnMode.WorldFixed:
                    SpawnAsWorldFixed(instance, grab);
                    break;

                default:
                    SpawnAsFollower(interactor, instance, grab, spawnBase, spawnMode, posOffset, rotOffsetEuler);
                    break;
            }

            return grab;
        }

        // フォローモード用 SpawnInternal の簡潔版も残す（プレハブ用）
        private XRGrabInteractable SpawnInternal(
            XRBaseInteractor interactor,
            GameObject instance,
            Transform spawnBase,
            SpawnMode spawnMode)
        {
            return SpawnInternal(interactor, instance, spawnBase, spawnMode, _localOffset, Vector3.zero);
        }


        /// <summary>
        /// 追従方式（FollowHand / FollowBody）の生成処理。
        /// </summary>
        private void SpawnAsFollower(
            XRBaseInteractor interactor,
            GameObject instance,
            XRGrabInteractable grab,
            Transform spawnBase,
            SpawnMode spawnMode,
            Vector3 localPositionOffset,
            Vector3 localRotationOffsetEuler,
            Transform spawnPointOverride = null)
        {
            if (spawnBase == null)
            {
                Debug.LogWarning(
                    $"[ItemUseSpawner] {spawnMode} ですが spawnBase が null です。" +
                    "SetForceSpawnPoint / SetBodyAttachPoint を設定してください。", instance);
                Destroy(instance);
                return;
            }

            // FollowBody のとき、XRGrabInteractable の Grab/Drop サイクルを完全に無効化する
            // Grab() 内で SetParent(null) が呼ばれるため、親子付けが切断されてしまうのを防ぐ
            if (spawnMode == SpawnMode.FollowBody)
            {
                grab.enabled = false;
                grab.trackPosition = false;
                grab.trackRotation = false;
            }

            // spawnBase の子として親子付け → 追従
            instance.transform.SetParent(spawnBase, worldPositionStays: false);
            instance.transform.localPosition = localPositionOffset;
            instance.transform.localRotation = Quaternion.Euler(localRotationOffsetEuler);

            // 物理を止めて落下させない
            if (_followDisablePhysics)
            {
                var rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // 必要に応じて Collider を無効化
            if (_followDisableColliders)
            {
                var cols = instance.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cols.Length; i++)
                {
                    if (cols[i] == null)
                    {
                        continue;
                    }

                    cols[i].enabled = false;
                }
            }

            // ItemEffectTrigger へ注入する Interactor を解決する
            // FollowBody 時は spawnPointOverride の階層からアクティブな Interactor を取得する
            var effectTrigger = instance.GetComponent<ItemEffectTrigger>();
            if (effectTrigger != null)
            {
                var resolvedInteractor = ResolveInteractorForEffect(interactor, spawnMode, spawnBase);
                if (resolvedInteractor != null)
                {
                    effectTrigger.SetCurrentInteractorForFollowMode(resolvedInteractor);
                }
            }

            Debug.Log(
                $"[ItemUseSpawner] {spawnMode} で生成: {instance.name}, parent={spawnBase.name}", instance);
        }

        /// <summary>
        /// ワールド固定方式の生成処理。
        /// </summary>
        private void SpawnAsWorldFixed(GameObject instance, XRGrabInteractable grab)
        {
            // 物理を止めて安定化（必要なら後から解除）
            if (_followDisablePhysics)
            {
                var rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            Debug.Log(
                $"[ItemUseSpawner] WorldFixed で生成: {instance.name}, pos={instance.transform.position}", instance);
        }

        /// <summary>
        /// ItemEffectTrigger に注入する XRBaseInteractor を解決します。
        /// FollowBody 時は spawnPointOverride の Transform 階層から
        /// アクティブかつ有効な XRBaseInteractor を取得します。
        /// </summary>
        private static XRBaseInteractor ResolveInteractorForEffect(
            XRBaseInteractor interactor,
            SpawnMode spawnMode,
            Transform spawnBase)
        {
            // FollowHand はそのまま interactor を使用
            if (spawnMode != SpawnMode.FollowBody)
            {
                return interactor;
            }

            // FollowBody かつ spawnPointOverride がある場合、その階層からアクティブな XRBaseInteractor を取得
            if (spawnBase != null)
            {
                // 子孫を検索（非アクティブ含めて取得し、アクティブかつ enabled なものを優先）
                var candidates = spawnBase.GetComponentsInChildren<XRBaseInteractor>(true);
                for (int i = 0; i < candidates.Length; i++)
                {
                    var candidate = candidates[i];
                    if (candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy)
                    {
                        return candidate;
                    }
                }

                // 子孫に見つからなければ親方向を検索
                var fromParent = spawnBase.GetComponentInParent<XRBaseInteractor>();
                if (fromParent != null && fromParent.enabled && fromParent.gameObject.activeInHierarchy)
                {
                    return fromParent;
                }
            }

            // フォールバック: 元の interactor がアクティブなら使用
            if (interactor != null && interactor.enabled && interactor.gameObject.activeInHierarchy)
            {
                return interactor;
            }

            Debug.LogWarning(
                "[ItemUseSpawner] ResolveInteractorForEffect: アクティブな XRBaseInteractor が見つかりませんでした。", interactor);
            return null;
        }
    }
}