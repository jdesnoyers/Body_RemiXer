using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//script to trigger events outside of the animated object on animatorIK

public class AnimatorIkEvent : MonoBehaviour {
    
    public BodyRemixerController bodyTracker;

    UnityEvent animatorIK;
    
	void Start () {
		if(animatorIK == null)
        {
            animatorIK = new UnityEvent();
        }
        if(bodyTracker == null)
        {
            bodyTracker = GameObject.Find("Kinect Space/Body Track").GetComponent<BodyRemixerController>();
        }

        animatorIK.AddListener(bodyTracker.ikAction);
	}
	

    void OnAnimatorIK(int layerIndex)
    {
        animatorIK.Invoke();
    }
}
