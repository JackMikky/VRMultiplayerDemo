using System.Diagnostics;
using UnityEngine;

public class changeobject : MonoBehaviour
{
    [SerializeField] GameObject table;  // 衝突フラグ用
    [SerializeField] GameObject VHFCT;  // VHFCT
    private bool isObjectAActive = true;

    void awake()
    {
        table.SetActive(true);
        VHFCT.SetActive(false);
    }
   

    public void change()
    {
        // 状態を反転させる
        isObjectAActive = !isObjectAActive;

        // 状態に合わせて表示・非表示を切り替え
        table.SetActive(isObjectAActive);
        VHFCT.SetActive(!isObjectAActive);

    }

}
