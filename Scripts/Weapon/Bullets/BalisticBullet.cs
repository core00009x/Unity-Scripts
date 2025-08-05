using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class BallisticBullet : MonoBehaviour
{
    [Header("Motion Settings")]
    public float initialSpeed = 80f;
    public float gravity = -9.81f;
    public float airDrag = 0.05f;
    public float maxLifetime = 10f;

    [Header("Ricochet & Penetration")]
    public int maxRicochets = 2;
    public float ricochetThreshold = 45f;
    public float ricochetDamping = 0.7f;
    public float penetrationPower = 100f;

    [Header("Damage Settings")]
    public float baseDamage = 50f;
    public AnimationCurve damageFalloffCurve; // Customize in Inspector

    [Header("FX")]
    public TrailRenderer tracer;
    public GameObject impactWoodFX;
    public GameObject impactMetalFX;
    public GameObject impactConcreteFX;
    public GameObject impactDefaultFX;
    
    [Header("Audio FX")]
    public AudioClip impactWoodSound;
    public AudioClip impactMetalSound;
    public AudioClip impactConcreteSound;
    public AudioClip impactGlassSound;
    public AudioClip impactDefaultSound;

    public AudioSource audioSourcePrefab;
    private Vector3 velocity;
    private float lifeTimer;
    private int ricochetCount = 0;
    private float totalDistance = 0f;

    void Start()
    {
        velocity = transform.forward * initialSpeed;
    }

    void Update()
    {
        velocity.y += gravity * Time.deltaTime;
        velocity -= velocity * airDrag * Time.deltaTime;

        Vector3 displacement = velocity * Time.deltaTime;
        transform.position += displacement;
        totalDistance += displacement.magnitude;

        transform.rotation = Quaternion.LookRotation(velocity);

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime) Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = collision.contacts[0].normal;
        float impactAngle = Vector3.Angle(-velocity.normalized, hitNormal);

        SpawnSurfaceFX(collision.collider.tag, hitPoint);

        string tag = collision.collider.tag;

        SpawnSurfaceFX(tag, hitPoint);
        PlayImpactSound(tag, hitPoint);

        // Ricochet Logic
        if (impactAngle < ricochetThreshold && ricochetCount < maxRicochets)
        {
            velocity = Vector3.Reflect(velocity, hitNormal) * ricochetDamping;
            ricochetCount++;
            return;
        }

        // Penetration Logic
        if (TryPenetrate(collision.collider))
        {
            penetrationPower -= GetMaterialResistance(collision.collider);
            ApplyDamage(collision.collider);
            return;
        }

        ApplyDamage(collision.collider);
        Destroy(gameObject);
    }

    void ApplyDamage(Collider target)
    {
        float distanceFactor = damageFalloffCurve.Evaluate(totalDistance / 50f);
        float finalDamage = baseDamage * distanceFactor;

        // Assuming target has a Health component
        var health = target.GetComponent<Life>();
        if (health != null)
            health.GetDamage(finalDamage);
    }

    void SpawnSurfaceFX(string tag, Vector3 point)
    {
        GameObject fx = impactDefaultFX;
        if (tag == "Wood") fx = impactWoodFX;
        else if (tag == "Metal") fx = impactMetalFX;
        else if (tag == "Concrete") fx = impactConcreteFX;

        if (fx != null)
            Instantiate(fx, point, Quaternion.identity);
    }
    
    void PlayImpactSound(string tag, Vector3 point)
    {
        AudioClip clip = impactDefaultSound;

        if (tag == "Wood") clip = impactWoodSound;
        else if (tag == "Metal") clip = impactMetalSound;
        else if (tag == "Concrete") clip = impactConcreteSound;
        else if (tag == "Glass") clip = impactGlassSound;

        if (clip != null && audioSourcePrefab != null)
        {
            AudioSource src = Instantiate(audioSourcePrefab, point, Quaternion.identity);
            src.clip = clip;
            src.Play();
            Destroy(src.gameObject, clip.length + 0.1f);
        }
    }

    bool TryPenetrate(Collider col)
    {
        return penetrationPower > GetMaterialResistance(col);
    }

    float GetMaterialResistance(Collider col)
    {
        switch (col.tag)
        {
            case "Wood": return 20f;
            case "Glass": return 10f;
            case "Concrete": return 80f;
            case "Metal": return 120f;
            default: return 50f;
        }
    }

    public void ApplyAmmoType(AmmoType type)
    {
        initialSpeed = type.velocity;
        baseDamage = type.damage;
        penetrationPower = type.penetration;
    }
}
