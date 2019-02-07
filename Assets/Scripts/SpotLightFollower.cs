using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* directs spotlight at object, light can stay in one place and pivot to follow, or light can follow directly above user if fixedLight is off.
 */

public class SpotLightFollower : MonoBehaviour {

    public Transform TargetObject;
    public bool fixedLight;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
        if(!fixedLight)
        {
            transform.position = new Vector3(TargetObject.position.x, transform.position.y, TargetObject.position.z);
        }
        
        transform.LookAt(TargetObject);

    }
}
