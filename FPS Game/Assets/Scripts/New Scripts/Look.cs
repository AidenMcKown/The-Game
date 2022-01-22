using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Look : MonoBehaviourPunCallbacks
{
    public Transform player;
    public Transform cams;
    public Transform weapon;

    public float xSensitivity;
    public float ySensitivity;
    public float maxAngle;

    private Quaternion camCenter;

    [SerializeField] public static bool cursorLocked = true;

    void Start()
    {
        camCenter = cams.localRotation;
    }

    void Update()
    {
        if(!photonView.IsMine) return;

        setY();
        setX();

        updateCursorLock();
    }

    void setY()
    {
        float input = Input.GetAxis("Mouse Y") * ySensitivity * Time.deltaTime;
        Quaternion adjustment = Quaternion.AngleAxis(input, -Vector3.right);
        Quaternion delta = cams.localRotation * adjustment;

        if (Quaternion.Angle(camCenter, delta) < maxAngle)
        {
            cams.localRotation = delta;
        }

        weapon.rotation = cams.rotation;
    }

    void setX()
    {
        float input = Input.GetAxis("Mouse X") * xSensitivity * Time.deltaTime;
        Quaternion adjustment = Quaternion.AngleAxis(input, Vector3.up);
        Quaternion delta = player.localRotation * adjustment;
        player.localRotation = delta;
    }

    void updateCursorLock()
    {
        if(cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible= false;

            if(Input.GetKeyDown(KeyCode.Escape))
                cursorLocked = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible= true;

            if(Input.GetKeyDown(KeyCode.Escape))
                cursorLocked = true;
        }
    }
}

