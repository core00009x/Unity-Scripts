using UnityEngine;
using System.Collections;

public class AdvancedKnife : MonoBehaviour
{
    public GameObject player;
    public float throwForce = 25f;
    public float damage = 100f;
    public float recallSpeed = 50f;
    public float maxDistanceOfAttack = 2f;
    public float maxDistanceOfRecall = 10f;
    public bool isHeld = true;
    public GameObject knifePrefab;
    public Transform throwOrigin;
    public AudioClip slashSound, hitSound, recallSound;
    public ParticleSystem hitEffectBlood, hitEffectMetal;
    public LineRenderer returnLine;

    private Rigidbody rb;
    private Collider col;
    private AudioSource audioSource;
    private bool isReturning = false;
    private Vector3 recallTarget;
    private GameObject originalKnife;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (isHeld) rb.isKinematic = true;
    }

    void Update()
    {
        if (isHeld && Input.GetKeyDown(KeyCode.Mouse0)) MeleeAttack();
        else if (isHeld && Input.GetKeyDown(KeyCode.Mouse1)) ThrowKnife();
        else if (!isHeld && Input.GetKeyDown(KeyCode.R)) StartCoroutine(RecallKnife());
    }

    void MeleeAttack()
    {
        //audioSource.PlayOneShot(slashSound);
        RaycastHit hit;
        if (Physics.Raycast(throwOrigin.position, throwOrigin.forward, out hit, maxDistanceOfAttack))
        {
            var enemy = hit.collider.GetComponentInParent<Life>();
            if (enemy != null)
            {
                enemy.GetDamage(damage);
                //audioSource.PlayOneShot(hitSound);
                Instantiate(hitEffectBlood, hit.point, Quaternion.identity);
            }
            else
            {
                Instantiate(hitEffectMetal, hit.point, Quaternion.identity);
            }
        }
    }

    void ThrowKnife()
    {
        isHeld = false;
        gameObject.SetActive(false); // Esconde a faca original

        GameObject thrownKnife = Instantiate(knifePrefab, throwOrigin.position, throwOrigin.rotation);
        thrownKnife.SetActive(true); // Garante que está ativa
        thrownKnife.transform.parent = null; // Desvincula do player
        thrownKnife.AddComponent<LineRenderer>();
        thrownKnife.AddComponent<AudioSource>();
        thrownKnife.AddComponent<AdvancedKnife>(); // Adiciona o script de faca avançada

        AdvancedKnife knifeScript = thrownKnife.GetComponent<AdvancedKnife>();
        if (knifeScript != null)
        {
            knifeScript.originalKnife = this.gameObject; // Referência à faca original
            knifeScript.isHeld = false;
            knifeScript.player = this.player; // Passa referência do player
            knifeScript.rb = thrownKnife.GetComponent<Rigidbody>(); // Inicializa o Rigidbody
            knifeScript.damage = this.damage; // Passa o dano
            knifeScript.hitEffectBlood = this.hitEffectBlood;
            knifeScript.hitEffectMetal = this.hitEffectMetal;
            knifeScript.returnLine = thrownKnife.GetComponent<LineRenderer>();
            knifeScript.audioSource = thrownKnife.GetComponent<AudioSource>();
        }

        thrownKnife.GetComponent<Rigidbody>().isKinematic = false;
        thrownKnife.GetComponent<Rigidbody>().AddForce(throwOrigin.forward * throwForce, ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isHeld)
        {
            rb.isKinematic = true;
            transform.parent = collision.transform;

            if (collision.gameObject.GetComponent<Life>() != null)
            {
                var enemy = collision.gameObject.GetComponent<Life>();
                if (enemy != null)
                {
                    enemy.GetDamage(damage);
                    Instantiate(hitEffectBlood, transform.position, Quaternion.identity);
                }
            }
            else
            {
                Instantiate(hitEffectMetal, transform.position, Quaternion.identity);
            }
        }
    }

    IEnumerator RecallKnife()
    {
        isReturning = true;
        //audioSource.PlayOneShot(recallSound);
        returnLine.enabled = true;

        while (Vector3.Distance(transform.position, player.transform.position) > 1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.transform.position, recallSpeed * Time.deltaTime);
            returnLine.SetPosition(0, transform.position);
            returnLine.SetPosition(1, player.transform.position);
            yield return null;
        }

        // Após chegar no player
        isHeld = true;
        isReturning = false;
        rb.isKinematic = true;
        returnLine.enabled = false;

        // Reativa a faca original na mão do player
        if (originalKnife != this.gameObject)
        {
            var playerKnife = originalKnife.GetComponentInChildren<AdvancedKnife>();
            if (playerKnife != null && playerKnife != this)
            {
                playerKnife.gameObject.SetActive(true);
                playerKnife.isHeld = true; // <-- Corrige para poder atacar/lançar novamente
            }
        }

        Destroy(gameObject); // Destroi a faca lançada
    }
}
