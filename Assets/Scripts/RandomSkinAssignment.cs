using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSkinAssignment : MonoBehaviour {

    public bool active;
    public bool ordered;
    public GameObject[] skinArray;
    static int assignment = 0;

	// Use this for initialization
	void Start () {
		


        if(ordered)
        {
            skinArray[assignment].SetActive(true);
            if (assignment < skinArray.Length - 1)
            {
                assignment++;
            }
            else
            {
                assignment = 0;
            }
        }
        else //otherwise random
        {
            float random = Random.Range(0,skinArray.Length-0.001f);
            int pick = Mathf.FloorToInt(random);
            skinArray[pick].SetActive(true);
        }

	}
	
	// Update is called once per frame
    private void OnDestroy()
    {
        //if (assignment > 0) assignment--;
    }
}
