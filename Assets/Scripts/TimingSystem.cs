﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TimingSystem : MonoBehaviour
{

    public KeyCode ActivasionKey1;
    public KeyCode ActivasionKey2;
    public KeyCode ActivasionKey3;

    public bool CanComboKey;
    public bool CanExitCollider;

    public List<GameObject> targets = new List<GameObject>();
    public List<GameObject> hitTargets = new List<GameObject>(); //Shitty solution to simultanious "good and miss" appearances.

    public static float ActivatedMechanicAndMissedNotesCounter = 0;

    private void Start()
    {
        ActivatedMechanicAndMissedNotesCounter = 0;
    }

    void Update()
    {
        if (Input.GetKeyDown(ActivasionKey1) || Input.GetKeyDown(ActivasionKey2) || Input.GetKeyDown(ActivasionKey3))
            ActivateMechanic();
    }

    void ActivateMechanic()
    {
        ActivatedMechanicAndMissedNotesCounter++;

        if (targets.Count == 0)
        {
            FailTiming();
            return;
        }
        else if (targets.Count > 0)
        {
            //Need multiple if-statements to handle simultanious notes.
            //Need to check count at every state because SucceedTiming removes target in timingstring.
            if (targets.Count > 0 && targets[0].name.Contains("_1") && Input.GetKey(ActivasionKey1))
                SucceedTiming();
            if (targets.Count > 0 && targets[0].name.Contains("_2") && Input.GetKey(ActivasionKey2))
                SucceedTiming();
            if (targets.Count > 0 && targets[0].name.Contains("_3") && Input.GetKey(ActivasionKey3))
                SucceedTiming();

            return;
        }
    }

    public virtual void FailTiming()
    {
        Debug.Log("FAILED TIMING");
        ActivatedMechanicAndMissedNotesCounter++;

        if (targets.Count > 0)
        {
            targets.RemoveAt(0);
        }
    }

    public virtual void SucceedTiming()
    {
        Debug.Log("SUCCEEDED TIMING");
        hitTargets.AddRange(targets);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "TimingObject" && !targets.Contains(collision.gameObject))
        {
            targets.Add(collision.gameObject);
        }
    }
}
