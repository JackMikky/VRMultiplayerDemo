using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownToggleHelper : MonoBehaviour, IPointerClickHandler
{
    private TMP_Dropdown m_Dropdown;
    private bool m_IsExpanded = false;

    private void Awake()
    {
        m_Dropdown = GetComponent<TMP_Dropdown>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (m_IsExpanded)
        {
            m_Dropdown.Hide();
            m_IsExpanded = false;
        }
        else
        {
            m_IsExpanded = true;

            StartCoroutine(WatchDropdownClose());
        }
    }

    private System.Collections.IEnumerator WatchDropdownClose()
    {
        yield return null;

        while (transform.Find("Blocker") != null || GameObject.Find("Blocker") != null)
        {
            yield return null;
        }
        m_IsExpanded = false;
    }
}