﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tutProduktScript : MonoBehaviour {
	
	public GameObject myPrecious;
	
	public delegate void tutEvent(GameObject t);
	public static event tutEvent unhideText;

	void OnMouseDown()
	{
		unhideText(myPrecious);
	}
}

