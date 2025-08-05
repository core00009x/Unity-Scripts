using System;
using UnityEngine;

public class Life : MonoBehaviour
{
    [Header("Life")]
    [SerializeField]
    public float life;

    [Header("Atributes")] 
    [SerializeField] 
    public string deadAnimationName;

    public float deadAnimationTransitionTime;
    public float destroyAfterAnimation;
    public float destroyWithoutAnimation;
    public GameObject deathEffect;
    public float deathEffectDestroyTime;
    
    private bool isDead = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (life <= 0f && !isDead)
        {
            isDead = true;
            if (this.gameObject.GetComponent<Animator>() != null)
            {
                this.gameObject.GetComponent<Animator>().CrossFade(deadAnimationName, deadAnimationTransitionTime);
                Destroy(this.gameObject, destroyAfterAnimation);
            }
            else if (deathEffect != null)
            {
                GameObject deathEffectInstance = Instantiate(deathEffect, this.transform.position, Quaternion.identity);
                Destroy(deathEffectInstance, deathEffectDestroyTime);
            }
            else if (gameObject.GetComponent<Explosion>() != null)
            {
                gameObject.GetComponent<Explosion>().Explode();
            }
            else
            {
                Destroy(this.gameObject, destroyWithoutAnimation);
            }
        }
    }

    public void GetLife(float amount)
    {
        life += amount;
    }

    public void GetDamage(float amount)
    {
        life -= amount;
    }
}
