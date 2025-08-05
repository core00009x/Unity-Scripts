using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class Explosion : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float radius = 10f;
    public float maxDamage = 100f;
    public float minDamage = 10f;
    public float force = 1500f;
    public float upwardsModifier = 0.5f;
    public LayerMask damageLayers;
    public string[] ignoreTags;

    [Header("Visual Effects")]
    public ParticleSystem explosionParticles;
    public GameObject decalPrefab;
    public float decalLifetime = 10f;

    [Header("Audio")]
    public AudioClip explosionSound;
    public float soundVolume = 1f;

    [Header("Camera Shake")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.7f;

    [Header("Events")]
    public UnityEvent OnExplode;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Explode()
    {
        // Play VFX
        if (explosionParticles)
            explosionParticles.Play();

        // Play SFX
        if (explosionSound)
            audioSource.PlayOneShot(explosionSound, soundVolume);

        // Camera Shake (call before destroy)
        CameraShake();

        // Damage & Physics
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, damageLayers);
        foreach (Collider hit in colliders)
        {
            if (ShouldIgnore(hit)) continue;

            // Damage falloff
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            float damage = Mathf.Lerp(maxDamage, minDamage, distance / radius);

            // Apply damage if possible
            var health = hit.GetComponent<Life>();
            if (health != null)
                health.GetDamage(damage);

            // Apply force
            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
                rb.AddExplosionForce(force, transform.position, radius, upwardsModifier, ForceMode.Impulse);

            // Decal
            if (decalPrefab)
                PlaceDecal(hit, hit.ClosestPoint(transform.position));
        }

        // Events (for networking, etc.)
        OnExplode?.Invoke();

        // Destroy explosion object after effects
        Destroy(gameObject, explosionParticles ? explosionParticles.main.duration : 2f);
    }

    bool ShouldIgnore(Collider hit)
    {
        foreach (var tag in ignoreTags)
            if (hit.CompareTag(tag)) return true;
        return false;
    }

    void PlaceDecal(Collider hit, Vector3 position)
    {
        RaycastHit rayHit;
        if (Physics.Raycast(transform.position, position - transform.position, out rayHit, radius))
        {
            GameObject decal = Instantiate(decalPrefab, rayHit.point + rayHit.normal * 0.01f, Quaternion.LookRotation(rayHit.normal));
            Destroy(decal, decalLifetime);
        }
    }

    void CameraShake()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            var shaker = cam.GetComponent<CameraShaker>();
            if (shaker != null)
                shaker.Shake(shakeDuration, shakeMagnitude);
        }
    }

    // For debugging in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}