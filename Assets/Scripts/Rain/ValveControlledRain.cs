using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValveControlledRain : MonoBehaviour
{

    [SerializeField] private float rainMinSetting;
    [SerializeField] private float rainMaxSetting;
    [SerializeField] private ParticleSystem rainSystem;
    private float rainSetting;


    // Use this for initialization
    void Start()
    {
        rainSetting = (((GetComponent<HingeJoint>().angle / 360) + 0.5f) * (rainMaxSetting - rainMinSetting)) + rainMinSetting;
        var emission = rainSystem.emission;
        emission.rateOverTime = rainSetting;

    }

    // Update is called once per frame
    void Update()
    {
        rainSetting = (((GetComponent<HingeJoint>().angle / 360) + 0.5f) * (rainMaxSetting - rainMinSetting)) + rainMinSetting;
        var emission = rainSystem.emission;
        emission.rateOverTime = rainSetting;
    }
}