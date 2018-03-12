﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class NoteGenerator : MonoBehaviour
{
    public int SongIndex = 0;

    public Song[] PracticeSongs;
    public Song[] GigSongs;

    public AudioSource MusicWithLeadAudioSource;
    public AudioSource MusicWithoutLeadAudioSource;
    public AudioSource NoteGenerationAudioSource;

    public AudioMixerGroup PracticeWithLeadMixer;
    public AudioMixerGroup PracticeWithoutLeadMixer;
    public AudioMixerGroup GigWithLeadMixer;
    public AudioMixerGroup GigWithoutLeadMixer;

    private int SampleDataLength = 1024;  //1024 samples, which is about 80 ms on a 44khz stereo clip, beginning at the current sample position of the clip.
    public float NoteGenerationStartDelay = 1;

    public GameObject[] NotePrefabs;
    public float[] NoteSpawnXOffset;

    public float NoteSpawnMinInterval = 0.1f;
    public GameObject EndGamePanel;
    public GameObject PracticeTutorialPanel;
    public GameObject GigTutorialPanel;
    public Text FameText;
    public Text MoneyText;
    public Text AngstText;
    public Text MetalText;
    public Slider ProgressionSlider;

    public static int NoteMultiplier = 1;
    public static int NumberOfUniqueNotes = 2;
    public static float DoubleNoteChance = 0;
    public static float NotesTotal = 0;
    public static bool ShowPracticeTutorial = true;
    public static bool ShowGigTutorial = true;

    public AudioClip VictorySoundHard;
    public AudioClip VictorySoundMedium;
    public AudioClip VictorySoundEasy;
    public AudioClip DefeatSound;

    public Item HardGuitar;
    public Item MediumGuitar;

    private float clipTime = 0;
    private float clipVolume;
    private float lastClipVolume;
    private float volumeTreshold = 0.1f;
    private float UpdateInterval = 0.1f; //For optimizing performance. And to not check same note twice, and at the same time update fast enough to not miss notes.
    private float musicStartDelay = 1;
    private float[] clipSampleData;
    private bool canSendNextNote = true;
    private float noteSpawnTimer = 0;
    private int noteIndex = 0;
    public static List<NoteSet> NoteSets = new List<NoteSet>();

    private bool useLeadAudioSource = true;
    private bool lerpAudio = false;
    private float lerpTimer = 0;
    [SerializeField]
    private float lerpSeconds = 0.25f;

    private Song selectedSong;

    void Start()
    {
        NoteSets.Clear();

        if (ShowPracticeTutorial || ShowGigTutorial)
        {
            SetTutorial(true);
        }
        else if (!ShowPracticeTutorial || !ShowGigTutorial)
        {
            SetTutorial(false);
        }

        Debug.Log(ShowPracticeTutorial);
    }

    void Initialize()
    {
        Debug.Log("INITIALIZE");

        NotesTotal = 0;
        clipSampleData = new float[SampleDataLength];

        //Assign audioclips.
        if (!GigBackgroundManager.GigSession)
        {
            selectedSong = PracticeSongs[SongIndex];
            
            MusicWithLeadAudioSource.outputAudioMixerGroup = PracticeWithLeadMixer;
            MusicWithoutLeadAudioSource.outputAudioMixerGroup = PracticeWithoutLeadMixer;
        }
        else if (GigBackgroundManager.GigSession)
        {
            selectedSong = GigSongs[SongIndex];

            MusicWithLeadAudioSource.outputAudioMixerGroup = GigWithLeadMixer;
            MusicWithoutLeadAudioSource.outputAudioMixerGroup = GigWithoutLeadMixer;
        }
        UpdateInterval = selectedSong.GeneratorUpdateInterval;
        volumeTreshold = selectedSong.MinimumVolumeTreshold;
        musicStartDelay = selectedSong.MusicDelay;

        //Play the music.
        MusicWithLeadAudioSource.clip = selectedSong.MusicWithLead;
        MusicWithLeadAudioSource.PlayDelayed(musicStartDelay);

        MusicWithoutLeadAudioSource.clip = selectedSong.MusicWithoutLead;
        MusicWithoutLeadAudioSource.PlayDelayed(musicStartDelay);

        NoteGenerationAudioSource.clip = selectedSong.MIDIMusic;
        NoteGenerationAudioSource.PlayDelayed(NoteGenerationStartDelay);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            MusicWithLeadAudioSource.time = MusicWithLeadAudioSource.clip.length - 2;
            MusicWithoutLeadAudioSource.time = MusicWithoutLeadAudioSource.clip.length - 2;
            NoteGenerationAudioSource.time = NoteGenerationAudioSource.clip.length - 2;
        }

        if ((!ShowPracticeTutorial && !GigBackgroundManager.GigSession) && !PracticeTutorialPanel.activeSelf || !ShowGigTutorial && !GigTutorialPanel.activeSelf)
        {
            if (NoteGenerationAudioSource.isPlaying && CheckForNote() && noteSpawnTimer >= NoteSpawnMinInterval && !EndGamePanel.activeSelf)
                SendNote();
            else if (!MusicWithLeadAudioSource.isPlaying && Application.isFocused && !EndGamePanel.activeSelf && !PauseMenu.paused && Time.timeScale > 0) //End game if song is over and the game hasn't already ended.
                EndGame(true);

            if (lerpAudio)
            {
                LerpAudioSourceVolume();
            }
        }

        if (MusicWithLeadAudioSource.clip != null)
            ProgressionSlider.value = MusicWithLeadAudioSource.time / MusicWithLeadAudioSource.clip.length;
    }

    bool CheckForNote()
    {
        noteSpawnTimer += Time.deltaTime;
        clipTime += Time.deltaTime;
        if (clipTime >= UpdateInterval)
        {
            clipTime = 0f;
            NoteGenerationAudioSource.clip.GetData(clipSampleData, NoteGenerationAudioSource.timeSamples);
            foreach (float sample in clipSampleData)
            {
                clipVolume += Mathf.Abs(sample);
            }
            //Debug.Log(clipVolume);

            //Set volumetreshold to the volume of the first note.
            //if (volumeTreshold <= 1 && clipVolume > 1 || clipVolume < volumeTreshold && clipVolume > 1)
            //    volumeTreshold = clipVolume;

            //If the tone is long, create only one note.
            //if (clipVolume >= lastClipVolume)
            //    canSendNextNote = true;
            //lastClipVolume = clipVolume;

            if (clipVolume >= volumeTreshold)
            {
                clipVolume = 0f;
                canSendNextNote = false;
                return true;
            }
        }
        clipVolume = 0f;
        return false;
    }

    void SendNote()
    {
        NoteSet noteSet = new NoteSet();

        float doubleNoteRng = Random.Range(0f, 1f);
        if (doubleNoteRng < DoubleNoteChance)
            NoteMultiplier++;

        int tempIndex = 0;
        for (int i = 0; i < NoteMultiplier; i++)
        {
            //While-loop are to ensure unique notes every send.
            do
            {
                tempIndex = Random.Range(0, NumberOfUniqueNotes);
            }
            while (noteIndex == tempIndex && doubleNoteRng < DoubleNoteChance);
            noteIndex = tempIndex;


            noteSet.Notes.Add(Instantiate(NotePrefabs[noteIndex], new Vector2(transform.position.x + NoteSpawnXOffset[noteIndex], transform.position.y), Quaternion.identity));
            NotesTotal++;
        }
        NoteSets.Add(noteSet);

        if (NoteMultiplier > 1)
            NoteMultiplier = 1;

        noteSpawnTimer = 0;
    }

    public void EndGame(bool victory = true)
    {
        Debug.Log("ENDED GAME");

        EndGamePanel.SetActive(true);
        FindObjectOfType<TimingString>().enabled = false;

        MusicWithLeadAudioSource.Stop();
        MusicWithoutLeadAudioSource.Stop();
        NoteGenerationAudioSource.Stop();

        angstStatScript angst = FindObjectOfType<angstStatScript>();
        metalStatScript metal = FindObjectOfType<metalStatScript>();
        fameStatScript fame = FindObjectOfType<fameStatScript>();
        moneyStatScript money = FindObjectOfType<moneyStatScript>();
        float metalGained = 0;
        float fameGained = 0;
        float moneyGained = 0;
        float angstGained = 0;

        FindObjectOfType<TimingString>().enabled = false;

        //Practice always goes to victory.
        if (victory)
        {
            Debug.Log("VICTORY");

            //Victory sound, based on the most expensive guitar purchased.
            SwitchMusicSource(true);
            if (ShopSystem.MyInventory.Contains(HardGuitar))
                MusicWithLeadAudioSource.PlayOneShot(VictorySoundHard);
            else if (ShopSystem.MyInventory.Contains(MediumGuitar))
                MusicWithLeadAudioSource.PlayOneShot(VictorySoundMedium);
            else
                MusicWithLeadAudioSource.PlayOneShot(VictorySoundEasy);

            Debug.Log(TimingSystem.FailedTimingCounter);

            //Calculate rewards then apply them.
#region Calculations
            float metalBase = 20;
            float metalStatMltp = (1 / (1 + (angst.getAmount() / 100)));
            float metalPerformance = ((1.5f * NotesTotal) / (NotesTotal + (2 * TimingSystem.FailedTimingCounter)));
            float metalItemMltp = TimingString.MetalMultiplier;
            metalGained = Mathf.CeilToInt(metalBase * metalStatMltp * metalPerformance * metalItemMltp);

            float fameBase = 50;
            float fameStatMltp = (2 / (10 - (metal.getAmount() / 15)));
            float famePerformance = ((1.5f * NotesTotal) / (NotesTotal + TimingSystem.FailedTimingCounter));
            float fameItemMltp = 1;
            fameGained = Mathf.CeilToInt(fameBase * fameStatMltp * famePerformance * fameItemMltp);

            float moneyBase = 3000;
            float moneyStatMltp = (6 / (100 - fame.getAmount()));
            float moneyPerformance = 1;
            float moneyItemMltp = 1;
            moneyGained = Mathf.CeilToInt(moneyBase * moneyStatMltp * moneyPerformance * moneyItemMltp);

            float angstBase = -15;
            float angstStatMltp = 1;
            float angstPerformance = ((1.5f * NotesTotal) / (NotesTotal + (2 * TimingSystem.FailedTimingCounter)));
            float angstItemMltp = 1;
            angstGained = Mathf.CeilToInt(angstBase * angstStatMltp * angstPerformance * angstItemMltp);

            if (moneyGained > money.getMax() || moneyGained < 0)
                moneyGained = money.getMax();
            if (fameGained > fame.getMax() || fameGained < 0)
                fameGained = fame.getMax();
            if (metalGained > metal.getMax() || metalGained < 0)
                metalGained = metal.getMax();
            //if (angstGained > angst.getMax() || angstGained < 0)
            //    angstGained = angst.getMax();
#endregion

            Debug.Log(NotesTotal + " TOTAL, " + TimingString.NotesHit + " HIT");

            UpdateScoreBoard(MetalText, metalBase, metalStatMltp, metalPerformance, 1, metalItemMltp, metalGained);
            UpdateScoreBoard(AngstText, angstBase, angstStatMltp, angstPerformance, 1, angstItemMltp, angstGained);

            metal.addOrRemoveAmount(metalGained);
            angst.addOrRemoveAmount(angstGained);

            if (GigBackgroundManager.GigSession)
            {
                FameText.transform.parent.gameObject.SetActive(true);
                MoneyText.transform.parent.gameObject.SetActive(true);

                UpdateScoreBoard(FameText, fameBase, fameStatMltp, famePerformance, 1, fameItemMltp, fameGained);
                UpdateScoreBoard(MoneyText, moneyBase, moneyStatMltp, moneyPerformance, 1, moneyItemMltp, moneyGained);
                
                fame.addOrRemoveAmount(fameGained);
                money.addOrRemoveAmount(moneyGained);

                if (fame.getAmount() >= fame.getMax())
                {
                    GameManager.ToEndGame = true;
                    GameManager.EndGameTitleText = "You're famous and won the game!";
                }
            }
        }
        else
        {
            Debug.Log("DEFEAT");

            SwitchMusicSource(true);
            MusicWithLeadAudioSource.PlayOneShot(DefeatSound);

            if (GigBackgroundManager.GigSession)
            {
                UpdateScoreBoard(FameText, -10, 1, 1, 1, 1, -10);
                UpdateScoreBoard(MetalText, -20, 1, 1, 1, 1, -20);
                UpdateScoreBoard(AngstText, 20, 1, 1, 1, 1, 20);
                UpdateScoreBoard(MoneyText, 150, 1, 1, 1, 1, 150);

                fame.addOrRemoveAmount(-10);
                metal.addOrRemoveAmount(-20);
                angst.addOrRemoveAmount(20);
                money.addOrRemoveAmount(150);
            }
        }
    }

    private void UpdateScoreBoard(Text text, float baseStat, float statMltp, float performance, float proficiency, float itemMltp, float total)
    {
        text.text = baseStat.ToString("0.00") + "\n" +
            statMltp.ToString("0.00") + "x" + "\n" +
            performance.ToString("0.00") + "x" + "\n" +
            proficiency.ToString("0.00") + "x" + "\n" +
            itemMltp.ToString("0.00") + "x" + "\n" + "\n" +
            total.ToString("0.00");
    }

    public void SwitchMusicSource(bool withLead)
    {
        lerpTimer = 0;

        //Lerp audio if there has been a switch.
        if (withLead != useLeadAudioSource)
            lerpAudio = true;

        if (withLead)
        {
            useLeadAudioSource = true;
        }
        else
        {
            useLeadAudioSource = false;
        }

    }

    public void LerpAudioSourceVolume()
    {
        lerpTimer += Time.deltaTime / lerpSeconds;

        if (useLeadAudioSource)
        {
            MusicWithLeadAudioSource.volume = Mathf.Lerp(0, 1, lerpTimer);
            MusicWithoutLeadAudioSource.volume = Mathf.Lerp(1, 0, lerpTimer);
        }
        else if (!useLeadAudioSource)
        {
            MusicWithLeadAudioSource.volume = Mathf.Lerp(1, 0, lerpTimer);
            MusicWithoutLeadAudioSource.volume = Mathf.Lerp(0, 1, lerpTimer);
        }

        if (lerpTimer >= 1)
            lerpAudio = false;
    }

    public void SetTutorial(bool active)
    {
        if (!GigBackgroundManager.GigSession)
        {
            ShowPracticeTutorial = active;
            PracticeTutorialPanel.SetActive(active);
        }
        if (GigBackgroundManager.GigSession)
        {
            ShowGigTutorial = active;
            GigTutorialPanel.SetActive(active);
        }

        ToggleMusic(!active);
    }

    public void ToggleMusic(bool resume)
    {
        if (!resume)
        {
            NoteGenerationAudioSource.Pause();
            MusicWithLeadAudioSource.Pause();
            MusicWithoutLeadAudioSource.Pause();

            FindObjectOfType<TimingString>().enabled = false;
            Time.timeScale = 0;
        }
        else if (resume)
        {
            NoteGenerationAudioSource.UnPause();
            MusicWithLeadAudioSource.UnPause();
            MusicWithoutLeadAudioSource.UnPause();

            FindObjectOfType<TimingString>().enabled = true;
            Time.timeScale = 1;

            if (!NoteGenerationAudioSource.isPlaying && NoteGenerationAudioSource.time <= 0)
                Initialize();
        }
    }

    public void LoadHub()
    {
        Time.timeScale = 1;

        if (FindObjectOfType<GameManager>() != null)
            FindObjectOfType<GameManager>().LoadHUB();

        //If you start the game in the Practice-scene.
        else
            SceneManager.LoadScene(0);
    }

    public static void Reset()
    {
        Debug.Log("RESETED");

        NoteMultiplier = 1;
        NumberOfUniqueNotes = 2;
        DoubleNoteChance = 0;
        ShowPracticeTutorial = true;
        ShowGigTutorial = true;
    }
}
