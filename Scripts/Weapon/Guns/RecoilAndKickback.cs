using UnityEngine;

public class RecoilController : MonoBehaviour
{
    [Header("Weapon Recoil Settings")]
    public Transform weaponTransform;
    public Vector3 recoilStrength = new Vector3(-5f, 2f, 1f);
    public AnimationCurve recoilCurveX;
    public AnimationCurve recoilCurveY;
    public AnimationCurve recoilCurveZ;
    public float recoilDuration = 0.2f;
    public float recoilReturnSpeed = 10f;

    [Header("Camera Shake Settings")]
    public Transform mainCamera;
    public float shakeMagnitude = 0.2f;
    public float shakeDuration = 0.1f;

    private Quaternion originalRotation;
    private Vector3 cameraOriginalPos;

    private float recoilTimer = 0f;
    private float shakeTimer = 0f;

    void Start()
    {
        if (weaponTransform != null)
            originalRotation = weaponTransform.localRotation;

        if (mainCamera != null)
            cameraOriginalPos = mainCamera.localPosition;
    }

    void Update()
    {
        Quaternion targetRotation = originalRotation;

        if (recoilTimer > 0f && weaponTransform != null)
        {
            float t = 1f - (recoilTimer / recoilDuration);

            float recoilX = recoilCurveX.Evaluate(t) * recoilStrength.x;
            float recoilY = recoilCurveY.Evaluate(t) * recoilStrength.y;
            float recoilZ = recoilCurveZ.Evaluate(t) * recoilStrength.z;

            Vector3 recoilOffset = new Vector3(recoilX, recoilY, recoilZ);
            targetRotation = Quaternion.Euler(originalRotation.eulerAngles + recoilOffset);

            recoilTimer -= Time.deltaTime;
        }
        
        if (weaponTransform != null)
        {
            weaponTransform.localRotation = Quaternion.Lerp(weaponTransform.localRotation, targetRotation, Time.deltaTime * recoilReturnSpeed);
        }
        
        if (shakeTimer > 0f && mainCamera != null)
        {
            mainCamera.localPosition = cameraOriginalPos + Random.insideUnitSphere * shakeMagnitude;
            shakeTimer -= Time.deltaTime;
        }
        else if (mainCamera != null)
        {
            mainCamera.localPosition = Vector3.Lerp(mainCamera.localPosition, cameraOriginalPos, Time.deltaTime * 15f); // smooth reset
        }
    }

    public void Fire()
    {
        recoilTimer = recoilDuration;
        shakeTimer = shakeDuration;
    }
}