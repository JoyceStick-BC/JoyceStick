using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCollisionScript : MonoBehaviour {

    public float hardness;
    public string eventName;
    private AudioManager AudioManager;

    // Use this for initialization
    void Start () {
        AudioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnCollisionEnter(Collision collision)
    {
        AudioCollisionScript otherObj = collision.gameObject.GetComponent<AudioCollisionScript>();

        if (otherObj == null)
        {
            return;
        }

        if (hardness > otherObj.hardness)
        {
            AudioManager.SetRTPCValue("Velocity", gameObject.GetComponent<Rigidbody>().velocity.magnitude);
            AudioManager.PlayEvent(eventName);
        }

        if ( hardness == otherObj.hardness)
        {
            if (gameObject.GetComponent<Rigidbody>().velocity.magnitude > collision.relativeVelocity.magnitude)
            {
                AudioManager.SetRTPCValue("Velocity", gameObject.GetComponent<Rigidbody>().velocity.magnitude);
                AudioManager.PlayEvent(eventName);
            }
        }

        return;
    }
}
