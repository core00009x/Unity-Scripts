using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Grenade : MonoBehaviour
{
    public float delay = 2f;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public float damage = 45;
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    private bool exploded = false;

    void Start()
    {
        Invoke("Explode", delay);
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        //AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider nearby in colliders)
        {
            Rigidbody rb = nearby.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            Life life = nearby.GetComponent<Life>();
            if (life != null)
            {
                float distance = Vector3.Distance(transform.position, nearby.transform.position);
                float adjustedDamage = Mathf.Clamp01(1 - (distance / explosionRadius)) * damage;
                life.GetDamage(adjustedDamage);
            }
        }

        Destroy(gameObject);
    }
}
