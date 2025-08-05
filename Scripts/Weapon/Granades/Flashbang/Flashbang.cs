using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Flashbang : MonoBehaviour
{
    public float delay = 2f;
    public float flashRadius = 10f;
    public GameObject flashEffect;
    public AudioClip bangSound;
    public LayerMask playerLayer;

    void Start()
    {
        Invoke("Detonate", delay);
    }

    void Detonate()
    {
        //Instantiate(flashEffect, transform.position, Quaternion.identity);
        //AudioSource.PlayClipAtPoint(bangSound, transform.position);

        Collider[] targets = Physics.OverlapSphere(transform.position, flashRadius, playerLayer);
        foreach (Collider target in targets)
        {
            if (target.CompareTag("Player"))
            {
                target.GetComponent<PlayerFlashEffect>()?.TriggerFlash(transform.position);
            }
        }

        Destroy(gameObject);
    }
}
