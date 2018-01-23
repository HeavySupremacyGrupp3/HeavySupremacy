﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
 
public class FadeOutManager : MonoBehaviour {

    public Image fadeImage;

    private void Start()
    {
        fadeImage.enabled = false;
        fadeImage.color = new Color(1, 1, 1, 0);
    }

    public void FadeOut()
    {
        Debug.Log("Sleeping");
        fadeImage.enabled = true;
        StartCoroutine(FadeAway(true));
        
    }
 
    IEnumerator FadeAway(bool fadeAway)
    {
        
        if (fadeAway)
        {
            // loop over 1 second 
            for (float i = 0; i <= 1; i += Time.deltaTime)
            {
                // alpha opaque
                fadeImage.color = new Color(0, 0, 0, i);
                yield return new WaitForSeconds (0.05f);
                StartCoroutine(FadeAway(false));
            }
        }
        
        else
        {
            // loop over 1 second backwards
            for (float i = 1; i >= 0; i -= Time.deltaTime)
            {
                // alpha transparent
                fadeImage.color = new Color(0, 0, 0, i);
                yield return new WaitForSeconds(0.05f);

                if (i <= 0)
                {
                    fadeImage.enabled = false;  //This isn't working yet :c
                }
              
            }
            
        }
    }
}