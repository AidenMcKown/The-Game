using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Sway : MonoBehaviourPunCallbacks
{
    public float intensity;
    public float returnToNormal;
    public bool isMine;

    private Quaternion targetRotation;
    private Quaternion originRotation;

    private void Start() {
        originRotation = transform.localRotation;
    }
    private void Update() 
    {
        //if(!photonView.IsMine) return;

        UpdateSway();
    }

    private void UpdateSway()
    {
        // Controls
        float xMouse = Input.GetAxis("Mouse X");
        float yMouse = Input.GetAxis("Mouse Y");

        if (!isMine) // stops infinite recoil for other players
        {
            xMouse = 0;
            yMouse = 0;
        }

        // Calculate Target Rotation
        Quaternion xAdjustment = Quaternion.AngleAxis(-intensity * xMouse, Vector3.up);
        Quaternion yAdjustment = Quaternion.AngleAxis(intensity * yMouse, Vector3.right);
        targetRotation = originRotation * xAdjustment * yAdjustment;

        // Rotate towards target rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * returnToNormal);
    }
}
