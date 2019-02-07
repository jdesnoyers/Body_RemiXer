using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RainTurbulenceControl : MonoBehaviour {

    public ParticleSystem rain;
    public float max =1 ;
    public float min = 0.01f;
    public float maxRadialV = 50f;
    public TextMeshPro rainText;

    private Rigidbody rigidDisc;

	// Use this for initialization
	void Start () {
        rigidDisc = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        var no = rain.noise;

        Vector3 localAngularVelocity = transform.InverseTransformDirection(rigidDisc.angularVelocity);
        float output = Mathf.Lerp(min, max, Mathf.Abs(localAngularVelocity.z / maxRadialV));
        no.strength = output;
        rainText.text = "Rain Turbulence: \n" + Mathf.RoundToInt(output*100) + "%";

	}
}
