using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityInstrument : MonoBehaviour {

    public Transform follow;

    [SerializeField] private float frequencyMin = 1f;
    [SerializeField] private float frequencyMax = 1000f;
    [SerializeField] private float gainMin = 0;
    [SerializeField] private float gainMax = 0.1f;
    [SerializeField] private float noteMin = 20f;
    [SerializeField] private float noteMax = 90f;
    [SerializeField] private float ringFreqMin = 1f;
    [SerializeField] private float ringFreqMax = 1000f;
    [SerializeField] private float ringMultMin = 0;
    [SerializeField] private float ringMultMax = 1f;
    
    [SerializeField] private float velocityMax = 10f;
    [SerializeField] private float velocityLimit = 15f; //value to see if velocity is reasonable

    [SerializeField] private float heightMax = 2f;

    [SerializeField] private int historySize = 10;

    private Hv_quadSynth_AudioLib synth;
    private Vector3 untrackedJointScale = new Vector3(0, 1, 1);
    private float speed;
    private float velocityAverage = 0.0f;
    private Vector3 positionAverage = Vector3.zero;
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<float> velocityHistory = new List<float>();


    // Use this for initialization
    void Start () {
        synth = GetComponent<Hv_quadSynth_AudioLib>();

	}
	
	// Update is called once per frame
	void Update () {

        
        if(positionHistory.Count >= historySize)
        {
            positionAverage = Vector3.LerpUnclamped(positionHistory[0], positionAverage, ((float)positionHistory.Count) / (positionHistory.Count - 1));
            positionHistory.RemoveAt(0);
        }
        if(velocityHistory.Count >= historySize)
        {
            velocityAverage = Mathf.LerpUnclamped(velocityHistory[0], velocityAverage, ((float)velocityHistory.Count) / (velocityHistory.Count - 1));
            velocityHistory.RemoveAt(0);
        }


        transform.position = follow.position;

        Vector3 position = transform.position;
        positionHistory.Add(position);

        positionAverage = Vector3.LerpUnclamped(positionHistory[positionHistory.Count - 1], positionAverage, ((float)positionHistory.Count - 1) / positionHistory.Count);

        if (follow.localScale == untrackedJointScale)
        {
            velocityHistory.Add(0);
            velocityAverage = Mathf.LerpUnclamped(velocityHistory[velocityHistory.Count - 1], velocityAverage, ((float)velocityHistory.Count - 1) / velocityHistory.Count);


        }
        else
        {
            
            if (positionHistory.Count > 1)
            {

                float velocity = Vector3.Magnitude(position - positionHistory[positionHistory.Count - 2]) / Time.deltaTime;

                

                if (velocity < velocityLimit) //check if the velocity is a reasonable value
                {
                    velocityHistory.Add(velocity);

                    velocityAverage = Mathf.LerpUnclamped(velocityHistory[velocityHistory.Count - 1], velocityAverage, ((float)velocityHistory.Count - 1) / velocityHistory.Count);
                }
            }
            


        }
        

        synth.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Gain, Mathf.Lerp(gainMin, gainMax, velocityAverage / velocityMax));
        synth.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Freqcutoff, Mathf.Lerp(frequencyMin, frequencyMax, velocityAverage / velocityMax));
        synth.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Oscnote, Mathf.Lerp(noteMin, noteMax, positionAverage.y / heightMax));
        synth.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Ringmodfreq, Mathf.Lerp(ringFreqMin, ringFreqMax, positionAverage.z / heightMax));
        synth.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Ringmodmultiplier, Mathf.Lerp(ringMultMin, ringMultMax, velocityAverage/velocityMax));


    }
    
}
