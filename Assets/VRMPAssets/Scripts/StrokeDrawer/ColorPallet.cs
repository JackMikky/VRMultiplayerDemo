using UnityEngine;

public class ColorPallet : MonoBehaviour
{
    [SerializeField] private Color color = Color.red;

    private void Start()
    {
        GetComponent<Renderer>().material.color = color;
    }

    public Color GetColor()
    {
        return color;
    }
}