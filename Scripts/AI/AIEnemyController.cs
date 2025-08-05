using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// IA Inimiga Ultra Final para Unity 6
/// FSM + Percepção Visual + Detecção de Som via Eventos + Memória + Inteligência Adaptativa + Emboscada
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class AIEnemyController : MonoBehaviour
{
    public enum AIState { Idle, Patrol, Chase, Search, Attack, Flee, Investigate, Ambush, Dead }
    public AIState currentState = AIState.Patrol;

    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;

    [Header("Referências")]
    public Transform eyes;
    public Transform[] patrolPoints;
    private int patrolIndex;

    [Header("Visão")]
    public float viewDistance = 30f;
    [Range(0, 360)] public float fieldOfView = 120f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Audição")]
    public float hearingRadius = 15f;

    [Header("Combate")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Memória & Inteligência")]
    public float memoryDuration = 10f;
    private float timeSinceLastSeen;
    private Vector3 lastKnownPlayerPosition;
    private Transform player;
    private Dictionary<string, int> behaviorMemory = new();
    private float thinkCooldown = 1f;
    private float lastThinkTime;

    [Header("Reações Especiais")]
    public bool enableAmbush = true;
    private bool isHiding = false;
    private float hideTimer = 0f;

    [Header("Debug")]
    public bool showGizmos = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (patrolPoints.Length > 0) agent.SetDestination(patrolPoints[0].position);
        AIGroupManager.Instance?.RegisterEnemy(this);
    }

    void Update()
    {
        if (currentState == AIState.Dead) return;

        PerceptionUpdate();
        FSMUpdate();
        IntelligenceUpdate();
    }

    // ------------------------- FSM -------------------------

    void FSMUpdate()
    {
        switch (currentState)
        {
            case AIState.Idle: Idle(); break;
            case AIState.Patrol: Patrol(); break;
            case AIState.Chase: Chase(); break;
            case AIState.Search: Search(); break;
            case AIState.Attack: Attack(); break;
            case AIState.Flee: Flee(); break;
            case AIState.Investigate: Investigate(); break;
            case AIState.Ambush: Ambush(); break;
            case AIState.Dead: break;
        }
    }

    // ------------------------- COMPORTAMENTOS -------------------------

    void Idle()
    {
        agent.SetDestination(transform.position);
        animator?.SetBool("Idle", true);
        animator?.SetBool("Walk", false);
        animator?.SetBool("Attack 1", false);
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
        animator?.SetBool("Walk", true);
        animator?.SetBool("Idle", false);
        animator?.SetBool("Attack 1", false);
    }

    void Chase()
    {
        if (player)
            agent.SetDestination(player.position);
        else
            currentState = AIState.Search;

        animator?.SetBool("Walk", true);
        animator?.SetBool("Idle", false);
        animator?.SetBool("Attack 1", false);
    }

    void Search()
    {
        agent.SetDestination(lastKnownPlayerPosition);
        if (agent.remainingDistance < 1f)
            currentState = AIState.Patrol;

        animator?.SetBool("Walk", true);
        animator?.SetBool("Idle", false);
        animator?.SetBool("Attack 1", false);
    }

    void Attack()
    {
        if (player == null)
        {
            currentState = AIState.Search;
            return;
        }

        transform.LookAt(player);
        agent.SetDestination(transform.position);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            Debug.Log("AI Attack!");

            animator?.SetBool("Attack 1", true);
            animator?.SetBool("Walk", false);
            animator?.SetBool("Idle", false);
            audioSource?.Play();
            player.GetComponent<Life>()?.GetDamage(10f);

            LogBehavior("close_attack");
        }

        if (Vector3.Distance(transform.position, player.position) > attackRange)
            currentState = AIState.Chase;
    }

    void Flee()
    {
        Vector3 dir = (transform.position - lastKnownPlayerPosition).normalized;
        Vector3 target = transform.position + dir * 12f;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        animator?.SetBool("Flee", true);
    }

    void Investigate()
    {
        agent.SetDestination(lastKnownPlayerPosition);
        if (agent.remainingDistance < 1f)
        {
            currentState = AIState.Patrol;
        }
        animator?.SetBool("Investigate", true);
    }

    void Ambush()
    {
        if (!isHiding)
        {
            hideTimer = Random.Range(3f, 7f);
            isHiding = true;
        }

        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0)
        {
            isHiding = false;
            currentState = AIState.Chase;
        }
        animator?.SetBool("Ambush", true);
    }

    // ------------------------- MORTE SIMPLES -------------------------

    public void Die()
    {
        if (currentState == AIState.Dead) return;

        currentState = AIState.Dead;
        agent.enabled = false;
        animator.SetBool("Death", true);
        // Aqui você pode adicionar efeitos visuais ou sons de morte simples
        Destroy(gameObject, 10f); // opcional: remove o inimigo após 10s
    }

    // ------------------------- PERCEPÇÃO VISUAL -------------------------

    void PerceptionUpdate()
    {
        timeSinceLastSeen += Time.deltaTime;

        Collider[] hits = Physics.OverlapSphere(transform.position, viewDistance, playerLayer);
        foreach (var hit in hits)
        {
            Vector3 dirToTarget = (hit.transform.position - eyes.position).normalized;
            float angle = Vector3.Angle(eyes.forward, dirToTarget);
            float dist = Vector3.Distance(eyes.position, hit.transform.position);

            if (angle < fieldOfView / 2 && !Physics.Raycast(eyes.position, dirToTarget, dist, obstacleLayer))
            {
                player = hit.transform;
                lastKnownPlayerPosition = player.position;
                timeSinceLastSeen = 0f;

                currentState = (Vector3.Distance(transform.position, player.position) <= attackRange)
                    ? AIState.Attack
                    : AIState.Chase;

                AIGroupManager.Instance?.BroadcastPlayerSpotted(player.position, this);

                return;
            }
        }

        if (timeSinceLastSeen > memoryDuration)
            player = null;
    }

    // ------------------------- PERCEPÇÃO SONORA -------------------------

    public void HeardNoise(Vector3 position, float volume)
    {
        if (player != null) return;
        if (Vector3.Distance(transform.position, position) <= hearingRadius * volume)
        {
            lastKnownPlayerPosition = position;
            currentState = AIState.Investigate;
            Debug.Log("AI heard noise!");
            AIGroupManager.Instance?.BroadcastNoiseHeard(position, this);
        }
    }

    // ------------------------- INTELIGÊNCIA ADAPTATIVA -------------------------

    void LogBehavior(string pattern)
    {
        if (!behaviorMemory.ContainsKey(pattern)) behaviorMemory[pattern] = 0;
        behaviorMemory[pattern]++;
    }

    void IntelligenceUpdate()
    {
        if (Time.time - lastThinkTime < thinkCooldown) return;
        lastThinkTime = Time.time;

        if (behaviorMemory.ContainsKey("close_attack") && behaviorMemory["close_attack"] >= 3)
        {
            if (enableAmbush)
            {
                Debug.Log("AI emboscando por padrão de combate próximo.");
                currentState = AIState.Ambush;
                behaviorMemory["close_attack"] = 0;
            }
            else
            {
                Debug.Log("AI recuando por padrão de combate próximo.");
                currentState = AIState.Flee;
                behaviorMemory["close_attack"] = 0;
            }
        }
    }

    // ------------------------- GIZMOS DEBUG -------------------------

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        if (eyes != null)
        {
            Vector3 left = Quaternion.Euler(0, -fieldOfView / 2, 0) * eyes.forward;
            Vector3 right = Quaternion.Euler(0, fieldOfView / 2, 0) * eyes.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(eyes.position, eyes.position + left * viewDistance);
            Gizmos.DrawLine(eyes.position, eyes.position + right * viewDistance);
        }
    }
    
    void OnDestroy()
    {
        AIGroupManager.Instance?.UnregisterEnemy(this);
    }
    
    public void OnAllySpottedPlayer(Vector3 position)
    {
        if (player == null)
        {
            lastKnownPlayerPosition = position;
            currentState = AIState.Chase;
            Debug.Log($"{gameObject.name} recebeu alerta visual de aliado!");
        }
    }

    public void OnAllyHeardNoise(Vector3 noisePos)
    {
        if (player == null)
        {
            lastKnownPlayerPosition = noisePos;
            currentState = AIState.Investigate;
            Debug.Log($"{gameObject.name} ouviu som de aliado!");
        }
    }
}
