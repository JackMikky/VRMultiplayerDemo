using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


public class AdjustTimeScale : MonoBehaviour
{
    TextMeshProUGUI textMesh;

    private void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (Mouse.current != null)
        {
            float scrollValue = Mouse.current.scroll.ReadValue().y; // スクロール量取得

            if (scrollValue > 0f)
            {
                if (Time.timeScale < 1.0f)
                {
                    Time.timeScale += 0.1f;
                }
            }
            else if (scrollValue < 0f)
            {
                if (Time.timeScale >= 0.2f)
                {
                    Time.timeScale -= 0.1f;
                }
            }

            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            if (textMesh != null)
            {
                textMesh.text = "Time Scale : " + System.Math.Round(Time.timeScale, 2);
            }
        }

    }

    void OnApplicationQuit()
    {
        Time.timeScale = 1.0F;
        Time.fixedDeltaTime = 0.02F;
    }
}