using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

public class Weapon : MonoBehaviourPunCallbacks
{
    #region Variables
    public Gun1[] loadout;
    [SerializeField] public Transform weaponParent;
    public KeyCode loadoutKey = KeyCode.Alpha1; // 1 key
    public GameObject bulletHolePrefab;
    public LayerMask canBeShot;
    public KeyCode reloadKey;

    private float currentCooldown;
    private int currentIndex;
    private GameObject currentWeapon;
    private int currentHealth;

    private bool isReloading = false;


    #endregion

    #region MonoBehavior Callbacks
    void Start()
    {
        foreach(Gun1 a in loadout) a.Initialize();
    }

    void Update()
    {   
        if (photonView.IsMine && Input.GetKeyDown(loadoutKey)) 
        { 
            photonView.RPC("Equip", RpcTarget.All, 0);
            
        
        }
        
        if (currentWeapon != null)
        {
            if(photonView.IsMine)
            {
                Aim(Input.GetMouseButton(1));
                
                if(Input.GetMouseButtonDown(0) && currentCooldown <= 0)
                {
                    if (loadout[currentIndex].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);
                    else StartCoroutine(Reload(loadout[currentIndex].reloadTime)); 
                } 

                // reload
                if (Input.GetKeyDown(reloadKey)) { StartCoroutine(Reload(loadout[currentIndex].reloadTime)); }

                // cooldown
                if(currentCooldown > 0) currentCooldown -= Time.deltaTime;
            }

            // weapon position elasticity (so gun goes back to normal after kickback)
            // if(currentWeapon != null)
            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
        }

        
    }
    #endregion

    #region Private Methods

    IEnumerator Reload(float p_wait)
    {
        isReloading = true;
        currentWeapon.SetActive(false);

        yield return new WaitForSeconds(p_wait);

        loadout[currentIndex].Reload();
        currentWeapon.SetActive(true);
        isReloading = false;
    }
    
    [PunRPC]
    void Equip(int p_ind)
    {
        if (currentWeapon != null) 
        { 
            if (isReloading) StopCoroutine("Reload");
            Destroy(currentWeapon);
        }

        currentIndex = p_ind;


        GameObject newWeapon = Instantiate (loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;
        newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

        currentWeapon = newWeapon;
    }

    void Aim(bool isAiming) {
        {
            Transform anchor = currentWeapon.transform.Find("Anchor");
            Transform stateADS = currentWeapon.transform.Find("States/ADS");
            Transform stateHip = currentWeapon.transform.Find("States/Hip");

            if (isAiming)
            {
                // ads
                anchor.position = Vector3.Lerp(anchor.position, stateADS.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }
            else
            {
                // hip
                anchor.position = Vector3.Lerp(anchor.position, stateHip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }
        }
    }

    [PunRPC]
    void Shoot()
    {
        Transform spawn = transform.Find("Cameras/Normal Camera");

        // bloom
        Vector3 bloom = spawn.position + spawn.forward * 1000f;
        bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) *spawn.up;
        bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) *spawn.right;
        bloom -= spawn.position;
        bloom.Normalize();

        // cooldown
        currentCooldown = loadout[currentIndex].fireRate;
        
        
        // raycast
        
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(spawn.position, bloom, out hit, 1000f, canBeShot))
        {
            GameObject newHole = Instantiate(bulletHolePrefab, hit.point + hit.normal * 0.001f, Quaternion.identity) as GameObject;
            newHole.transform.LookAt(hit.point + hit.normal);
            if(hit.collider.gameObject.layer == 12)
            {
                Destroy(newHole, 0.1f);
            }
            else
            {
                Destroy(newHole, 5f);
            }

            if(photonView.IsMine)
            {
                //shooting other player on network
                if(hit.collider.gameObject.layer == 12)
                {
                    hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage);
                }
            }
        }

        //gun effects(recoil and kickback)
        if (currentWeapon != null)
        {
            currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
            currentWeapon.transform.position -=currentWeapon.transform.forward * loadout[currentIndex].kickback;
        }
    }

    [PunRPC]
    private void TakeDamage(int p_damage)
    {
        GetComponent<PlayerMovement>().TakeDamage(p_damage);
    }

    #endregion

    #region Public Methods

    public void RefreshAmmo(Text p_text)
    {
        int t_clip = loadout[currentIndex].GetClip();
        int t_stash = loadout[currentIndex].GetStash();

        p_text.text = t_clip.ToString("D2") + " / " + t_stash.ToString("D4"); //Displays current ammo on ui
    }

    #endregion
}
