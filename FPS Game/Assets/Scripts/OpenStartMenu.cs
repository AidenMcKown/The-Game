using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenStartMenu : MonoBehaviour
{
    
    [SerializeField] KeyCode startMenuKey = KeyCode.Escape;
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

		if(Input.GetKeyDown(startMenuKey))
		{
			MenuManager.Instance.OpenMenu("start");
			
		}



    }
}
