using UnityEngine;
using VRTK;
using System.Collections;

public class ShowOnInteract : MonoBehaviour
{

    public GameObject gameObject;

    void Start()
    {
        if (GetComponent<VRTK_InteractableObject>() == null)
        {
            Debug.LogError("ShowOnInteract is required to be attached to an Object that has the VRTK_InteractableObject script attached to it");
            return;
        }

        if(gameObject.activeInHierarchy == true)
        {
            Debug.LogError("gameObject must be inactive to start");
            return;
        }
        
        GetComponent<VRTK_InteractableObject>().InteractableObjectGrabbed += new InteractableObjectEventHandler(ObjectGrabbed);
        GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += new InteractableObjectEventHandler(ObjectLetGo);
    }

    private void ObjectGrabbed(object sender, InteractableObjectEventArgs e)
    {
        gameObject.SetActive(true);
    }

    private void ObjectLetGo(object sender, InteractableObjectEventArgs e)
    {
        gameObject.SetActive(false);
    }
}