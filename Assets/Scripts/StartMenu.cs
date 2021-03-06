﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{

    private void Start()
    {
        AudioManager.instance.Play("IntroSound");
    }

    public void LoadHUB()
    {
        GameManager.RestartGame = true;
        GameManager.IsFirstHubRun = true;
        GameManager.IsFirstWorkRun = true;
        GameManager.IsFirstPracticeRun = true;
        GameManager.IsFirstGigRun = true;
        SceneManager.LoadScene(GameManager.HUBScene);
    }

    public void LoadTutorial()
    {
        GameManager.RestartGame = true;
        SceneManager.LoadScene("WorkTutorialScene");
    }

    public void Quit()
    {
        Application.Quit();
    }

}
