using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainCollisionDetecter : MonoBehaviour {

    [SerializeField] private GameObject dronePrefab;
    [SerializeField] private GameObject melodicPrefab;
    [SerializeField] private GameObject percussivePrefab;
    [SerializeField] private Transform parentTransform;
    public ParticleSystem rainParticleSystem;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    // Use this for initialization
    void Start () {
        rainParticleSystem = GetComponent<ParticleSystem>();
    }
	

	void OnParticleCollision(GameObject other) {

        rainParticleSystem.GetCollisionEvents(other, collisionEvents);
        if(other.name == "Floor")
        {
            for (int i = 0; i < collisionEvents.Count; i++)
            {
                Instantiate(dronePrefab, collisionEvents[i].intersection, Quaternion.identity, parentTransform);
            }
        }
        /*else if (other.name == "Umbrella")
        {
        }

        else if (other.name == "Umbrella_Folded")
        {

            for (int i = 0; i < collisionEvents.Count; i++)
            {
                Instantiate(percussivePrefab, collisionEvents[i].intersection, Quaternion.identity, parentTransform);
            }

        }
        else if (other.name == "Head")
        {

        }*/
        else
        {
            for (int i = 0; i < collisionEvents.Count; i++)
            {
                Instantiate(melodicPrefab, collisionEvents[i].intersection, Quaternion.identity, parentTransform);
            }
        }
    }
}
