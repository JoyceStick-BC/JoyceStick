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
}


/*
 *Used events:
 * sfx_Imp_Wood /
 * sfx_Imp_Rock /
 * sfx_Imp_Paper /
 * sfx_Imp_Book / 
 * sfx_Imp_Cloth /
 * sfx_Mvnt_Paper /
 * sfx_Imp_Frame /
 * sfx_Imp_Telegraph /
 * sfx_Imp_Guitar /
 * sfx_Imp_Clock/
 * sfx_Clock / 
 * sfx_Fire / 
 * 
 */

