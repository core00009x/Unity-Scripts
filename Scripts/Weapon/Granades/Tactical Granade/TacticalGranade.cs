using UnityEngine;

public class TacticalGrenade : MonoBehaviour {
    public float explosionRadius = 6f;
    public float explosionForce = 900f;
    public GameObject explosionEffect;
    public AudioClip beepSound;
    public AudioClip explosionSound;
    public GameObject clusterPrefab;
    public int clusterCount = 5;
    public float clusterSpread = 2f;
    public Light ledLight;

    private bool stuck = false;
    private bool detonated = false;
    private float beepInterval = 0.5f;
    private float timeToDetonate = 4f;

    void Start() {
        InvokeRepeating("BeepCountdown", 0f, beepInterval);
    }

    void OnCollisionEnter(Collision collision) {
        if (stuck) return;
        stuck = true;
        GetComponent<Rigidbody>().isKinematic = true;
        transform.parent = collision.transform;
    }

    void BeepCountdown() {
        if (detonated) return;
        AudioSource.PlayClipAtPoint(beepSound, transform.position);
        if (ledLight != null) ledLight.enabled = !ledLight.enabled;

        timeToDetonate -= beepInterval;
        if (timeToDetonate <= 0f) {
            Detonate();
        }
    }

    void Detonate() {
        if (detonated) return;
        detonated = true;

        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider obj in colliders) {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        SpawnClusters();
        Destroy(gameObject);
    }

    void SpawnClusters() {
        for (int i = 0; i < clusterCount; i++) {
            Vector3 offset = Random.insideUnitSphere * clusterSpread;
            GameObject cluster = Instantiate(clusterPrefab, transform.position + offset, Quaternion.identity);
            Rigidbody rb = cluster.GetComponent<Rigidbody>();
            rb.AddExplosionForce(200f, transform.position, 3f);
        }
    }
}
