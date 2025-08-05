using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AdvancedPistol : MonoBehaviour
{
    [Header("References")]
    public Transform muzzleTransform;
    public Transform gunTransform;
    public Camera playerCamera;
    public ParticleSystem muzzleFlash;
    public AudioSource gunshotAudio;
    public Animator animator;
    public GameObject bulletPrefab;

    [Header("Gun Settings")]
    public float fireRate = 0.2f;
    public int magazineSize = 15;
    public float reloadTime = 2f;
    public float swayIntensity = 1.2f;
    public float swaySmooth = 6f;
    public bool canFullAuto = false;

    [Header("ADS Settings")]
    public bool allowADS = true;
    public Vector3 adsPositionOffset;
    public float adsFOV = 40f;
    public float adsSpeed = 8f;

    [Header("Munition System")]
    public AmmoDefinitions ammoDefinitions;
    private Dictionary<string, int> reserveAmmoDict = new Dictionary<string, int>();
    private Dictionary<string, int> maxReserveAmmoDict = new Dictionary<string, int>();
    public string currentAmmoType;
    private int currentAmmo;
    private bool isReloading = false;
    private bool canFire = true;
    private bool isAiming = false;

    private Quaternion defaultRotation;
    private Vector3 defaultGunPosition;
    private float defaultFOV;

    void Start()
    {
        currentAmmo = magazineSize;
        defaultRotation = transform.localRotation;
        defaultGunPosition = gunTransform.localPosition;
        defaultFOV = playerCamera.fieldOfView;
    }

    void Update()
    {
        if (!isReloading)
        {
            HandleInput();
            ApplySway();
            HandleADS();
        }
    }

    void HandleInput()
    {
        if (!canFullAuto)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && canFire)
            {
                Fire();
            }
        }
        else
        {
            if (Mouse.current.leftButton.isPressed && canFire)
            {
                Fire();
            }
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
            StartCoroutine(Reload());

        isAiming = Mouse.current.rightButton.isPressed;
    }

    void HandleADS()
    {
        if (!allowADS) return;

        Vector3 targetPos = isAiming 
            ? defaultGunPosition + adsPositionOffset 
            : defaultGunPosition;

        gunTransform.localPosition = Vector3.Lerp(
            gunTransform.localPosition,
            targetPos,
            adsSpeed * Time.deltaTime
        );

        float targetFOV = isAiming ? adsFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            adsSpeed * Time.deltaTime
        );
    }

    void Fire()
    {
        if (currentAmmo <= 0 || bulletPrefab == null) return;

        currentAmmo--;
        canFire = false;
        Invoke(nameof(ResetFire), fireRate);

        var x = Instantiate(muzzleFlash, muzzleTransform.position, muzzleTransform.rotation);
        Destroy(x.gameObject, 0.2f);

        // Instantiate bullet and apply ammo type if available
        GameObject bulletObj = Instantiate(bulletPrefab, muzzleTransform.position, muzzleTransform.rotation);
        BallisticBullet bullet = bulletObj.GetComponent<BallisticBullet>();
        if (bullet != null && ammoDefinitions != null && !string.IsNullOrEmpty(currentAmmoType))
        {
            // Find the correct AmmoType from definitions
            AmmoType ammoTypeObj = null;
            foreach (var type in ammoDefinitions.ammoTypes)
            {
                if (type.name == currentAmmoType)
                {
                    ammoTypeObj = type;
                    break;
                }
            }
            if (ammoTypeObj != null)
                bullet.ApplyAmmoType(ammoTypeObj);
        }

        this.gameObject.GetComponent<RecoilController>().Fire();
    }

    void ResetFire()
    {
        canFire = true;
    }

    System.Collections.IEnumerator Reload()
    {
        if (isReloading || currentAmmo == magazineSize)
            yield break;

        int availableReserve = 0;
        if (!string.IsNullOrEmpty(currentAmmoType) && reserveAmmoDict.ContainsKey(currentAmmoType))
            availableReserve = reserveAmmoDict[currentAmmoType];

        if (availableReserve <= 0)
            yield break;

        isReloading = true;
        animator?.SetTrigger("Reload");
        yield return new WaitForSeconds(reloadTime);

        int neededAmmo = magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(neededAmmo, availableReserve);

        currentAmmo += ammoToReload;
        reserveAmmoDict[currentAmmoType] -= ammoToReload;

        isReloading = false;
    }

    // Dynamic ammo pickup
    public void AddAmmo(string ammoName, int amount, AmmoType type)
    {
        if (!reserveAmmoDict.ContainsKey(ammoName))
        {
            reserveAmmoDict[ammoName] = 0;
            maxReserveAmmoDict[ammoName] = type.maxCarry;
        }
        reserveAmmoDict[ammoName] += amount;
        reserveAmmoDict[ammoName] = Mathf.Clamp(reserveAmmoDict[ammoName], 0, type.maxCarry);

        currentAmmoType = ammoName;
    }

    void ApplySway()
    {
        float mouseX = Mouse.current.delta.x.ReadValue() * swayIntensity * Time.deltaTime;
        float mouseY = Mouse.current.delta.y.ReadValue() * swayIntensity * Time.deltaTime;

        Quaternion targetRotation = Quaternion.Euler(-mouseY, mouseX, 0) * defaultRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, swaySmooth * Time.deltaTime);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, muzzleTransform.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, muzzleTransform.rotation);
        }
    }
}
