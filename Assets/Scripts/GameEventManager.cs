﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using System.IO;
using UnityEngine.UI;
using Newtonsoft.Json;

public class GameEventManager : MonoBehaviour
{
    public GameObject EventPanel;
    public Text TitleText, DescriptionText, SenderNameTitle;
    public GameObject[] PanelChoiceButtons;
    public GameObject ClosePanelButton;
    public GameObject[] SMSChoiceButtons;
    public GameObject RecievedMessagePrefab;
    public GameObject SentMessagePrefab;
    public GameObject SMSScrollContent;

    public GameObject GigMessagePanel;
    public GameObject GigUpcomingPanel;
    public GameObject RentReminderPanel;

    public EventEnum[] MapEventPins;

    public Animator OpenPhoneController;
    public GameObject MessageNotification;
    public GameObject HUBMessageNotification;

    [Header("Event Variables")]
    [Range(0, 1)]
    public float SpecialNodeChance = 0;

    [Range(0, 100)]
    public float FameWeekCap, MusicWeekCap, SocialWeekCap;
    public float MoneyCap = 1000;

    public float RecieveMessageDelay = 0.75f;
    public int SMSDayInterval = 1;

 
    private List<StoryNode> choices = new List<StoryNode>();

    public static List<StoryNode> messageNodes = new List<StoryNode>();
    public static List<StoryNode> socialNodes = new List<StoryNode>();
    public static List<StoryNode> musicNodes = new List<StoryNode>();
    public static List<StoryNode> fameNodes = new List<StoryNode>();
    public static List<StoryNode> moneyNodes = new List<StoryNode>();
    public static List<StoryNode> specialNodes = new List<StoryNode>();

    [HideInInspector]
    public enum nodeType { social, musical, fame, special, money }

    private StoryNode firstNode;
    private string YesChar = "#";

