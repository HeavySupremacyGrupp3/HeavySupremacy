﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guitarr : Item {

    private void Start()
    {
        //base.Start();
    }

    public override void ActivatePurchase()
    {
        base.ActivatePurchase();
        Debug.Log("PURCHASED GUITARR!");
        NoteGenerator.NoteMultiplier++;
    }
}
