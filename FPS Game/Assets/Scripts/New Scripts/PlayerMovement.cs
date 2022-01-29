using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Variables
    public float speed;
    public float sprintModifier;
    public float jumpForce;
    public float lengthOfSlide;
    public float slideModifier;
    public float crouchModifier;
    public float slideCooldown;
    public int maxHealth;
    public Camera normalCam;
    public GameObject cameraParent;
    public Transform weaponParent;
    public Transform groundDetector;
    public LayerMask ground;

    public float slideAmount;
    public float crouchAmount;
    public GameObject standingCollider;
    public GameObject crouchingCollider;
    public GameObject standingMesh;
    public GameObject crouchingMesh;
    public GameObject glasses;

    private Transform uiHealthBar;
    private Text uiAmmo;

    private Rigidbody rig;

    private Vector3 targetWeaponBobPosition;
    private Vector3 weaponParentOrigin;
    private Vector3 weaponParentCurrentPosition;

    private float movementCounter;
    private float idleCounter;

    private float baseFOV;
    private float sprintFOVModifier = 1.25f;
    private Vector3 origin;

    private int currentHealth;

    private Manager manager;
    private Weapon weapon;

    private bool crouched;

    private bool sliding;
    private float slideTime;
    private Vector3 slideDirection;
    private float currentCooldown;

    private float aimAngle;

    [SerializeField] ParticleSystem speedlines = null;

    [SerializeField] KeyCode shiftKey = KeyCode.LeftShift;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode crouchKey = KeyCode.LeftControl;

    #endregion

    #region Photon Callbacks

    public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message) 
    {
        if (p_stream.IsWriting)
        {
            p_stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
        }
        else
        {
            aimAngle = (int)p_stream.ReceiveNext() / 100f;
        }
    }

    #endregion

    #region MonoBehaviorCallbacks
    void Start()
    {
        glasses.SetActive(false);
        weapon = GetComponent<Weapon>();
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        currentHealth = maxHealth;

        cameraParent.SetActive(photonView.IsMine);
        if(!photonView.IsMine) 
        {
            gameObject.layer = 12;
            standingCollider.gameObject.layer = 12;
            crouchingCollider.gameObject.layer = 12;
        }

        baseFOV = normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;

        rig = GetComponent<Rigidbody>();
        if(Camera.main)
            Camera.main.enabled = false;
        
        weaponParentOrigin = weaponParent.localPosition;
        weaponParentCurrentPosition = weaponParentOrigin;

        if (photonView.IsMine)
        {
            uiHealthBar = GameObject.Find("HUD/Health/Bar").transform;
            uiAmmo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            RefreshHealthBar();
        }
    }

    private void Update() 
    {
        if(!photonView.IsMine) // so other players can see you move gun up and down
        {
            RefreshMultiplayerState();
            return;
        }
        
        // Input
        float horizontalMovement = Input.GetAxisRaw("Horizontal");
        float verticalMovement = Input.GetAxisRaw("Vertical");

        // Controls
        bool sprint = Input.GetKey(shiftKey);
        bool jump = Input.GetKeyDown(jumpKey);
        bool crouch = Input.GetKeyDown(crouchKey);
        bool pause = Input.GetKeyDown(KeyCode.Escape);

        // States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.3f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && verticalMovement > 0; //&& !isJumping && isGrounded;
        bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;

        // Crouching
        if (isCrouching)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }
        
        // Jumping
        if (isJumping)
        {
            if(crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
            rig.AddForce(Vector3.up * jumpForce);
        }

        // Testing the damage
        if(Input.GetKeyDown(KeyCode.U)) 
        {
            TakeDamage(40);
            RefreshHealthBar(); 
        }

        // HeadBob
        if (isGrounded && !sliding) // When not in the air
        {
            if(horizontalMovement == 0 && verticalMovement == 0) 
            { 
                //idle
                HeadBob(idleCounter, 0.02f, 0.02f); 
                idleCounter += Time.deltaTime; 
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
            }
            else if (!isSprinting && !crouched)  
            { 
                //walking
                HeadBob(movementCounter, 0.03f, 0.03f); 
                movementCounter += Time.deltaTime * 3f; 
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
            }
            else if (crouched)
            {   
                //crouched
                HeadBob(movementCounter, 0.02f, 0.02f); 
                movementCounter += Time.deltaTime * 1.75f; 
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
            }
            else
            {
                //sprinting
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
        bool slide = Input.GetKey(crouchKey);


        // States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.3f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && verticalMovement > 0; // && !isJumping && isGrounded;
        bool isSliding = isSprinting && slide && isGrounded && !sliding;

        // Movement
        Vector3 direction = Vector3.zero;
        float adjustedSpeed = speed;

        if (!sliding)
        {
            direction = new Vector3(horizontalMovement, 0, verticalMovement);
            direction.Normalize();
            direction = transform.TransformDirection(direction);

            if (isSprinting) 
            {
                adjustedSpeed *= sprintModifier;
                if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
                adjustedSpeed *= sprintModifier;
            }
            else if (crouched)
            {
                adjustedSpeed *= crouchModifier;
            }
        }
        else
        {
            direction = slideDirection;
            adjustedSpeed *= slideModifier;
            slideTime -= Time.deltaTime;
            if (slideTime <= 0) 
            {
                sliding = false;
                weaponParentCurrentPosition -= Vector3.down * (slideAmount - crouchAmount);
            }
        }

        // Actual movement
        Vector3 targetVelocity = direction * adjustedSpeed * Time.deltaTime;
        targetVelocity.y = rig.velocity.y;
        rig.velocity = targetVelocity;

        // Sliding
        if(currentCooldown > 0) currentCooldown -= Time.deltaTime;
        if (isSliding && currentCooldown <= 0)
        {
            speedlines.Play(); // Speedlines particle effect

            sliding = true;
            slideDirection = direction;
            slideTime = lengthOfSlide;
            currentCooldown = slideCooldown; //cooldown
            //adjust camera
            weaponParentCurrentPosition += Vector3.down * (slideAmount - crouchAmount);
            if (!crouched) photonView.RPC("SetCrouch", RpcTarget.All, true);
            

        }

        // FOV
        if (sliding) 
        { 
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8f);
            normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime * 10f);
        }
        else 
        { 
            if (isSprinting) {normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);}
            else {normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);}

            if (crouched) normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 6f);
            else normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f);
        }

        
    }
    #endregion

    #region Private Methods

    void RefreshMultiplayerState()
    {
        float cacheEulY = weaponParent.localEulerAngles.y;

        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

        Vector3 finalRotation = weaponParent.localEulerAngles;
        finalRotation.y = cacheEulY;

        weaponParent.localEulerAngles = finalRotation;
    }

    void HeadBob(float p_z, float p_xIntensity, float p_yIntensity)
    {
        targetWeaponBobPosition = weaponParentCurrentPosition + new Vector3 (Mathf.Cos(p_z) * p_xIntensity, Mathf.Sin(p_z * 2) * p_yIntensity, 0);
    }

    private void RefreshHealthBar()
    {
        float t_healthratio = (float) currentHealth / (float) maxHealth;
        uiHealthBar.localScale = Vector3.Lerp(uiHealthBar.localScale, new Vector3(t_healthratio, 1, 1), Time.deltaTime * 10f);
    }

    [PunRPC]
    private void SetCrouch(bool p_state)
    {
        if (crouched == p_state) return;

        crouched = p_state;

        if (crouched)
        {
            standingCollider.SetActive(false);
            crouchingCollider.SetActive(true);
            if(!photonView.IsMine) 
            {
                standingMesh.SetActive(false);
                crouchingMesh.SetActive(true);
                glasses.SetActive(false); // glasses don't show when crouching
            } 
            else
            {
                standingMesh.SetActive(false);
                crouchingMesh.SetActive(false);
                glasses.SetActive(false);
            }
            weaponParentCurrentPosition += Vector3.down * crouchAmount;
            // different way
            //weaponParentCurrentPosition -= new Vector3(0, crouchAmount, 0);
        }
        else
        {
            standingCollider.SetActive(true);
            crouchingCollider.SetActive(false);
            if(!photonView.IsMine) 
            {
                standingMesh.SetActive(true);
                crouchingMesh.SetActive(false);
                glasses.SetActive(true);
            } 
            else
            {
                standingMesh.SetActive(true);
                crouchingMesh.SetActive(false);
                glasses.SetActive(false);
            }
            weaponParentCurrentPosition -= Vector3.down * crouchAmount;
            // diferent way
            //weaponParentCurrentPosition += new Vector3(0, crouchAmount, 0);
        }
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

