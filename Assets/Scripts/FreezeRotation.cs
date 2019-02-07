using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//prevent this object from rotating despite parent objects rotation

public class FreezeRotation : MonoBehaviour {

    public float xRotation = 0;
    public float yRotation = 0;
    public float zRotation = 0;

    Quaternion fixedRotation;

	void Start () {
        fixedRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        //fixedRotation = transform.rotation;
	}
	

	void LateUpdate () {
        transform.rotation = fixedRotation; //keep the rotation oriented the same as at start
	}
}
