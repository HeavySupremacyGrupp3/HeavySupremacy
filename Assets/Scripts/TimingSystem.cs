﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TimingSystem : MonoBehaviour
{

    public KeyCode ActivateKey;
    public bool CanExitCollider;

    protected GameObject target;

    void Update()
    {
        if (Input.GetKeyDown(ActivateKey))
            ActivateMechanic();
    }

    void ActivateMechanic()
    {
        if (target == null)
        {
            FailTiming();
        }
        else if (target != null)
        {
            SucceedTiming();
        }
    }

    public virtual void FailTiming()
    {
        Debug.Log("FAILED TIMING");
		target = null;
    }

    public virtual void SucceedTiming()
    {
        Debug.Log("SUCCEEDED TIMING");
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "TimingObject")
        {
            target = collision.gameObject;
        }
    }
}
