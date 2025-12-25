using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI; // UIコンポーネントを使用するために必要

// ToDo: 要マルチ対応
public class MoveSceneButtonManager : MonoBehaviour
{
    [SerializeField]
    private GameObject targetObject; // 移動させたいGameObjectをインスペクターで設定

    [SerializeField]
    private GameObject destinationObject; // 移動先の位置を持つGameObjectをインスペクターで設定

    [SerializeField]
    public GameObject hpBarObject; // 非アクティブ化する対象のGameObjectをインスペクターで設定

    [SerializeField]
    private bool activateHpBarOnClick = true; // trueならHPバーを有効、falseなら無効

    private Button button; // ボタンコンポーネント

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ボタンコンポーネントを取得
        button = GetComponent<Button>();

        if (button != null)
        {
            // ボタンのクリックイベントにリスナーを登録
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