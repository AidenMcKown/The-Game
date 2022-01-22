using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviourPunCallbacks
{
    #region Variables
    [SerializeField] public float speed;
    [SerializeField] public float sprintModifier;
    [SerializeField] public float jumpForce;
    public int maxHealth;
    public Camera normalCam;
    public GameObject cameraParent;
    public Transform weaponParent;
    public Transform groundDetector;
    public LayerMask ground;

    private Transform uiHealthBar;
    private Text uiAmmo;

    private Rigidbody rig;

    private Vector3 targetWeaponBobPosition;
    private Vector3 weaponParentOrigin;

    private float movementCounter;
    private float idleCounter;

    private float baseFOV;
    private float sprintFOVModifier = 1.25f;

    public int currentHealth;

    private Manager manager;
    private Weapon weapon;

    [SerializeField] KeyCode shiftKey = KeyCode.LeftShift;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;

    #endregion

    #region MonoBehaviorCallbacks
    void Start()
    {
        weapon = GetComponent<Weapon>();
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        currentHealth = maxHealth;

        cameraParent.SetActive(photonView.IsMine);
        if(!photonView.IsMine) gameObject.layer = 12;

        baseFOV = normalCam.fieldOfView;
        rig = GetComponent<Rigidbody>();
        if(Camera.main)
            Camera.main.enabled = false;
        weaponParentOrigin = weaponParent.localPosition;

        if (photonView.IsMine)
        {
            uiHealthBar = GameObject.Find("HUD/Health/Bar").transform;
            uiAmmo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            RefreshHealthBar();
        }
    }

    private void Update() 
    {
        if(!photonView.IsMine) return;
        
        // Input
        float horizontalMovement = Input.GetAxisRaw("Horizontal");
        float verticalMovement = Input.GetAxisRaw("Vertical");

        // Controls
        bool sprint = Input.GetKey(shiftKey);
        bool jump = Input.GetKey(jumpKey);

        // States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.3f, ground);
        bool isJumping = Input.GetKeyDown(jumpKey) && isGrounded;
        bool isSprinting = sprint && verticalMovement > 0; //&& !isJumping && isGrounded;
        
        // Jumping
        if (isJumping)
        {
            rig.AddForce(Vector3.up * jumpForce);
        }

        // Testing the damage
        if(Input.GetKeyDown(KeyCode.U)) 
        {
            TakeDamage(40);
            RefreshHealthBar(); 
        }

        // HeadBob
        if (isGrounded) // When not in the air
        {
            if(horizontalMovement == 0 && verticalMovement == 0) 
            { 
                HeadBob(idleCounter, 0.02f, 0.02f); 
                idleCounter += Time.deltaTime; 
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
            }
            else if (!isSprinting)  
            { 
                HeadBob(movementCounter, 0.03f, 0.03f); 
                movementCounter += Time.deltaTime * 3f; 
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
            }
            else
            {
                HeadBob(movementCounter, 0.09f, 0.05f); 
                movementCounter += Time.deltaTime * 7f; 
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
            }
        }
        else 
        {
            // When in the air
            HeadBob(idleCounter, 0.005f, 0.005f); 
            idleCounter += Time.deltaTime; 
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
        }

        // UI Refreshes
        RefreshHealthBar();
        weapon.RefreshAmmo(uiAmmo);
    }

    void FixedUpdate()
    {
        if(!photonView.IsMine) return;

        // Input
        float horizontalMovement = Input.GetAxisRaw("Horizontal");
        float verticalMovement = Input.GetAxisRaw("Vertical");

        // Controls
        bool sprint = Input.GetKey(shiftKey);
        bool jump = Input.GetKey(jumpKey);


        // States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.3f, ground);
        bool isJumping = Input.GetKeyDown(jumpKey) && isGrounded;
        bool isSprinting = sprint && verticalMovement > 0; // && !isJumping && isGrounded;

        // Movement
        Vector3 direction = new Vector3(horizontalMovement, 0, verticalMovement);
        direction.Normalize();

        float adjustedSpeed = speed;
        if (isSprinting)
        {
            adjustedSpeed += sprintModifier;
        }

        Vector3 targetVelocity = transform.TransformDirection(direction) * adjustedSpeed * Time.deltaTime;
        targetVelocity.y = rig.velocity.y;
        rig.velocity = targetVelocity;

        // FOV
        if (isSprinting) {normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);}
        else {normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);}
    }
    #endregion

    #region Private Methods
    void HeadBob(float p_z, float p_xIntensity, float p_yIntensity)
    {
        targetWeaponBobPosition = weaponParentOrigin + new Vector3 (Mathf.Cos(p_z) * p_xIntensity, Mathf.Sin(p_z * 2) * p_yIntensity, 0);
    }

    private void RefreshHealthBar()
    {
        float t_healthratio = (float) currentHealth / (float) maxHealth;
        uiHealthBar.localScale = Vector3.Lerp(uiHealthBar.localScale, new Vector3(t_healthratio, 1, 1), Time.deltaTime * 10f);
    }

    #endregion

    #region Public Methods

    public void TakeDamage(int p_damage)
    {
        if(photonView.IsMine) 
        {
            currentHealth -= p_damage;
            RefreshHealthBar();

            if(currentHealth <= 0)
            {
                manager.Spawn();
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    #endregion
}

