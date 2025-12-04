using UnityEngine;

public class PenTrailDestoryer : MonoBehaviour
{
    private GameObject parentObject;

    public void SetParent(GameObject gameObject)
    {
        this.parentObject = gameObject;
    }

    private void OnDestroy()
    {
        Destroy(parentObject);
    }
}