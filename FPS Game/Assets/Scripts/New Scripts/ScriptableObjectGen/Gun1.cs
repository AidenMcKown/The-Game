using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
public class Gun1 : ScriptableObject
{
    public string gunName;
    public int damage;
    public int ammo;
    public int clipSize;
    public float fireRate;
    public float bloom;
    public float recoil;
    public float kickback;
    public float aimSpeed;
    public GameObject prefab;
    public float reloadTime;

    private int stash; //current ammo
    private int clip; //current clip

    public void Initialize()
    {
        stash = ammo;
        clip = clipSize;
    }

    public bool FireBullet()
    {
        if (clip > 0)
        {
            clip -= 1;
            return true;
        }
        else return false;
    }

    public void Reload()
    {
        stash += clip; 
        clip = Mathf.Min(clipSize, stash);
        stash -= clip;
    }

    public int GetStash() { return stash; }
    public int GetClip() { return clip; }

   
}
