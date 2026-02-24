using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Assets.Team_Prevention.Script
{
    public class ItemUseSpawner : MonoBehaviour
    {
        [Tooltip("掴ませたい XRGrabInteractable を含むプレハブ (Project 資産)")]
        [SerializeField] private GameObject _itemPrefab;

        [Tooltip("Hierarchy 内のアイテム群ルート（例: 'Items' オブジェクト）を指定すると、その子をコピーして生成します")]
        [SerializeField] private Transform _itemsRoot;

        [Tooltip("生成位置のオフセット（attachTransform に対するローカルオフセット）")]
        [SerializeField] private Vector3 _localOffset = Vector3.zero;

        [Tooltip("生成後に Rigidbody を一時的に kinematic にして安定化させる")]
        [SerializeField] private bool _stabilizeRigidbody = true;

        [Header("生成位置の上書き（Near-Far対策）")]
        [Tooltip("未設定時は interactor.attachTransform を使用します。設定すると常にこの Transform を手元として生成します。")]
        [SerializeField] private Transform _forceSpawnPoint;

        private void Awake()
        {
            // ItemUseSpawner が Items に付いている場合、_itemsRoot を自動設定
            if (_itemsRoot == null && gameObject.name == "Items")
            {
                _itemsRoot = transform;
            }
        }

        public XRGrabInteractable SpawnAndAttachToInteractor(XRBaseInteractor interactor)
        {
            if (_itemPrefab == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] _itemPrefab が未設定です。: {gameObject.name}", this);
                return null;
            }

            GameObject instance = Instantiate(_itemPrefab);

            Transform spawnBase = ResolveSpawnBase(interactor);
            if (spawnBase != null && instance != null)
            {
                instance.transform.SetPositionAndRotation(spawnBase.TransformPoint(_localOffset), spawnBase.rotation);
            }

            return SpawnInternal(interactor, instance);
        }

        public XRGrabInteractable SpawnAndAttachToInteractor(XRBaseInteractor interactor, string sceneObjectName)
        {
            if (interactor == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] interactor が null です。", this);
                return null;
            }

            if (string.IsNullOrEmpty(sceneObjectName))
            {
                Debug.LogWarning($"[ItemUseSpawner] sceneObjectName が空です。", this);
                return null;
            }

            Transform source = null;

            if (_itemsRoot != null)
            {
                var all = _itemsRoot.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == sceneObjectName)
                    {
                        source = all[i];
                        break;
                    }
                }
            }

            if (source == null)
            {
                var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == sceneObjectName)
                    {
                        source = all[i];
                        break;
                    }
                }
            }

            if (source == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] シーン内にオブジェクト '{sceneObjectName}' が見つかりません。", this);
                return null;
            }

            GameObject instance = Instantiate(source.gameObject);
            if (instance == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] シーンオブジェクトの複製に失敗しました: {sceneObjectName}", this);
                return null;
            }

            Transform spawnBase = ResolveSpawnBase(interactor);
            if (spawnBase != null)
            {
                instance.transform.SetPositionAndRotation(spawnBase.TransformPoint(_localOffset), spawnBase.rotation);
            }

            return SpawnInternal(interactor, instance);
        }

        private Transform ResolveSpawnBase(XRBaseInteractor interactor)
        {
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

        private XRGrabInteractable SpawnInternal(XRBaseInteractor interactor, GameObject instance)
        {
            if (interactor == null || instance == null)
            {
                Debug.LogWarning("[ItemUseSpawner] SpawnInternal の引数が不正です。", this);
                return null;
            }

            var grab = instance.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] インスタンスに XRGrabInteractable がありません: {instance.name}", instance);
                Destroy(instance);
                return null;
            }

            var rb = instance.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] インスタンスに Rigidbody がありません: {instance.name}", instance);
                Destroy(instance);
                return null;
            }

            if (!instance.activeInHierarchy)
            {
                instance.SetActive(true);
            }

            if (_stabilizeRigidbody)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            XRInteractionManager manager = interactor.interactionManager != null
                ? interactor.interactionManager
                : FindFirstObjectByType<XRInteractionManager>();

            if (manager == null)
            {
                Debug.LogWarning($"[ItemUseSpawner] XRInteractionManager が見つかりません。", instance);
                Destroy(instance);
                return null;
            }

            grab.interactionManager = manager;

            StartCoroutine(StartManualInteractionNextFrame(interactor, grab, rb));

            return grab;
        }

        private IEnumerator StartManualInteractionNextFrame(XRBaseInteractor interactor, XRGrabInteractable grab, Rigidbody rb)
        {
            yield return new WaitForEndOfFrame();
            yield return null;

            if (interactor == null || grab == null || grab.gameObject == null)
            {
                yield break;
            }

            try
            {
                interactor.StartManualInteraction(grab as IXRSelectInteractable);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ItemUseSpawner] StartManualInteraction に失敗しました: {ex}", grab);
                if (grab != null && grab.gameObject != null)
                {
                    Destroy(grab.gameObject);
                }
                yield break;
            }

            if (_stabilizeRigidbody && rb != null)
            {
                yield return null;
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
        }
    }
}