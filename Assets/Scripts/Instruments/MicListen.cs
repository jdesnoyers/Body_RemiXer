using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicListen : MonoBehaviour {

    public bool active;
    public string microphoneName = "Microphone Array (Xbox NUI Sensor)";
    private AudioSource micSource;

	// Use this for initialization
	void Start () {
        micSource = GetComponent<AudioSource>();

       
        foreach (string mic in Microphone.devices)
        {

            int minFreq;
            int maxFreq;
            Microphone.GetDeviceCaps(mic,out minFreq,out maxFreq);

            Debug.Log(mic + ": " + maxFreq);
        }
	}
	
	// Update is called once per frame
	void Update () {
		
        if(active && !micSource.isPlaying)
        {
            StartMicrophone();
        }
        else if (!active && micSource.isPlaying)
        {
            StopMicrophone();
        }
        

    }

    void StartMicrophone()
    {
        micSource.clip = Microphone.Start(null, true, 10, 16000);
        Debug.Log(micSource.clip);
        if(micSource.clip != null)
        {
            micSource.loop = true;
            //while (!(Microphone.GetPosition(null) > 0)) { };
            micSource.Play();
        }
        //yield return null;
    }
    void StopMicrophone()
    {
        micSource.Stop();
    }


}
