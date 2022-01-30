using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowHideCrosshair : MonoBehaviour
{
    public GameObject crosshairZoom;
    public GameObject crosshair;

    // Update is called once per frame
    void Update()
    {
     
      {   
         if (Input.GetMouseButton(1))
                {

                    crosshair.SetActive(false);
                    crosshairZoom.SetActive(true);
                    
                }
                else 
                {
                    
                    crosshair.SetActive(true);
                    crosshairZoom.SetActive(false);
                }
    }
    }
}
