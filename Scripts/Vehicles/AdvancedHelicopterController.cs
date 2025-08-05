using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class AdvancedHelicopterController : MonoBehaviour
{
    [Header("Rotor Systems")]
    [SerializeField] private Transform mainRotor;
    [SerializeField] private Transform tailRotor;
    [SerializeField] private float mainRotorDiameter = 14f;
    [SerializeField] private float tailRotorDiameter = 2.5f;
    [SerializeField] private float maxRotorRPM = 300f;
    [SerializeField] private float rotorAcceleration = 2f;
    [SerializeField] private float rotorDeceleration = 4f;
    [SerializeField] private float bladeFlapAngle = 5f;
    [SerializeField] private float rotorStartupTime = 5f;

    [Header("Flight Parameters")]
    [SerializeField] private float maxLiftForce = 10000f;
    [SerializeField] private float liftCurveSharpness = 2f;
    [SerializeField] private float cyclicResponse = 8f;
    [SerializeField] private float pedalResponse = 5f;
    [SerializeField] private float stabilityFactor = 0.7f;
    [SerializeField] private float autoHoverThreshold = 2f;

    [Header("Stability Systems")]
    [SerializeField] private bool autoStabilization = true;
    [SerializeField] private float stabilizationStrength = 10f;
    [SerializeField] private float startupStabilizationDuration = 10f;
    [SerializeField] private float groundStabilizationHeight = 5f;
    [SerializeField] private float groundStabilizationForce = 500f;
    [SerializeField] private float hoverAltitude = 10f;

    [Header("Advanced Aerodynamics")]
    [SerializeField] private float groundEffectHeight = 10f;
    [SerializeField] private float groundEffectMultiplier = 1.3f;
    [SerializeField] private float vortexRingThreshold = 200f;
    [SerializeField] private float vortexRingRecovery = 0.5f;
    [SerializeField] private float retreatingBladeStallThreshold = 0.9f;
    [SerializeField] private float bladeStallRecovery = 0.8f;

    [Header("Engine Systems")]
    [SerializeField] private float enginePower = 1000f;
    [SerializeField] private float engineTorque = 500f;
    [SerializeField] private float fuelCapacity = 200f;
    [SerializeField] private float fuelConsumptionRate = 0.1f;
    [SerializeField] private float engineFailureThreshold = 0.3f;

    [Header("Weapon Systems")]
    [SerializeField] private Transform[] hardpoints;
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private float missileCooldown = 1f;
    [SerializeField] private float missileVelocity = 80f;

    [Header("Audio Systems")]
    [SerializeField] private AudioSource engineAudio;
    [SerializeField] private AudioSource rotorAudio;
    [SerializeField] private AudioSource missileAudio;
    [SerializeField] private AudioSource damageAudio;
    [SerializeField] private AudioSource windAudio;
    [SerializeField] private AudioSource startupAudio;

    [SerializeField] private AudioClip engineSound;
    [SerializeField] private AudioClip rotorSound;
    [SerializeField] private AudioClip missileFireSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip windSound;
    [SerializeField] private AudioClip startupSound;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem engineExhaust;
    [SerializeField] private ParticleSystem rotorWash;
    [SerializeField] private ParticleSystem damageSmoke;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject bladeTipVortex;

    // Flight controls
    private float collectiveInput;
    private Vector2 cyclicInput;
    private float pedalInput;
    private bool weaponFire;
    private bool engineToggle = true;

    // Physics state
    private Rigidbody rb;
    private float currentRotorRPM;
    private float currentEnginePower;
    private float currentFuel;
    private bool engineRunning = true;
    private bool isDestroyed;
    private bool isStartingUp = true;

    // Aerodynamics
    private float bladeAngleOfAttack;
    private float vortexRingState;
    private float retreatingBladeStall;

    // Damage system
    private float structuralIntegrity = 100f;
    private float maxStructuralIntegrity = 100f;

    // Audio
    private float enginePitch = 1f;
    private float rotorPitch = 1f;

    // Timers
    private float lastMissileTime;
    private float lastDamageTime;
    private float startupTimer;

    // Stability systems
    private float stabilizationTimer;
    private bool groundStabilizationActive = true;
    private Vector3 initialPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero; // Start with center mass at origin
        currentFuel = fuelCapacity;
        initialPosition = transform.position;

        // Configure rigidbody for stability
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = 3000f; // Typical helicopter mass in kg

        InitializeAudio();
        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        isStartingUp = true;
        stabilizationTimer = startupStabilizationDuration;

        // Play startup sound
        if (startupAudio && startupSound)
        {
            startupAudio.PlayOneShot(startupSound);
        }

        // Gradually increase rotor RPM
        float startupProgress = 0f;
        while (startupProgress < 1f)
        {
            startupProgress += Time.deltaTime / rotorStartupTime;
            currentRotorRPM = Mathf.Lerp(0, maxRotorRPM * 0.5f, startupProgress);
            yield return null;
        }

        // Engine at idle
        currentEnginePower = 0.3f;
        isStartingUp = false;
    }

    private void InitializeAudio()
    {
        if (engineAudio && engineSound)
        {
            engineAudio.clip = engineSound;
            engineAudio.loop = true;
            engineAudio.Play();
        }

        if (rotorAudio && rotorSound)
        {
            rotorAudio.clip = rotorSound;
            rotorAudio.loop = true;
            rotorAudio.Play();
        }

        if (windAudio && windSound)
        {
            windAudio.clip = windSound;
            windAudio.loop = true;
            windAudio.volume = 0.4f;
            windAudio.Play();
        }
    }

    private void Update()
    {
        if (isDestroyed) return;

        if (isStartingUp)
        {
            ApplyStartupStabilization();
            return;
        }

        HandleInput();
        UpdateRotors();
        UpdateEngine();
        UpdateAudio();
        UpdateVisualEffects();
        CheckWeapons();
        CheckDamage();
        UpdateStabilizationTimer();
    }

    private void UpdateStabilizationTimer()
    {
        if (stabilizationTimer > 0)
        {
            stabilizationTimer -= Time.deltaTime;
        }
    }

    private void ApplyStartupStabilization()
    {
        // Strong stabilization during startup
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, 5f * Time.deltaTime);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, 5f * Time.deltaTime);

        // Maintain position during startup
        transform.position = Vector3.Lerp(transform.position, initialPosition, 2f * Time.deltaTime);

        // Keep upright orientation
        Quaternion targetRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
    }

    private void HandleInput()
    {
        // Engine toggle
        if (Input.GetKeyDown(KeyCode.I))
        {
            engineToggle = !engineToggle;
        }

        // Collective: Q/E keys
        if (Input.GetKey(KeyCode.Q)) collectiveInput += Time.deltaTime * 0.5f;
        if (Input.GetKey(KeyCode.E)) collectiveInput -= Time.deltaTime * 0.5f;
        collectiveInput = Mathf.Clamp01(collectiveInput);

        // Cyclic: WASD keys
        cyclicInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) cyclicInput.y += 1f;
        if (Input.GetKey(KeyCode.S)) cyclicInput.y -= 1f;
        if (Input.GetKey(KeyCode.A)) cyclicInput.x -= 1f;
        if (Input.GetKey(KeyCode.D)) cyclicInput.x += 1f;

        // Pedals: Arrow keys
        pedalInput = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) pedalInput -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) pedalInput += 1f;

        // Weapons: Space
        weaponFire = Input.GetKey(KeyCode.Space);

        // Stability override: B key
        if (Input.GetKeyDown(KeyCode.B))
        {
            autoStabilization = !autoStabilization;
        }
    }

    private void UpdateRotors()
    {
        // Main rotor rotation
        if (mainRotor)
        {
            float targetRPM = engineRunning ? maxRotorRPM * currentEnginePower : 0f;
            currentRotorRPM = Mathf.MoveTowards(currentRotorRPM, targetRPM,
                (targetRPM > currentRotorRPM ? rotorAcceleration : rotorDeceleration) * Time.deltaTime);

            mainRotor.Rotate(Vector3.up, currentRotorRPM * 6f * Time.deltaTime);
        }

        // Tail rotor rotation
        if (tailRotor)
        {
            tailRotor.Rotate(Vector3.forward, currentRotorRPM * 8f * Time.deltaTime);
        }
    }

    private void UpdateEngine()
    {
        if (!engineRunning) return;

        // Consume fuel
        currentFuel -= fuelConsumptionRate * currentEnginePower * Time.deltaTime;

        // Engine failure when fuel is low
        if (currentFuel <= 0)
        {
            engineRunning = false;
            currentEnginePower = 0f;
            if (engineExhaust) engineExhaust.Stop();
            return;
        }

        // Adjust engine power based on collective input
        currentEnginePower = Mathf.MoveTowards(currentEnginePower, engineToggle ? collectiveInput : 0f, 2f * Time.deltaTime);

        // Engine damage effects
        if (structuralIntegrity < maxStructuralIntegrity * engineFailureThreshold)
        {
            currentEnginePower *= 0.8f + 0.4f * Mathf.PerlinNoise(Time.time * 5f, 0);
        }
    }

    private void FixedUpdate()
    {
        if (isDestroyed || isStartingUp) return;

        CalculateAerodynamics();
        ApplyForces();
        ApplyStabilization();
        ApplyGroundStabilization();
    }

    private void CalculateAerodynamics()
    {
        // Calculate air density (simplified)
        float airDensity = 1.225f * Mathf.Exp(-transform.position.y / 9000f);

        // Calculate rotor properties
        float rotorArea = Mathf.PI * Mathf.Pow(mainRotorDiameter / 2, 2);
        float tipSpeed = (currentRotorRPM / 60f) * Mathf.PI * mainRotorDiameter;

        // Calculate lift force
        float liftForce = airDensity * rotorArea * Mathf.Pow(tipSpeed, 2) * collectiveInput;
        liftForce = Mathf.Min(liftForce, maxLiftForce);

        // Apply ground effect
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundEffectHeight))
        {
            float groundFactor = 1 - (hit.distance / groundEffectHeight);
            liftForce *= 1 + groundFactor * (groundEffectMultiplier - 1);
        }

        // Apply lift force
        rb.AddForce(Vector3.up * liftForce);

        // Calculate cyclic forces
        Vector3 cyclicForce = new Vector3(cyclicInput.x, 0, cyclicInput.y) * cyclicResponse * liftForce;
        rb.AddRelativeForce(cyclicForce);

        // Calculate torque and anti-torque
        float torque = engineTorque * currentEnginePower;
        float antiTorque = pedalInput * pedalResponse * liftForce;
        rb.AddRelativeTorque(new Vector3(0, antiTorque - torque, 0));

        // Apply blade flapping
        bladeAngleOfAttack = Mathf.Clamp(bladeAngleOfAttack + cyclicInput.magnitude * 0.1f, 0, bladeFlapAngle);

        // Calculate vortex ring state
        float descentRate = -rb.linearVelocity.y;
        if (descentRate > vortexRingThreshold && collectiveInput > 0.2f)
        {
            vortexRingState = Mathf.Clamp01(vortexRingState + (descentRate / vortexRingThreshold) * Time.fixedDeltaTime);
            rb.AddForce(Vector3.down * liftForce * vortexRingState * 0.5f);
        }
        else
        {
            vortexRingState = Mathf.Clamp01(vortexRingState - vortexRingRecovery * Time.fixedDeltaTime);
        }

        // Calculate retreating blade stall
        float advanceRatio = rb.linearVelocity.magnitude / tipSpeed;
        if (advanceRatio > retreatingBladeStallThreshold)
        {
            retreatingBladeStall = Mathf.Clamp01(retreatingBladeStall + (advanceRatio - retreatingBladeStallThreshold) * 5f * Time.fixedDeltaTime);
            rb.AddRelativeTorque(Vector3.right * -retreatingBladeStall * 100f);
        }
        else
        {
            retreatingBladeStall = Mathf.Clamp01(retreatingBladeStall - bladeStallRecovery * Time.fixedDeltaTime);
        }
    }

    private void ApplyForces()
    {
        // Apply damping forces
        rb.AddForce(-rb.linearVelocity * 0.1f);
        rb.AddTorque(-rb.angularVelocity * 0.2f);
    }

    private void ApplyStabilization()
    {
        if (!autoStabilization) return;

        // Stronger stabilization during initial flight
        float stabilizationPower = stabilizationTimer > 0 ? 2f : 1f;

        // Auto-stabilize when near zero input
        if (cyclicInput.magnitude < 0.1f && Mathf.Abs(pedalInput) < 0.1f)
        {
            // Stabilize roll and pitch
            float rollStabilization = -Vector3.Dot(transform.right, Vector3.up) * stabilityFactor * 50f * stabilizationPower;
            float pitchStabilization = -Vector3.Dot(transform.forward, Vector3.up) * stabilityFactor * 50f * stabilizationPower;

            rb.AddRelativeTorque(new Vector3(
                pitchStabilization,
                0,
                rollStabilization
            ) * Time.fixedDeltaTime);

            // Stabilize yaw
            if (Mathf.Abs(rb.angularVelocity.y) > 0.1f)
            {
                rb.AddRelativeTorque(Vector3.up * -rb.angularVelocity.y * stabilityFactor * 5f * stabilizationPower);
            }
        }

        // Altitude hold
        if (collectiveInput < autoHoverThreshold && rb.linearVelocity.y < -1f)
        {
            rb.AddForce(Vector3.up * 50f * stabilizationPower);
        }

        // Horizontal position stabilization
        if (rb.linearVelocity.magnitude < 2f && stabilizationTimer > 0)
        {
            rb.AddForce(-rb.linearVelocity * 10f * stabilizationPower);
        }
    }

    private void ApplyGroundStabilization()
    {
        if (!groundStabilizationActive) return;

        // Check if close to ground
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundStabilizationHeight))
        {
            // Push away from ground
            rb.AddForce(Vector3.up * groundStabilizationForce * (1 - (hit.distance / groundStabilizationHeight)));

            // Reduce horizontal velocity near ground
            Vector3 horizontalVelocity = rb.linearVelocity;
            horizontalVelocity.y = 0;
            rb.AddForce(-horizontalVelocity * 5f);

            // Stabilize rotation
            if (hit.distance < groundStabilizationHeight * 0.5f)
            {
                Quaternion targetRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.fixedDeltaTime));
            }
        }
    }

    private void UpdateAudio()
    {
        if (!engineAudio || !rotorAudio || !windAudio) return;

        // Engine audio
        enginePitch = 0.8f + currentEnginePower * 0.6f;
        engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, enginePitch, 5f * Time.deltaTime);
        engineAudio.volume = 0.5f + currentEnginePower * 0.3f;

        // Rotor audio
        rotorPitch = 0.7f + (currentRotorRPM / maxRotorRPM) * 0.8f;
        rotorAudio.pitch = Mathf.Lerp(rotorAudio.pitch, rotorPitch, 5f * Time.deltaTime);
        rotorAudio.volume = 0.6f + (currentRotorRPM / maxRotorRPM) * 0.4f;

        // Damage effects on audio
        if (structuralIntegrity < maxStructuralIntegrity * 0.5f)
        {
            engineAudio.pitch *= 1f + Mathf.PerlinNoise(Time.time * 8f, 0) * 0.3f;
            rotorAudio.pitch *= 1f + Mathf.PerlinNoise(Time.time * 10f, 10) * 0.2f;
        }

        // Wind audio
        float windIntensity = Mathf.Clamp01(rb.linearVelocity.magnitude / 50f);
        windAudio.volume = windIntensity * 0.5f;
        windAudio.pitch = 0.8f + windIntensity * 0.4f;
    }

    private void UpdateVisualEffects()
    {
        // Engine exhaust
        if (engineExhaust)
        {
            var emission = engineExhaust.emission;
            emission.rateOverTime = currentEnginePower * 50f;

            if (engineRunning && !engineExhaust.isPlaying)
                engineExhaust.Play();
            else if (!engineRunning && engineExhaust.isPlaying)
                engineExhaust.Stop();
        }

        // Rotor wash
        if (rotorWash)
        {
            var emission = rotorWash.emission;
            emission.rateOverTime = collectiveInput * 100f;

            if (currentRotorRPM > 100f && !rotorWash.isPlaying)
                rotorWash.Play();
            else if (currentRotorRPM < 50f && rotorWash.isPlaying)
                rotorWash.Stop();
        }

        // Damage smoke
        if (damageSmoke)
        {
            var emission = damageSmoke.emission;
            float smokeIntensity = 1f - (structuralIntegrity / maxStructuralIntegrity);
            emission.rateOverTime = smokeIntensity * 50f;

            if (structuralIntegrity < maxStructuralIntegrity * 0.7f && !damageSmoke.isPlaying)
                damageSmoke.Play();
            else if (structuralIntegrity > maxStructuralIntegrity * 0.8f && damageSmoke.isPlaying)
                damageSmoke.Stop();
        }

        // Blade tip vortices
        if (bladeTipVortex && mainRotor && currentRotorRPM > 200f && collectiveInput > 0.2f)
        {
            if (!bladeTipVortex.activeSelf)
                bladeTipVortex.SetActive(true);
        }
        else if (bladeTipVortex && bladeTipVortex.activeSelf)
        {
            bladeTipVortex.SetActive(false);
        }
    }

    private void CheckWeapons()
    {
        if (!weaponFire) return;
        if (Time.time - lastMissileTime < missileCooldown) return;
        if (hardpoints.Length == 0 || missilePrefab == null) return;
        if (isDestroyed) return;

        // Fire missile from random hardpoint
        Transform launchPoint = hardpoints[Random.Range(0, hardpoints.Length)];
        GameObject missile = Instantiate(missilePrefab, launchPoint.position, launchPoint.rotation);

        // Add velocity to missile
        Rigidbody missileRb = missile.GetComponent<Rigidbody>();
        if (missileRb)
        {
            missileRb.linearVelocity = rb.linearVelocity;
            missileRb.AddForce(transform.forward * missileVelocity, ForceMode.VelocityChange);
        }

        // Play missile sound
        if (missileAudio && missileFireSound)
        {
            missileAudio.PlayOneShot(missileFireSound);
        }

        lastMissileTime = Time.time;
    }

    private void CheckDamage()
    {
        // Damage from high-G maneuvers
        float gForce = rb.linearVelocity.magnitude / 9.81f;
        if (gForce > 4f && Time.time - lastDamageTime > 1f)
        {
            ApplyDamage(gForce * 0.5f);
            lastDamageTime = Time.time;
        }

        // Damage when hitting the ground too hard
        if (transform.position.y < 5f && rb.linearVelocity.y < -10f)
        {
            ApplyDamage(-rb.linearVelocity.y * 0.1f);
        }
    }

    public void ApplyDamage(float amount)
    {
        if (isDestroyed) return;

        structuralIntegrity -= amount;
        structuralIntegrity = Mathf.Max(structuralIntegrity, 0);

        // Play damage sound
        if (damageAudio && damageSound && amount > 5f)
        {
            damageAudio.PlayOneShot(damageSound);
        }

        // Critical damage effects
        if (structuralIntegrity < maxStructuralIntegrity * 0.3f)
        {
            // Random system failures
            if (Random.value < 0.02f) engineRunning = false;
            if (Random.value < 0.01f) currentRotorRPM *= 0.8f;
        }

        // Destruction
        if (structuralIntegrity <= 0)
        {
            DestroyHelicopter();
        }
    }

    private void DestroyHelicopter()
    {
        isDestroyed = true;
        rb.linearDamping = 1f;
        rb.angularDamping = 1f;

        if (explosionPrefab)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // Play explosion sound
        if (damageAudio && explosionSound)
        {
            damageAudio.PlayOneShot(explosionSound);
        }

        // Stop all systems
        if (engineExhaust) engineExhaust.Stop();
        if (rotorWash) rotorWash.Stop();
        if (damageSmoke) damageSmoke.Stop();
        if (bladeTipVortex) bladeTipVortex.SetActive(false);

        // Stop audio
        if (engineAudio) engineAudio.Stop();
        if (rotorAudio) rotorAudio.Stop();

        // Disable controls
        enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 10f)
        {
            ApplyDamage(collision.relativeVelocity.magnitude * 0.5f);
        }

        // Disable ground stabilization after first collision
        groundStabilizationActive = false;
    }

    // Public API for UI systems
    public float GetAltitude() => transform.position.y;
    public float GetSpeed() => rb.linearVelocity.magnitude * 3.6f; // km/h
    public float GetRotorRPM() => currentRotorRPM;
    public float GetEnginePower() => currentEnginePower;
    public float GetFuelPercentage() => currentFuel / fuelCapacity;
    public float GetStructuralIntegrity() => structuralIntegrity / maxStructuralIntegrity;
    public bool IsEngineRunning() => engineRunning;
    public float GetVortexState() => vortexRingState;
    public float GetBladeStall() => retreatingBladeStall;
    public bool IsStabilizing() => stabilizationTimer > 0;
    public bool IsStartingUp() => isStartingUp;
}
