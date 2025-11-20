using UnityEngine;

public class DoNotDestroyObject : MonoBehaviour
{
    private void OnEnable()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}