using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTest : MonoBehaviour {
	private AudioManager aM;
	private float distance;
	private GameObject playerObj;
	private Vector3 objPos;
	private Vector3 playerPos;


	// Use this for initialization
	void Start () {
		aM = FindObjectOfType<AudioManager> ();
		objPos = gameObject.transform.position;
		playerObj = GameObject.Find ("Player");
		aM.LoadBank ("AudioTest");

	}
	
	// Update is called once per frame
	void Update () {
		playerPos = playerObj.transform.position;
		distance = Vector3.Distance (objPos, playerPos);
//		Debug.Log (distance);

		aM.SetRTPCValue ("Wind_Velocity", distance);


		if (Input.GetKeyDown (KeyCode.A)) {
			aM.PlayEvent ("Aeolus_Start");
		}
		if (Input.GetKeyDown (KeyCode.B)) {
			aM.PlayEvent ("Aeolus_MX_P1");
		}
		if (Input.GetKeyDown (KeyCode.C)){
			aM.PlayEvent ("Aeolus_MX_P2");
		}
		if (Input.GetKeyDown (KeyCode.D)){
			aM.PlayEvent ("Aeolus_SFX_Wind");
		}
		if (Input.GetKeyDown(KeyCode.E)){
			aM.PlayEvent ("Aeolus_StopAll");
		}
	}
}
