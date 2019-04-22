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
            //set and activate display 1 and set resolution to windows system resolution
            reflection.targetDisplay = 1;
            Display.displays[1].Activate();
            Display.displays[1].SetRenderingResolution(Display.displays[1].systemWidth, Display.displays[1].systemHeight);

            //set and activate display 2 and set resolution to windows system resolution
            reflectionLeft.targetDisplay = 2;
            Display.displays[2].Activate();
            Display.displays[2].SetRenderingResolution(Display.displays[2].systemWidth, Display.displays[2].systemHeight);
            for(int i = 0; i < Display.displays.Length; i++)
            {
                Display.displays[i].Activate();
                Display.displays[i].SetRenderingResolution(Display.displays[i].systemWidth, Display.displays[i].systemHeight);
            }

        }

        
    }
	
}