    private void Awake()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameManager.HUBScene)
        {
            LoadEvents("messageNodes", messageNodes);
            LoadEvents("socialNodes", socialNodes);
            LoadEvents("musicNodes", musicNodes);
            LoadEvents("fameNodes", fameNodes);
            LoadEvents("moneyNodes", moneyNodes);
            LoadEvents("specialNodes", specialNodes);
        }
    }

    void Start()
    {
        //Fetch the mappins here because they start inactive.
        foreach (EventEnum e in MapEventPins)
        {
            e.StartEventEnum();
            GameManager.sleep += e.RefreshEvent;
        }
    }

    private void OnDestroy()
    {
        foreach (EventEnum e in MapEventPins)
        {
            GameManager.sleep -= e.RefreshEvent;
        }
    }

    void LoadEvents(string _fileName, List<StoryNode> _nodes)
    {
        using (StreamReader s = new StreamReader(Application.dataPath + "/Nodes/" + _fileName + ".txt"))
        {
            string line = s.ReadLine();
            while (line != null)
            {
                StoryNode node = JsonConvert.DeserializeObject<StoryNode>(line);
                _nodes.Add(node);

                line = s.ReadLine();
            }
        }
    }

    public void CheckForStatEvents()
    {
        //Currently take the highest stat and trigger a event based on the highest one.
        int[] statValues = new int[3];
        statValues[0] = Mathf.RoundToInt(FindObjectOfType<metalStatScript>().getAmount());
        statValues[1] = Mathf.RoundToInt(FindObjectOfType<fameStatScript>().getAmount());
        statValues[2] = Mathf.RoundToInt(FindObjectOfType<angstStatScript>().getAmount());

        if (GameManager.day % SMSDayInterval == 0)
        {
            //TODO: Replace statValues[0,1,2] with different Nodelists accordingly.
            if (Mathf.Max(statValues) == statValues[0])
                TriggerSMSEvent(messageNodes[Random.Range(0, messageNodes.Count)]);
            else if (Mathf.Max(statValues) == statValues[1])
                TriggerSMSEvent(messageNodes[Random.Range(0, messageNodes.Count)]);
            else if (Mathf.Max(statValues) == statValues[2])
                TriggerSMSEvent(messageNodes[Random.Range(0, messageNodes.Count)]);
        }
    }

    public void TriggerEventFromPool(EventEnum eventEnum)
    {
        TriggerEvent(EventEnum.NodesSelected[eventEnum.NodeIndex]);
        EventEnum.NodesSelected[eventEnum.NodeIndex] = null;
    }

    public void TriggerSMSEvent(StoryNode node, bool firstNode = true)
    {
        if (firstNode)
        {
            ClearSMSPanel();
            AudioManager.instance.Play("MobilNotification");
            SenderNameTitle.text = node.Title;

            OpenPhoneController.SetTrigger("RecievedNotification");
            MessageNotification.SetActive(true);
            HUBMessageNotification.SetActive(true);
        }
        else
        {
            AudioManager.instance.Play("MessageRecieved");
        }
        if (node.Text == "" || node.Text == null)
            return;

        Debug.Log(node.Title);

        GameObject message = Instantiate(RecievedMessagePrefab, SMSScrollContent.transform, false);
        message.GetComponentInChildren<Text>().text = node.Text;

        choices.Clear();

        for (int i = 0; i < SMSChoiceButtons.Length; i++)
        {
            SMSChoiceButtons[i].SetActive(false);
        }

        for (int i = 0; i < node.Choices.Count; i++)
        {
            choices.Add(node.Choices[i]);
            SMSChoiceButtons[i].SetActive(true);
            SMSChoiceButtons[i].transform.GetComponentInChildren<Text>().text = choices[i].Title;
        }

        GiveRewards(node);
    }

    public void ClearSMSPanel()
    {
        for (int i = SMSScrollContent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(SMSScrollContent.transform.GetChild(i).gameObject);
        }
    }

    public void TriggerEvent(StoryNode node)
    {
        if (node.EnergyBonus != null && node.EnergyBonus != "" && node.EnergyBonus != YesChar && FindObjectOfType<energyStatScript>().getAmount() - float.Parse(node.EnergyBonus) < 0)
            return;

        Debug.Log(node.Title);


        EventPanel.SetActive(true);

        TitleText.text = node.Title;
        DescriptionText.text = node.Text;

        choices.Clear();
        for (int i = 0; i < node.Choices.Count; i++)
        {
            Debug.Log(node.Choices.Count);

            choices.Add(node.Choices[i]);
            PanelChoiceButtons[i].SetActive(true);
            PanelChoiceButtons[i].transform.GetComponentInChildren<Text>().text = choices[i].Title;
        }

        if (choices.Count > 1)
            firstNode = node;
        if (choices.Count < 2 && node.EnergyBonus == YesChar)
            GiveRewards(firstNode);

        if (!PanelChoiceButtons[0].activeSelf)
        {
            ClosePanelButton.SetActive(true);
        }
        else
        {
            ClosePanelButton.SetActive(false);
        }
    }

    void GiveRewards(StoryNode node)
    {
        if (node.MetalBonus != null && node.MetalBonus != "")
            FindObjectOfType<metalStatScript>().addOrRemoveAmount(float.Parse(node.MetalBonus));
        if (node.CashBonus != null && node.CashBonus != "")
            FindObjectOfType<moneyStatScript>().addOrRemoveAmount(float.Parse(node.CashBonus));
        if (node.AngstBonus != null && node.AngstBonus != "")
            FindObjectOfType<angstStatScript>().addOrRemoveAmount(float.Parse(node.AngstBonus));
        if (node.FameBonus != null && node.FameBonus != "")
            FindObjectOfType<fameStatScript>().addOrRemoveAmount(float.Parse(node.FameBonus));
        if (node.EnergyBonus != null && node.EnergyBonus != "")
            FindObjectOfType<energyStatScript>().addOrRemoveAmount(float.Parse(node.EnergyBonus));
    }

    public void MakeChoice(int index)
    {
        for (int i = 0; i < PanelChoiceButtons.Length; i++)
        {
            PanelChoiceButtons[i].SetActive(false);
        }

        TriggerEvent(choices[index]);
    }

    public void MakeSMSChoice(int index)
    {
        for (int i = 0; i < SMSChoiceButtons.Length; i++)
        {
            SMSChoiceButtons[i].SetActive(false);
        }

        GameObject message = Instantiate(SentMessagePrefab, SMSScrollContent.transform, false);
        message.GetComponentInChildren<Text>().text = choices[index].Title;

        StartCoroutine(WaitForSMS(index));
    }

    IEnumerator WaitForSMS(int index)
    {
        SMSScrollContent.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0;
        yield return new WaitForSeconds(RecieveMessageDelay);

        TriggerSMSEvent(choices[index], false);

        yield return new WaitForEndOfFrame();
        SMSScrollContent.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0;
    }

    public void TriggerGig()
    {
        GigMessagePanel.SetActive(true);
        GigMessagePanel.transform.parent.parent.gameObject.SetActive(true);
    }

    public void TriggerUpcomingGig()
    {
        GigUpcomingPanel.SetActive(true);
        GigUpcomingPanel.transform.parent.parent.gameObject.SetActive(true);
    }

    public void TriggerRentReminder()
    {
        RentReminderPanel.SetActive(true);
        RentReminderPanel.transform.parent.parent.gameObject.SetActive(true);
    }
}
