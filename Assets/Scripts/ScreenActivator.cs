using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenActivator : MonoBehaviour {

    public Camera reflection;
    public Camera reflectionLeft;


	// Use this for initialization
	void Start () {
        if (Display.displays.Length > 1)
        {
            reflection.targetDisplay = 1;
            Display.displays[1].Activate();

            reflectionLeft.targetDisplay = 2;
            Display.displays[2].Activate();

        }

        
    }
	
}
