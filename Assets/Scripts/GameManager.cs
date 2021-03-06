﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
    //public static GameManager instance;

    public FadeOutManager fadeScript;
    public float Rent;
    public float GigEnergyCost;
    public static int day = 1;
    public static int week = 1;
    public static bool IsFirstWorkRun = true;
    public static bool IsFirstHubRun = false;
    public static bool IsFirstPracticeRun = true;
    public static bool IsFirstGigRun = true;
    public GameObject EndGamePanel;
    public Text EndGameTitle;
    public SceneTransitionScript SceneTransition;
    public Animator EnergyAnimator;
    public Animator AngstAnimator;
    public Image[] StatPreviewCosts;
    public Image[] StatPreviewRewards;

    [Header("Songs")]
    public static int SongIndex = 0;
    public static Song[] PracticeSongs;
    public static Song[] GigSongs;

    public Song[] Practice;
    public Song[] Gig;

    public delegate void mittEvent();
    public static event mittEvent sleep;

    public string WinGameText;
    public string LoseGameText;
    public static bool ToEndGame;
    public static bool RestartGame;

    public static string HUBScene = "HUBScene", WorkScene = "WorkScene", PracticeScene = "PracticeScene", StartScene = "StartScene";

    /*
    private void Awake()
    {
        if(instance == null)
            instance = this;
        else if(instance != this)
            Destroy(gameObject);
    }
    */

    void Start()
    {
        if (ToEndGame)
            EndGame(0);
        if (RestartGame)
            Restart();

        Initialize();
        fadeScript = GetComponent<FadeOutManager>();

        AudioManager am = AudioManager.instance;
        am.Play("HUBMusic");
        Sound s = Array.Find(am.sounds, Sound => Sound.name == "HUBMusic");
        s.source.time = UnityEngine.Random.Range(0, s.clip.length);

        am.Play("HUBAmbience");

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLevelLoaded;

        if (IsFirstHubRun)
        {
            IsFirstHubRun = !IsFirstHubRun;
            GameObject.Find("HubTutorial").transform.GetChild(0).GetComponent<Tutorial>().Run(); // BECAUSE UNITY IS AWESOME, AND GAMEOBJECT.FIND("HUB_TUTORIAL").GETCOMPONENT<TUTORIAL>() DOESN'T WORK! YAAY! Not even mad.
        }

        PracticeSongs = Practice;
        GigSongs = Gig;
    }

    void Initialize()
    {
        if (FindObjectsOfType<GameManager>().Length > 1)
        {
            Destroy(FindObjectsOfType<GameManager>()[0].gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnLevelLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.buildIndex == 0)
            Destroy(gameObject);

        if (scene.buildIndex == 1)
        {
            Time.timeScale = 1;
            PauseMenu.paused = false;

            ShopSystem.UpdateHUBEnvironment();
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void EndGame(int textChildIndex)
    {
        ToEndGame = false;
        EndGamePanel.SetActive(true);
        EndGamePanel.transform.GetChild(textChildIndex).gameObject.SetActive(true);
    }

    public void Restart()
    {
        RestartGame = false;

        ShopSystem.MyInventory.Clear();

        FindObjectOfType<metalStatScript>().ResetAmount();
        FindObjectOfType<moneyStatScript>().ResetAmount();
        FindObjectOfType<angstStatScript>().ResetAmount();
        FindObjectOfType<fameStatScript>().ResetAmount();
        FindObjectOfType<energyStatScript>().ResetAmount();

        NoteGenerator.Reset();
        TimingString.Reset();
        ToEndGame = false;

        day = 1;
        week = 1;

        if (SceneManager.GetActiveScene().name != HUBScene)
            LoadHUB();
    }

    public void LoadWork(float energy)
    {
        if (CheckAngst())
        {
            if (CheckEnergy(energy))
            {
                AudioManager.instance.Play("DoorClick");
                StopHUBLoops();
                SceneTransition.StartTransition(WorkScene);
            }
        }
    }

    public void LoadPractice(float energy)
    {
        if (CheckEnergy(energy))
        {
            AudioManager.instance.Play("practiceClick");
            StopHUBLoops();
            GigBackgroundManager.GigSession = false;
            SceneTransition.StartTransition(PracticeScene);
        }
    }

    public void LoadGig(float energy)
    {
        if (CheckEnergy(GigEnergyCost))
        {
            StopHUBLoops();
            GigBackgroundManager.GigSession = true;
            SceneTransition.StartTransition(PracticeScene);
        }
    }

    public void LoadStart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(StartScene);
    }

    public void LoadHUB()
    {
        StopHUBLoops();
        GigBackgroundManager.GigSession = false;
        SceneManager.LoadScene(HUBScene);
    }

    public void LoadSleep(float energy)
    {
        fadeScript = FindObjectOfType<FadeOutManager>();
        fadeScript.FadeOut(true, ContinueSleep);
    }

    private void ContinueSleep()
    {
        IncreaseDay();
        sleep();
    }

    public void IncreaseDay()
    {
        day++;
        IncreaseWeek();
        FindObjectOfType<GameEventManager>().CheckForStatEvents();
    }

    public void IncreaseWeek()
    {
        if (day == 5)
        {
            FindObjectOfType<GameEventManager>().TriggerRentReminder();
        }
        if (day == 6)
        {
            FindObjectOfType<GameEventManager>().TriggerUpcomingGig();
        }
        if (day == 7)
        {
            FindObjectOfType<GameEventManager>().TriggerGig();
        }

        if (day == 8)
        {
            week++;
            day = 1;

            SongIndex++;

            FindObjectOfType<fameStatScript>().UpdateWeeklyStatGains();
            FindObjectOfType<metalStatScript>().UpdateWeeklyStatGains();
            FindObjectOfType<angstStatScript>().UpdateWeeklyStatGains();

            if ((FindObjectOfType<moneyStatScript>().getAmount() - Rent) < 0)
                EndGame(2);
            else
                FindObjectOfType<moneyStatScript>().addOrRemoveAmount(-Rent);
        }
    }

    void StopHUBLoops()
    {
        AudioManager.instance.Stop("HUBAmbience");
        AudioManager.instance.Stop("HUBMusic");
    }

    public void ToggleGameObject(GameObject target)
    {
        target.SetActive(!target.activeSelf);
    }

    public bool CheckEnergy(float energyCost, bool reduceEnergy = true)
    {

        if (FindObjectOfType<energyStatScript>().getAmount() - energyCost >= 0)
        {
            if (reduceEnergy)
                FindObjectOfType<energyStatScript>().addOrRemoveAmount(-energyCost);
            return true;
        }
        else
        {
            EnergyAnimator.SetTrigger("LerpEnergy");
            AudioManager.instance.Play("LowEnergy");
            return false;
        }

    }

    bool CheckAngst()
    {
        float angstAmount = FindObjectOfType<angstStatScript>().getAmount();
        if (angstAmount >= 100)
        {
            AngstAnimator.SetTrigger("LerpEnergy");
            AudioManager.instance.Play("LowEnergy");
            return false;
        }
        else
            return true;
    }

    public void UpdateStatPreviewFill(StatPreviewData data)
    {
        int statIndex = data.StatIndex;
        float value = data.Value;

        if (value == 0)
            return;

        float tempValue = 1 - StatPreviewCosts[statIndex].fillAmount;

        if (value < 0)
        {
            value *= -1;
            StatPreviewCosts[statIndex].material.SetFloat("_EdgeWidth", tempValue + (value / 100));
        }
        else if (value > 0)
        {
            StatPreviewRewards[statIndex].fillAmount = StatPreviewCosts[statIndex].fillAmount + (value / 100) - 0.02f; //0.02f is for not showing top green when bar is full. This is because of the fillshader...;
        }
    }

    public void ResetAllStatPreviews()
    {
        for (int i = 0; i < StatPreviewCosts.Length; i++)
        {
            StatPreviewCosts[i].material.SetFloat("_EdgeWidth", 0);
            StatPreviewRewards[i].fillAmount = 0;
        }
    }
}
