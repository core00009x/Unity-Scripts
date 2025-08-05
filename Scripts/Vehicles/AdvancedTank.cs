using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
public class AdvancedTank : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private Transform turretPivot;
    [SerializeField] private Transform cannonPivot;
    [SerializeField] private Transform[] trackShoes;
    [SerializeField] private ParticleSystem[] smokeEmitters;

    [Header("Movement Settings")]
    [SerializeField] private float enginePower = 850f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private float brakeForce = 2000f;
    [SerializeField] private float suspensionDamping = 4f;
    [SerializeField] private float trackScrollSpeed = 0.15f;

    [Header("Combat System")]
    [SerializeField] private Transform mainGunMuzzle;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float shellVelocity = 85f;
    [SerializeField] private float turretRotationSpeed = 22f;
    [SerializeField] private float cannonElevationSpeed = 10f;
    [SerializeField] private Vector2 cannonElevationLimits = new Vector2(-5f, 20f);

    [Header("Damage System")]
    [SerializeField] private float maxArmor = 1000f;
    [SerializeField] private float[] componentArmor = new float[4]; // 0=Tracks, 1=Turret, 2=Engine, 3=Gun
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private GameObject hitEffect;

    [Header("AI Settings")]
    [SerializeField] private bool isAI = false;
    [SerializeField] private float aiDetectionRange = 100f;
    [SerializeField] private float aiFiringRange = 70f;

    // Private state variables
    private Rigidbody rb;
    private AudioSource engineSound;
    private float currentArmor;
    private float nextFireTime;
    private float currentTurretRotation;
    private float currentCannonElevation;
    private bool isMoving;
    private bool tracksDisabled;
    private bool turretDisabled;
    private bool engineDisabled;
    private bool gunDisabled;

    // AI variables
    private Transform playerTarget;
    private Vector3 patrolDestination;
    private AIState currentAIState = AIState.Patrolling;

    private enum AIState { Patrolling, Engaging, Evading }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.down * 0.5f;
        engineSound = GetComponent<AudioSource>();
        currentArmor = maxArmor;

        if (isAI)
        {
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
            SetNewPatrolDestination();
        }
    }

    void Update()
    {
        UpdateTrackVisuals();
        UpdateEngineSound();
        UpdateSmokeEmissions();

        if (isAI)
        {
            ExecuteAIBehavior();
        }
    }

    void FixedUpdate()
    {
        if (!tracksDisabled && !engineDisabled)
        {
            ApplyTankPhysics();
        }
    }

    #region Movement System
    public void MoveTank(float throttleInput, float steerInput)
    {
        if (tracksDisabled || engineDisabled) return;

        // Convert inputs to physical forces
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        float effectivePower = enginePower * (1f - speedFactor * 0.6f);

        Vector3 moveForce = transform.forward * throttleInput * effectivePower;
        rb.AddForce(moveForce, ForceMode.Force);

        // Apply rotation based on steering
        float rotationTorque = steerInput * rotationSpeed * rb.mass;
        rb.AddTorque(transform.up * rotationTorque, ForceMode.Acceleration);

        // Apply braking when not accelerating
        if (Mathf.Approximately(throttleInput, 0f))
        {
            ApplyBraking();
        }

        isMoving = rb.linearVelocity.magnitude > 0.1f;
    }

    private void ApplyBraking()
    {
        Vector3 brakeForceVector = -rb.linearVelocity.normalized * brakeForce;
        rb.AddForce(brakeForceVector, ForceMode.Force);
    }

    private void ApplyTankPhysics()
    {
        // Simulate track-ground interaction
        RaycastHit hit;
        for (int i = 0; i < trackShoes.Length; i++)
        {
            if (Physics.Raycast(trackShoes[i].position, -transform.up, out hit, 1.5f))
            {
                Vector3 suspensionForce = transform.up * suspensionDamping * rb.mass;
                rb.AddForceAtPosition(suspensionForce, trackShoes[i].position);
            }
        }

        // Limit maximum speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
    #endregion

    #region Combat System
    public void RotateTurret(float rotationInput)
    {
        if (turretDisabled) return;
        currentTurretRotation += rotationInput * turretRotationSpeed * Time.deltaTime;
        turretPivot.localRotation = Quaternion.Euler(0, currentTurretRotation, 0);
    }

    public void ElevateCannon(float elevationInput)
    {
        if (gunDisabled) return;
        currentCannonElevation = Mathf.Clamp(
            currentCannonElevation + elevationInput * cannonElevationSpeed * Time.deltaTime,
            cannonElevationLimits.x,
            cannonElevationLimits.y
        );
        cannonPivot.localRotation = Quaternion.Euler(-currentCannonElevation, 0, 0);
    }

    public void FireMainGun()
    {
        if (gunDisabled || Time.time < nextFireTime) return;

        GameObject shell = Instantiate(shellPrefab, mainGunMuzzle.position, mainGunMuzzle.rotation);
        Rigidbody shellRb = shell.GetComponent<Rigidbody>();
        shellRb.linearVelocity = mainGunMuzzle.forward * shellVelocity;

        // Apply realistic recoil
        rb.AddForceAtPosition(-mainGunMuzzle.forward * shellVelocity * 50f, mainGunMuzzle.position, ForceMode.Impulse);

        nextFireTime = Time.time + fireRate;

        // Screen shake and sound would be triggered here
    }
    #endregion

    #region Damage System
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Calculate component hit based on position
        TankComponent hitComponent = GetComponentFromHitPosition(hitPoint);
        float componentDamage = damage * GetComponentDamageMultiplier(hitComponent);

        // Apply component-specific damage
        switch (hitComponent)
        {
            case TankComponent.Tracks:
                componentArmor[0] -= componentDamage;
                if (componentArmor[0] <= 0 && !tracksDisabled) DisableTracks();
                break;
            case TankComponent.Turret:
                componentArmor[1] -= componentDamage;
                if (componentArmor[1] <= 0 && !turretDisabled) DisableTurret();
                break;
            case TankComponent.Engine:
                componentArmor[2] -= componentDamage;
                if (componentArmor[2] <= 0 && !engineDisabled) DisableEngine();
                break;
            case TankComponent.MainGun:
                componentArmor[3] -= componentDamage;
                if (componentArmor[3] <= 0 && !gunDisabled) DisableGun();
                break;
        }

        // Show hit effect
        Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(hitNormal));

        // Apply overall armor damage
        currentArmor -= damage;
        if (currentArmor <= 0) DestroyTank();
    }

    private TankComponent GetComponentFromHitPosition(Vector3 hitPosition)
    {
        Vector3 localHit = transform.InverseTransformPoint(hitPosition);

        if (localHit.y < 0.5f) return TankComponent.Tracks;
        if (localHit.z > 0) return TankComponent.MainGun;
        if (Mathf.Abs(localHit.x) > 1.5f) return TankComponent.Turret;

        return TankComponent.Engine;
    }

    private float GetComponentDamageMultiplier(TankComponent component)
    {
        return component switch
        {
            TankComponent.Tracks => 1.5f,
            TankComponent.Engine => 1.2f,
            TankComponent.MainGun => 0.8f,
            _ => 1.0f
        };
    }

    private void DisableTracks()
    {
        tracksDisabled = true;
        maxSpeed *= 0.3f;
        rotationSpeed *= 0.2f;
    }

    private void DisableTurret()
    {
        turretDisabled = true;
        turretRotationSpeed *= 0.1f;
    }

    private void DisableEngine()
    {
        engineDisabled = true;
        enginePower = 0;
        brakeForce = 0;
    }

    private void DisableGun()
    {
        gunDisabled = true;
    }

    private void DestroyTank()
    {
        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
    #endregion

    #region AI System
    private void ExecuteAIBehavior()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool hasLineOfSight = CheckLineOfSightToPlayer();

        switch (currentAIState)
        {
            case AIState.Patrolling:
                PatrolBehavior();
                if (distanceToPlayer < aiDetectionRange && hasLineOfSight)
                {
                    currentAIState = AIState.Engaging;
                }
                break;

            case AIState.Engaging:
                EngageBehavior(distanceToPlayer, hasLineOfSight);
                if (distanceToPlayer > aiDetectionRange * 1.2f || !hasLineOfSight)
                {
                    currentAIState = AIState.Patrolling;
                }
                break;

            case AIState.Evading:
                EvadeBehavior();
                if (currentArmor > maxArmor * 0.3f)
                {
                    currentAIState = AIState.Engaging;
                }
                break;
        }
    }

    private void PatrolBehavior()
    {
        // Move toward patrol destination
        Vector3 direction = (patrolDestination - transform.position).normalized;
        float throttle = Vector3.Dot(transform.forward, direction);
        float steering = Vector3.Dot(transform.right, direction);

        MoveTank(throttle, steering);

        // Find new destination when close
        if (Vector3.Distance(transform.position, patrolDestination) < 5f)
        {
            SetNewPatrolDestination();
        }
    }

    private void EngageBehavior(float distance, bool hasLOS)
    {
        if (!hasLOS)
        {
            // Move to get line of sight
            Vector3 moveDirection = (playerTarget.position - transform.position).normalized;
            MoveTank(1f, Mathf.Sign(Vector3.Dot(transform.right, moveDirection)));
            return;
        }

        // Position turret
        Vector3 targetDirection = playerTarget.position - turretPivot.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        turretPivot.rotation = Quaternion.RotateTowards(
            turretPivot.rotation,
            targetRotation,
            turretRotationSpeed * Time.deltaTime
        );

        // Fire when ready
        if (distance < aiFiringRange &&
            Quaternion.Angle(turretPivot.rotation, targetRotation) < 5f &&
            Time.time > nextFireTime)
        {
            FireMainGun();
        }

        // Maneuvering
        float moveInput = distance > aiFiringRange * 0.7f ? 1f : -0.5f;
        float steerInput = Mathf.PerlinNoise(Time.time * 0.3f, 0) - 0.5f;
        MoveTank(moveInput, steerInput);

        // Check for critical damage
        if (currentArmor < maxArmor * 0.3f)
        {
            currentAIState = AIState.Evading;
        }
    }

    private void EvadeBehavior()
    {
        // Move away from player while facing toward
        Vector3 evadeDirection = (transform.position - playerTarget.position).normalized;
        Vector3 moveDirection = Quaternion.Euler(0, 45f, 0) * evadeDirection;

        float throttle = Vector3.Dot(transform.forward, moveDirection);
        float steering = Vector3.Dot(transform.right, moveDirection);

        MoveTank(throttle, steering);
    }

    private bool CheckLineOfSightToPlayer()
    {
        RaycastHit hit;
        Vector3 rayDirection = playerTarget.position - (turretPivot.position + Vector3.up);
        if (Physics.Raycast(turretPivot.position + Vector3.up, rayDirection, out hit, aiDetectionRange))
        {
            return hit.transform == playerTarget;
        }
        return false;
    }

    private void SetNewPatrolDestination()
    {
        Vector2 randomCircle = Random.insideUnitCircle * 50f;
        patrolDestination = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
    #endregion

    #region Visual & Audio
    private void UpdateTrackVisuals()
    {
        if (!isMoving) return;

        float scrollAmount = rb.linearVelocity.magnitude * trackScrollSpeed * Time.deltaTime;
        foreach (Transform track in trackShoes)
        {
            track.localPosition += transform.right * scrollAmount;
            if (track.localPosition.magnitude > 1f) track.localPosition = Vector3.zero;
        }
    }

    private void UpdateEngineSound()
    {
        if (engineSound == null) return;

        float speedRatio = rb.linearVelocity.magnitude / maxSpeed;
        float targetPitch = 0.7f + speedRatio * 0.5f;
        engineSound.pitch = Mathf.Lerp(engineSound.pitch, targetPitch, Time.deltaTime * 2f);

        float targetVolume = engineDisabled ? 0.1f : 0.7f;
        engineSound.volume = Mathf.Lerp(engineSound.volume, targetVolume, Time.deltaTime * 3f);
    }

    private void UpdateSmokeEmissions()
    {
        foreach (var emitter in smokeEmitters)
        {
            var emission = emitter.emission;
            emission.rateOverTime = engineDisabled ? 100 :
                Mathf.Lerp(20, 50, rb.linearVelocity.magnitude / maxSpeed);
        }
    }
    #endregion

    #region Public API
    public bool AreTracksDisabled() => tracksDisabled;
    public bool IsTurretDisabled() => turretDisabled;
    public bool IsEngineDisabled() => engineDisabled;
    public bool IsGunDisabled() => gunDisabled;
    public float GetArmorPercentage() => currentArmor / maxArmor;
    #endregion

    private enum TankComponent { Tracks, Turret, Engine, MainGun }
}
