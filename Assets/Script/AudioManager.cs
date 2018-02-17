using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
	
	public static AudioManager instance = null;

	private uint bankID;

	void Awake(){
		if (instance == null) {
			instance = this;
		} else if (instance != this) {
			Destroy (gameObject);
		}
	}
		
	public void LoadBank (string bankName){
		AkSoundEngine.LoadBank (bankName, AkSoundEngine.AK_DEFAULT_POOL_ID, out bankID);
	}

	public void PlayEvent (string eventName){
		AkSoundEngine.PostEvent (eventName, gameObject);
	}

	public void SetRTPCValue (string rtpcName, float rtpcValue){
		AkSoundEngine.SetRTPCValue (rtpcName ,rtpcValue);
	}

//	public void SetGameObjectPosition (GameObject objectToSet, Transform objectTransform){
//		AkSoundEngine.SetObjectPosition (objectToSet, objectTransform);
//	}

//	public void SettingAttenuation (){
//		AkSoundEngine.
//	}
}
