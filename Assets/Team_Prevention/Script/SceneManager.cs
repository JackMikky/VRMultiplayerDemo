using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [SerializeField]
    public GameObject hpBarObject; // 非アクティブ化する対象のGameObjectをインスペクターで設定

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //if (hpBarObject != null)
        //{
        //    // 対象のGameObjectを非アクティブ化
        //    hpBarObject.SetActive(false);
        //}
        //else
        //{
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}