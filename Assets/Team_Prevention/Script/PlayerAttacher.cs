using Assets.Team_Prevention.Script.UI;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerAttacher : MonoBehaviour
{
    [Header("ItemBox 初期化用参照")]
    [SerializeField] private MainPlayer targetPlayer;        // ★ 追加
    [SerializeField] private GameObject catalogSearchRoot;   // ★ 追加

    // Lobbyに戻る際はSetActiveをFalseにする
    private void OnDestroy()
    {
        LocalManager.Instance.SetLocalPreventionInvisibility(false);
    }

    private void Start()
    {
        // Prevention_Basic 以外なら何もしない
        if (SceneManager.GetActiveScene().name != "Prevention_Basic")
        {
            return;
        }

        // 1) LocalManager にローカルオブジェクトを表示させる
        if (LocalManager.Instance != null)
        {
            LocalManager.Instance.SetLocalPreventionInvisibility(true);
        }
        else
        {
            Debug.LogWarning("LocalManager.Instance が見つかりません。LocalManager が先にシーンに存在するか確認してください。");
        }

        // 2) ItemBoxComponent を取得
        GameObject itemBoxObject = null;

        // LocalManager 経由で取得（推奨）
        if (LocalManager.Instance != null)
        {
            itemBoxObject = LocalManager.Instance.ItemBoxObject;
        }

        // 取得できなかったら名前で検索（保険）
        if (itemBoxObject == null)
        {
            itemBoxObject = GameObject.Find("ItemBox");
        }

        if (itemBoxObject == null)
        {
            Debug.LogError("[PlayerAttacher] ItemBox の GameObject が見つかりません。");
        }

        var itemBoxComponent = itemBoxObject.GetComponent<ItemBoxComponent>();
        if (itemBoxComponent == null)
        {
            Debug.LogError("[PlayerAttacher] ItemBoxComponent が ItemBox にアタッチされていません。");
        }

        if (targetPlayer == null)
        {
            Debug.LogError("[PlayerAttacher] targetPlayer がインスペクタで設定されていません。MainPlayer をアサインしてください。");
        }

        // ======== ここから XR Rig 関連の自動アタッチ ========

        // 先に一度だけ取得して以後は再利用（←ここが今回の修正ポイント）
        ItemSelectionPhaseManager itemPhaseManager = targetPlayer.GetComponent<ItemSelectionPhaseManager>();
        if (itemPhaseManager == null)
        {
            Debug.LogWarning("[PlayerAttacher] MainPlayer に ItemSelectionPhaseManager がアタッチされていません。");
        }

        // XR Origin (XR Rig) オブジェクトを取得
        var xrOrigin = GameObject.Find("XR Origin Hands (XR Rig) MP Template Variant Customized");

        if (xrOrigin == null)
        {
            Debug.LogError("[PlayerAttacher] XR Origin Hands (XR Rig) MP Template Variant Customized がシーン内に見つかりません。");
        }
        else
        {
            // Camera Offset を取得
            Transform cameraOffsetTf = xrOrigin.transform.Find("Camera Offset");
            if (cameraOffsetTf == null)
            {
                Debug.LogError("[PlayerAttacher] XR Origin 配下に Camera Offset が見つかりません。");
            }
            else
            {
                // -------- MainPlayer 用の Player / Head を設定 --------
                Transform playerTf = cameraOffsetTf.Find("Left Controller/Player");
                Transform headTf = cameraOffsetTf; // そのまま Camera Offset を HeadObject として使う

                if (playerTf == null)
                {
                    Debug.LogError("[PlayerAttacher] Camera Offset 配下に Left Controller/Player が見つかりません。");
                }
                else
                {
                    targetPlayer.PlayerObject = playerTf.gameObject;
                }

                targetPlayer.HeadObject = headTf.gameObject;

                // -------- ItemSelectionPhaseManager 用の各 Controller / Hand を設定 --------
                if (itemPhaseManager != null)
                {
                    Transform leftControllerTf = cameraOffsetTf.Find("Left Controller");
                    Transform rightControllerTf = cameraOffsetTf.Find("Right Controller");
                    Transform leftHandTf = cameraOffsetTf.Find("Left Hand");
                    Transform rightHandTf = cameraOffsetTf.Find("Right Hand");

                    if (leftControllerTf == null)
                        Debug.LogError("[PlayerAttacher] Camera Offset 配下に Left Controller が見つかりません。");
                    if (rightControllerTf == null)
                        Debug.LogError("[PlayerAttacher] Camera Offset 配下に Right Controller が見つかりません。");
                    if (leftHandTf == null)
                        Debug.LogError("[PlayerAttacher] Camera Offset 配下に Left Hand が見つかりません。");
                    if (rightHandTf == null)
                        Debug.LogError("[PlayerAttacher] Camera Offset 配下に Right Hand が見つかりません。");

                    // 見つかったものだけ代入
                    if (leftControllerTf != null)
                        itemPhaseManager.leftController = leftControllerTf.gameObject;
                    if (rightControllerTf != null)
                        itemPhaseManager.rightController = rightControllerTf.gameObject;
                    if (leftHandTf != null)
                        itemPhaseManager.leftHand = leftHandTf.gameObject;
                    if (rightHandTf != null)
                        itemPhaseManager.rightHand = rightHandTf.gameObject;

                    // 念のため targetPlayer もここで保証しておく
                    itemPhaseManager.targetPlayer = targetPlayer;
                }

                // ======== ここから 追加: インベントリUI(※Prefab以外) の自動アタッチ ========

                // UIルート(Canvas)は XR Origin 直下にある前提（画像どおり）
                Transform uiRoot = xrOrigin.transform.Find("Canvas (Body-locked UI)");
                if (uiRoot == null)
                {
                    // 念のため再帰検索（別名や多少の構造違いに備える）
                    uiRoot = FindDeepChildByName(xrOrigin.transform, "Canvas (Body-locked UI)");
                }

                if (uiRoot == null)
                {
                    Debug.LogWarning("[PlayerAttacher] Canvas (Body-locked UI) が見つかりませんでした。インベントリUIの自動アタッチはスキップします。");
                }
                else if (itemPhaseManager != null)
                {
                    // --- ListContent: PlayerItemBox/Scroll View/Viewport/Content ---
                    RectTransform listContentRt = null;
                    var listContentTf = uiRoot.Find("PlayerItemBox/Scroll View/Viewport/Content");
                    if (listContentTf == null)
                    {
                        // 念のため再帰検索（Content名は被りがちなので、パス優先→無ければ最後の名前で探索）
                        var playerItemBox = uiRoot.Find("PlayerItemBox");
                        if (playerItemBox == null)
                            playerItemBox = FindDeepChildByName(uiRoot, "PlayerItemBox");

                        if (playerItemBox != null)
                        {
                            var scrollView = playerItemBox.Find("Scroll View") ?? FindDeepChildByName(playerItemBox, "Scroll View");
                            var viewport = scrollView?.Find("Viewport") ?? FindDeepChildByName(scrollView, "Viewport");
                            var content = viewport?.Find("Content") ?? FindDeepChildByName(viewport, "Content");
                            listContentTf = content;
                        }
                    }
                    if (listContentTf != null)
                    {
                        listContentRt = listContentTf.GetComponent<RectTransform>();
                    }
                    else
                    {
                        Debug.LogError("[PlayerAttacher] List Content(UI) が見つかりません。パス: Canvas/PlayerItemBox/Scroll View/Viewport/Content を確認してください。");
                    }

                    //// --- Buttons: ButtonObjects/Remove, ButtonObjects/Use ---
                    //Button removeBtn = null;
                    //Button useBtn = null;

                    //var buttonObjects = uiRoot.Find("ButtonObjects") ?? FindDeepChildByName(uiRoot, "ButtonObjects");
                    //if (buttonObjects != null)
                    //{
                    //    removeBtn = buttonObjects.Find("Remove")?.GetComponent<Button>()
                    //               ?? FindDeepChildByName(buttonObjects, "Remove")?.GetComponent<Button>();
                    //    useBtn = buttonObjects.Find("Use")?.GetComponent<Button>()
                    //               ?? FindDeepChildByName(buttonObjects, "Use")?.GetComponent<Button>();

                    //    if (removeBtn == null) Debug.LogError("[PlayerAttacher] Remove Button が見つかりません。");
                    //    if (useBtn == null) Debug.LogError("[PlayerAttacher] Use Button が見つかりません。");
                    //}
                    //else
                    //{
                    //    Debug.LogError("[PlayerAttacher] ButtonObjects が見つかりません。");
                    //}

                    //// --- Buttons: ItemSelectButton/Upper, ItemSelectButton/Down ---
                    //Button upBtn = null;
                    //Button downBtn = null;

                    //var itemSelectButton = uiRoot.Find("ItemSelectButton") ?? FindDeepChildByName(uiRoot, "ItemSelectButton");
                    //if (itemSelectButton != null)
                    //{
                    //    upBtn = itemSelectButton.Find("Upper")?.GetComponent<Button>()
                    //           ?? FindDeepChildByName(itemSelectButton, "Upper")?.GetComponent<Button>();
                    //    downBtn = itemSelectButton.Find("Down")?.GetComponent<Button>()
                    //           ?? FindDeepChildByName(itemSelectButton, "Down")?.GetComponent<Button>();

                    //    if (upBtn == null) Debug.LogError("[PlayerAttacher] Up Button(Upper) が見つかりません。");
                    //    if (downBtn == null) Debug.LogError("[PlayerAttacher] Down Button が見つかりません。");
                    //}
                    //else
                    //{
                    //    Debug.LogError("[PlayerAttacher] ItemSelectButton オブジェクトが見つかりません。");
                    //}

                    // --- 見つかったものを ItemSelectionPhaseManager に代入 ---
                    // ※ フィールド名はあなたのクラスに合わせてください
                    if (listContentRt != null) itemPhaseManager.itemListContent = listContentRt;
                    //if (removeBtn != null) itemPhaseManager.removeButton = removeBtn;
                    //if (useBtn != null) itemPhaseManager.useButton = useBtn;
                    //if (upBtn != null) itemPhaseManager.upButton = upBtn;
                    //if (downBtn != null) itemPhaseManager.downButton = downBtn;

                    //Debug.Log("[PlayerAttacher] インベントリUI（Prefab以外）を自動アタッチしました。");
                }
                // ======== インベントリUI 自動アタッチ ここまで ========
            }
        }

        // ======== XR Rig 自動アタッチここまで ========

        // 3) 「アタッチ相当」の初期化
        itemBoxComponent.Initialize(targetPlayer, catalogSearchRoot);

        Debug.Log("[PlayerAttacher] ItemBoxComponent を初期化しました。");
    }

    // -------- ユーティリティ: 子孫を名前で再帰検索 --------
    private static Transform FindDeepChildByName(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var result = FindDeepChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }
}