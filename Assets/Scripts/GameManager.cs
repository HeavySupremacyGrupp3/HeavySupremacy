﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public FadeOutManager fadeScript;
<<<<<<< HEAD
    public int day;
    public int week;
    private TextMesh t;
=======
    public int day = 1;
    public int week = 1;
    public TextMesh WeekText;
>>>>>>> c0746df35b07b577b5283bee0909a0c4dced169b

    public delegate void mittEvent();
    public static event mittEvent sleep;

    void Start()
    {
        Initialize();
        fadeScript = GetComponent<FadeOutManager>();
<<<<<<< HEAD
        t = GetComponent<TextMesh>();
        t.text = "Approximately week: ";
        day = 1;
        week = 1;
=======
        //WeekText = GetComponent<TextMesh>();
        WeekText.text = "Approximately week: ";
>>>>>>> c0746df35b07b577b5283bee0909a0c4dced169b
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        FindObjectOfType<fameStatScript>().addOrRemoveAmount(10);
        WeekText.text = "Approximately week: " + week;
    }

    void Initialize()
    {
        if (FindObjectsOfType<GameManager>().Length > 1)
            Destroy(FindObjectsOfType<GameManager>()[0]);
        else
            DontDestroyOnLoad(gameObject);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void LoadWork(float energyCost)
    {
        if (CheckEnergy(energyCost))
            SceneManager.LoadScene("WorkScene");
    }

    public void LoadPractice(float energyCost)
    {
        if (CheckEnergy(energyCost))
            SceneManager.LoadScene("PracticeScene");
    }

    public void LoadGig(float energyCost)
    {
        if (CheckEnergy(energyCost))
        {
            GigBackgroundManager.GigSession = true;
            SceneManager.LoadScene("PracticeScene");
        }
    }
    public void LoadHUB()
    {
        GigBackgroundManager.GigSession = false;
        SceneManager.LoadScene("HUBScene");
    }

    public void LoadSleep()
    {
        fadeScript = FindObjectOfType<FadeOutManager>();
        fadeScript.FadeOut();
        IncreaseDay();
        sleep();
    }

    public void IncreaseDay()
    {
        day++;
        IncreaseWeek();
    }

    public void IncreaseWeek()
    {
        if (day % 7 == 0)
        {
            week++;
        }
    }

    public void ToggleGameObject(GameObject target)
    {
        target.SetActive(!target.active);
    }

    bool CheckEnergy(float energyCost)
    {
        if (FindObjectOfType<energyStatScript>().getAmount() - energyCost >= 0)
        {
            FindObjectOfType<energyStatScript>().addOrRemoveAmount(-energyCost);
            return true;
        }
        else
            return false;
    }
}
