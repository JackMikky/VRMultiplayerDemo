using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI; // UIコンポーネントを使用するために必要

// ToDo: 要マルチ対応
public class MoveSceneButtonManager : MonoBehaviour
{
    [SerializeField]
    public GameObject targetObject; // 移動させたいGameObjectをインスペクターで設定

    [SerializeField]
    private GameObject destinationObject; // 移動先の位置を持つGameObjectをインスペクターで設定

    [SerializeField]
    public GameObject hpBarObject; // 非アクティブ化する対象のGameObjectをインスペクターで設定

    [SerializeField]
    private bool activateHpBarOnClick = true; // trueならHPバーを有効、falseなら無効

    private Button button; // ボタンコンポーネント

    // 自動で探す対象の名前
    private const string TargetObjectName = "XR Origin Hands (XR Rig) MP Template Variant Customized";

    void Start()
    {
        // 名前から対象を自動検索（未設定時のみ）
        if (targetObject == null)
        {
            var found = GameObject.Find(TargetObjectName);
            if (found != null)
            {
                targetObject = found;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Target GameObject \"{TargetObjectName}\" が見つかりませんでした。インスペクターで手動設定してください。");
            }
        }

        // ボタンコンポーネントを取得
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            // 必要ならログなど
            // UnityEngine.Debug.LogWarning("Button component not found on this GameObject.");
        }
    }

    // ボタンがクリックされたときに呼び出されるメソッド
    private void OnButtonClicked()
    {
        if (targetObject != null && destinationObject != null)
        {
            // 対象のGameObjectを移動先のGameObjectの位置に移動
            targetObject.transform.position = destinationObject.transform.position;
        }

        if (hpBarObject != null)
        {
            // フラグに応じてHPバーのアクティブを切り替え
            hpBarObject.SetActive(activateHpBarOnClick);
        }
    }

    // Updateは使わないのでそのまま残しておく
    void Update()
    {

    }
}