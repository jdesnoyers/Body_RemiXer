using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropletProcess : MonoBehaviour {

    public float destroyDelay = 5.0f;
    [SerializeField] private float yMin = 0.5f;
    [SerializeField] private float yMax = 2.5f;
    [SerializeField] private float rainRadius = 0.6f;

    [SerializeField] private float gainMin = 0.001f;
    [SerializeField] private float gainMax = 0.005f;

    [SerializeField] private float oscMin = 21f;
    [SerializeField] private float oscMax = 100f;

    [SerializeField] private float decayMin = 50f;
    [SerializeField] private float decayMax = 500f;

    [SerializeField] private bool melodicObj = false;
    public Hv_dropletMax_AudioLib droplet;
    //[SerializeField] private Transform rainTransform; //add in later
    //[SerializeField] private ParticleSystem rainSystem; //add in later

    // Use this for initialization
    void Start ()
    {
        Destroy(gameObject, destroyDelay); //preprogram to self-destruct


        float x = (transform.localPosition.z);
        float y = (transform.position.y);
        float z = (transform.localPosition.z);

        Vector3 bloopNormalized = new Vector3((x + rainRadius) /(2* rainRadius), (y - yMin)/(yMax- yMin), (z + rainRadius) / (2 * rainRadius));

        //Debug.Log(bloopNormalized.z);

        if(droplet != null)
        {
            droplet.SetFloatParameter(Hv_dropletMax_AudioLib.Parameter.Gain,Mathf.Lerp(gainMin,gainMax,bloopNormalized.x));

            if (melodicObj)
            {
                droplet.SetFloatParameter(Hv_dropletMax_AudioLib.Parameter.Decay, Mathf.Lerp(decayMin, decayMax, bloopNormalized.z));
                droplet.SetFloatParameter(Hv_dropletMax_AudioLib.Parameter.Oscnote, Mathf.Lerp(oscMin, oscMax, bloopNormalized.y));
                droplet.SetFloatParameter(Hv_dropletMax_AudioLib.Parameter.Cutoff, Mathf.Lerp(oscMin, oscMax, bloopNormalized.y));

            }
            else
            {
                droplet.SetFloatParameter(Hv_dropletMax_AudioLib.Parameter.Decay, Mathf.Lerp(decayMin, decayMax, bloopNormalized.y));
                droplet.SetFloatParameter(Hv_dropletMax_AudioLib.Parameter.Oscnote, Mathf.Lerp(oscMin, oscMax, bloopNormalized.z));
                droplet.SetFloatParameter(Hv_dropletMax_AudioLib.Parameter.Cutoff, Mathf.Lerp(oscMin, oscMax, bloopNormalized.z));

            }

            droplet.SendEvent(Hv_dropletMax_AudioLib.Event.Bang);
        }

    }
	
}
