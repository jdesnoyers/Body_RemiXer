using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UmbrellaControls : MonoBehaviour {

    [SerializeField] private GameObject umbrellaFolded;
    [SerializeField] private GameObject umbrellaUnfolded;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.JoystickButton15))
        {

            umbrellaFolded.SetActive(!umbrellaFolded.activeInHierarchy);
            umbrellaUnfolded.SetActive(!umbrellaUnfolded.activeInHierarchy);
            Debug.Log("trigger pull");

        }
	}
}
