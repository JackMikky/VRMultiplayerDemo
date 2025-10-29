using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpController : MonoBehaviour
{
    [SerializeField] Material fullScreenMat;

    [Range(0, 10)]
    [SerializeField] float fadeTime=1;

    [Range(0, 5)]
    [SerializeField] float waitTime=1;

    float currentfade = 0;

    bool fadeOut;

    bool fadeIn;
    float changeTimer=0;

    [SerializeField] GameObject Screen1;

    [SerializeField] GameObject Screen2;


    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        StartFade();
    }

    public void StartFade()
    {
        currentfade = Mathf.Clamp(currentfade, 0, 1);
        if (fadeOut)
        {
            var tValue = 1 / fadeTime;
            currentfade += tValue * Time.fixedDeltaTime;
            
            if (fullScreenMat.HasProperty("_T"))
            {
                fullScreenMat.SetFloat("_T", currentfade);
            }

            if(currentfade >= 1)
            {
                Screen1.SetActive(false);
                Screen2.SetActive(true);
                changeTimer += Time.fixedDeltaTime;
                if (changeTimer >= waitTime)
                {
                    FadeIn();
                }

            }
        }

        if (fadeIn)
        {
            var tValue = 1 / fadeTime;
            currentfade -= tValue * Time.fixedDeltaTime;
            if (fullScreenMat.HasProperty("_T"))
            {
                fullScreenMat.SetFloat("_T", currentfade);
            }

            if (currentfade <= 0)
            {
                Destroy(this);
            }
        }
    }

    public void FadeIn()
    {
        this.fadeIn = true;
        this.fadeOut = false;
    }

    public void FadeOut()
    {
        this.fadeIn = false;
        this.fadeOut = true;
    }

    private void OnDestroy()
    {
        fullScreenMat.SetFloat("_T", 0);
    }
}
