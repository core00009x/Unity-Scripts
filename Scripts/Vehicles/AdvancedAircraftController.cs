using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class AdvancedAircraftController : MonoBehaviour
{
    [Header("Physics Parameters")]
    [SerializeField] private float maxThrust = 250f;
    [SerializeField] private float thrustAcceleration = 2f;
    [SerializeField] private float wingArea = 25f;
    [SerializeField] private AnimationCurve liftCoefficient = AnimationCurve.Linear(-20, 0, 20, 1.5f);
    [SerializeField] private float dragCoefficient = 0.024f;
    [SerializeField] private float inducedDragCoefficient = 0.8f;

    [Header("Keyboard Controls")]
    [SerializeField] private float keyboardSensitivity = 1f;
    [SerializeField] private float throttleSpeed = 0.5f;

    [Header("Control Surfaces")]
    [SerializeField] private float maxPitchAngle = 25f;
    [SerializeField] private float pitchSpeed = 60f;
    [SerializeField] private float maxRollAngle = 30f;
    [SerializeField] private float rollSpeed = 100f;
    [SerializeField] private float maxYawAngle = 25f;
    [SerializeField] private float yawSpeed = 50f;

    [Header("Flight Assistance")]
    [SerializeField] private float stabilityFactor = 0.5f;
    [SerializeField] private float autoTrimSpeed = 0.1f;
    [SerializeField] private float stallRecoveryThreshold = 20f;

    [Header("Damage System")]
    [SerializeField] private float maxStructuralIntegrity = 1000f;
    [SerializeField] private float gForceDamageThreshold = 8f;
    [SerializeField] private float damageDragMultiplier = 2f;

    [Header("References")]
    [SerializeField] private Transform centerOfMass;
    [SerializeField] private List<ControlSurface> controlSurfaces = new List<ControlSurface>();
    [SerializeField] private ParticleSystem afterburnerEffect;
    [SerializeField] private GameObject explosionPrefab;

    [Header("Keyboard Assistance")]
    [SerializeField] private float keyboardStallThreshold = 30f;
    [SerializeField] private float keyboardStallRecovery = 2f;

    [Header("Advanced Aerodynamics")]
    [SerializeField] private bool compressibilityEffects = true;
    [SerializeField] private float machCritical = 0.8f;
    [SerializeField] private AnimationCurve machTuckEffect = AnimationCurve.Linear(0.8f, 0, 1.2f, -1.5f);
    [SerializeField] private float windTurbulenceIntensity = 0.5f;
    [SerializeField] private float groundEffectHeight = 10f;
    [SerializeField] private float groundEffectMultiplier = 1.5f;

    [Header("Advanced Systems")]
    [SerializeField] private float fuelCapacity = 1000f;
    [SerializeField] private float idleFuelConsumption = 5f;
    [SerializeField] private float maxFuelConsumption = 25f;
    [SerializeField] private float afterburnerFuelMultiplier = 3f;
    [SerializeField] private bool flyByWireEnabled = true;
    [SerializeField] private float flyByWireAuthority = 0.8f;
    [SerializeField] private float stallWarningThreshold = 15f;

    [Header("Weapon Systems")]
    [SerializeField] private Transform[] hardpoints;
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private float missileCooldown = 0.5f;
    [SerializeField] private float missileVelocity = 100f;

    [Header("Audio Systems")]
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private AudioSource afterburnerAudioSource;
    [SerializeField] private AudioSource stallAudioSource;
    [SerializeField] private AudioSource missileAudioSource;
    [SerializeField] private AudioSource damageAudioSource;
    [SerializeField] private AudioSource sonicBoomAudioSource;
    [SerializeField] private AudioSource windAudioSource;
    
    [SerializeField] private AudioClip engineSound;
    [SerializeField] private AudioClip afterburnerSound;
    [SerializeField] private AudioClip stallWarningSound;
    [SerializeField] private AudioClip missileFireSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip sonicBoomSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip windTurbulenceSound;

    // Physics components
    private Rigidbody rb;
    private float currentThrust;
    private float airDensity = 1.225f;
    private float angleOfAttack;
    private float structuralIntegrity;
    private bool isDestroyed;

    // Input values
    private Vector2 controlInput;
    private float throttleInput;
    private bool afterburnerActive;

    // G-force calculation
    private Vector3 lastVelocity;
    private float currentGForce;

    private float pitchInput;
    private float rollInput;
    private float yawInput;

    // Advanced systems state
    private float currentFuel;
    private bool stallWarningActive;
    private float lastMissileFireTime;
    private Vector3 windVelocity;
    private float lastWindChangeTime;
    private float enginePitchTarget = 1f;
    private float engineVolumeTarget = 0.7f;

    // Fly-by-wire system
    private Vector3 fbwTargetAngles;
    private Vector3 fbwCurrentAngles;

    // Supersonic effects
    private bool sonicBoomTriggered;
    private float currentMach;

    private float smoothPitch;
    private float smoothRoll;
    private float smoothYaw;

    [System.Serializable]
    public class ControlSurface
    {
        public Transform surfaceTransform;
        public Vector3 axis = Vector3.right;
        public float maxAngle = 30f;
        public float speed = 90f;
        [HideInInspector] public float targetAngle;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass) rb.centerOfMass = centerOfMass.localPosition;
        structuralIntegrity = maxStructuralIntegrity;
        currentFuel = fuelCapacity;
        windVelocity = Random.onUnitSphere * windTurbulenceIntensity;
        
        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        if (engineAudioSource && engineSound)
        {
            engineAudioSource.clip = engineSound;
            engineAudioSource.loop = true;
            engineAudioSource.Play();
        }
        
        if (afterburnerAudioSource && afterburnerSound)
        {
            afterburnerAudioSource.clip = afterburnerSound;
            afterburnerAudioSource.loop = true;
            afterburnerAudioSource.volume = 0.7f;
        }
        
        if (stallAudioSource && stallWarningSound)
        {
            stallAudioSource.clip = stallWarningSound;
            stallAudioSource.loop = true;
        }
        
        if (windAudioSource && windTurbulenceSound)
        {
            windAudioSource.clip = windTurbulenceSound;
            windAudioSource.loop = true;
            windAudioSource.volume = 0.3f;
            windAudioSource.Play();
        }
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        lastVelocity = rb.linearVelocity;
    }

    private void OnDisable()
    {
        if (afterburnerAudioSource && afterburnerAudioSource.isPlaying)
            afterburnerAudioSource.Stop();
            
        if (stallAudioSource && stallAudioSource.isPlaying)
            stallAudioSource.Stop();
    }

    private void Update()
    {
        if (isDestroyed) return;

        HandleKeyboardInput();
        CalculateGForces();
        UpdateControlSurfaces();
        HandleAfterburner();
        CheckStructuralFailure();
        HandleWeaponInput();
        UpdateWindSystem();
        CheckFuelStatus();
        UpdateFlyByWireSystem();
        UpdateStallWarning();
        UpdateEngineAudio();

        smoothPitch = Mathf.Lerp(smoothPitch, pitchInput, 10f * Time.deltaTime);
        smoothRoll = Mathf.Lerp(smoothRoll, rollInput, 10f * Time.deltaTime);
        smoothYaw = Mathf.Lerp(smoothYaw, yawInput, 8f * Time.deltaTime);
    }

    #region Keyboard Input Handling
    private void HandleKeyboardInput()
    {
        // Pitch: W/S keys
        pitchInput = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        
        // Roll: A/D keys
        rollInput = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        
        // Yaw: Q/E keys
        yawInput = Input.GetKey(KeyCode.Q) ? -1f : Input.GetKey(KeyCode.E) ? 1f : 0f;
        
        // Throttle: Shift/Ctrl
        if (Input.GetKey(KeyCode.LeftShift)) throttleInput += throttleSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftControl)) throttleInput -= throttleSpeed * Time.deltaTime;
        throttleInput = Mathf.Clamp01(throttleInput);
        
        // Afterburner: Space
        afterburnerActive = Input.GetKey(KeyCode.Space);
        
        // Fire weapons: F or Mouse0
        if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0)) 
            FireMissile();
    }
    #endregion

    #region Control System
    private void UpdateControlSurfaces()
    {
        // Apply keyboard sensitivity
        float pitch = pitchInput * maxPitchAngle * keyboardSensitivity;
        float roll = rollInput * maxRollAngle * keyboardSensitivity;
        float yaw = yawInput * maxYawAngle * keyboardSensitivity;

        // Apply to control surfaces
        foreach (ControlSurface surface in controlSurfaces)
        {
            if (surface.surfaceTransform == null) continue;

            // Simplified control surface mapping
            surface.targetAngle = pitch;

            // Smoothly move control surface
            Quaternion targetRotation = Quaternion.AngleAxis(surface.targetAngle, surface.axis);
            surface.surfaceTransform.localRotation = Quaternion.RotateTowards(
                surface.surfaceTransform.localRotation,
                targetRotation,
                surface.speed * Time.deltaTime
            );
        }

        // Apply rotational forces
        rb.AddTorque(transform.right * pitch * pitchSpeed * Time.fixedDeltaTime);
        rb.AddTorque(-transform.forward * roll * rollSpeed * Time.fixedDeltaTime);
        rb.AddTorque(transform.up * yaw * yawSpeed * Time.fixedDeltaTime);
    }
    #endregion

    #region Aerodynamics
    private void CalculateAerodynamics()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 worldVelocity = rb.linearVelocity;
        float speed = worldVelocity.magnitude;
        float dynamicPressure = 0.5f * airDensity * speed * speed;

        // Calculate angle of attack (pitch)
        angleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z) * Mathf.Rad2Deg;

        // Calculate lift
        float liftCoeff = liftCoefficient.Evaluate(angleOfAttack);
        float liftForce = dynamicPressure * wingArea * liftCoeff;
        Vector3 liftDirection = Vector3.Cross(worldVelocity.normalized, transform.right).normalized;
        rb.AddForce(liftDirection * liftForce);

        // Calculate drag
        float inducedDrag = liftCoeff * liftCoeff * inducedDragCoefficient;
        float totalDrag = dragCoefficient + inducedDrag;
        Vector3 dragForce = -worldVelocity.normalized * dynamicPressure * totalDrag * wingArea;
        rb.AddForce(dragForce * (structuralIntegrity / maxStructuralIntegrity));

        // 1. Compressibility effects (Mach tuck)
        currentMach = speed / 340f; // Speed of sound
        if (compressibilityEffects && currentMach > machCritical)
        {
            float machTuck = machTuckEffect.Evaluate(currentMach);
            rb.AddTorque(transform.right * machTuck * dynamicPressure * Time.fixedDeltaTime);
        }

        // 2. Ground effect
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundEffectHeight))
        {
            float groundFactor = 1 - (hit.distance / groundEffectHeight);
            liftForce *= 1 + groundFactor * groundEffectMultiplier;
            inducedDrag *= 1 - groundFactor * 0.7f;
        }

        // 3. Wind turbulence
        Vector3 relativeWind = windVelocity - worldVelocity;
        if (relativeWind.magnitude > 0.1f)
        {
            float windEffect = Vector3.Dot(relativeWind.normalized, transform.forward);
            rb.AddForce(relativeWind * dynamicPressure * 0.1f);
            rb.AddTorque(transform.right * windEffect * 5f * Time.fixedDeltaTime);
            
            // Wind audio effect
            if (windAudioSource)
            {
                float turbulence = Mathf.Clamp01(relativeWind.magnitude / 50f);
                windAudioSource.volume = turbulence * 0.5f;
                windAudioSource.pitch = 0.8f + turbulence * 0.4f;
            }
        }
    }

    private void ApplyThrust()
    {
        if (currentFuel <= 0) return;
        
        currentThrust = Mathf.MoveTowards(currentThrust, maxThrust * throttleInput, thrustAcceleration * Time.fixedDeltaTime);
        
        float thrust = currentThrust;
        if (afterburnerActive && throttleInput > 0.95f)
        {
            thrust *= 1.8f;
        }

        rb.AddForce(transform.forward * thrust);
    }
    #endregion

    #region Flight Controls
    private void ApplyFlightAssistance()
    {
        // Auto-stability
        Vector3 angularVelocity = transform.InverseTransformDirection(rb.angularVelocity);
        rb.AddTorque(-transform.right * angularVelocity.x * stabilityFactor * 100 * Time.fixedDeltaTime);
        rb.AddTorque(-transform.up * angularVelocity.y * stabilityFactor * 50 * Time.fixedDeltaTime);

        // Stall recovery
        if (angleOfAttack > stallRecoveryThreshold && rb.linearVelocity.magnitude > 30)
        {
            rb.AddTorque(transform.right * -pitchSpeed * 0.5f * Time.fixedDeltaTime);
        }

        // Fly-by-wire stall prevention
        if (flyByWireEnabled && angleOfAttack > stallWarningThreshold)
        {
            float correction = Mathf.Clamp((angleOfAttack - stallWarningThreshold) * -2f, -10f, 0f);
            rb.AddTorque(transform.right * correction * pitchSpeed * Time.fixedDeltaTime);
        }
    }
    #endregion

    #region Weapon Systems
    private void HandleWeaponInput()
    {
        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.F))
        {
            FireMissile();
        }
    }

    private void FireMissile()
    {
        if (Time.time - lastMissileFireTime < missileCooldown) return;
        if (hardpoints.Length == 0 || missilePrefab == null) return;
        if (isDestroyed) return;

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
        if (missileAudioSource && missileFireSound)
        {
            missileAudioSource.PlayOneShot(missileFireSound);
        }

        lastMissileFireTime = Time.time;
    }
    #endregion

    #region Fuel Management
    private void CheckFuelStatus()
    {
        float consumptionRate = Mathf.Lerp(idleFuelConsumption, maxFuelConsumption, throttleInput);
        if (afterburnerActive) consumptionRate *= afterburnerFuelMultiplier;
        
        currentFuel -= consumptionRate * Time.deltaTime;
        currentFuel = Mathf.Max(currentFuel, 0);

        if (currentFuel <= 0)
        {
            currentThrust = 0;
            if (afterburnerEffect && afterburnerEffect.isPlaying) 
                afterburnerEffect.Stop();
                
            if (afterburnerAudioSource && afterburnerAudioSource.isPlaying)
                afterburnerAudioSource.Stop();
        }
    }
    #endregion

    #region Fly-By-Wire System
    private void UpdateFlyByWireSystem()
    {
        if (!flyByWireEnabled) return;

        // Calculate desired attitude based on input
        fbwTargetAngles = new Vector3(
            pitchInput * maxPitchAngle,
            yawInput * maxYawAngle,
            rollInput * maxRollAngle
        );

        // Smoothly interpolate to target attitude
        fbwCurrentAngles = Vector3.Lerp(
            fbwCurrentAngles, 
            fbwTargetAngles, 
            flyByWireAuthority * Time.deltaTime
        );

        // Apply as torque with aerodynamic limits
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 correction = (fbwCurrentAngles - localAngVel) * 0.1f;
        
        rb.AddRelativeTorque(correction * 50f);
    }
    #endregion

    #region Environmental Systems
    private void UpdateWindSystem()
    {
        // Change wind direction periodically
        if (Time.time - lastWindChangeTime > 10f)
        {
            windVelocity = Random.onUnitSphere * windTurbulenceIntensity;
            lastWindChangeTime = Time.time;
        }

        // Sonic boom effect
        if (!sonicBoomTriggered && currentMach > 1.0f)
        {
            SonicBoomEffect();
            sonicBoomTriggered = true;
        }
        else if (sonicBoomTriggered && currentMach < 0.9f)
        {
            sonicBoomTriggered = false;
        }
    }

    private void SonicBoomEffect()
    {
        // Create shockwave effect
        GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shockwave.transform.position = transform.position;
        shockwave.transform.localScale = Vector3.one * 0.1f;
        ShockwaveController shockwaveController = shockwave.AddComponent<ShockwaveController>();
        shockwaveController.Initialize(transform.forward);
        Destroy(shockwave.GetComponent<Collider>());

        // Play sonic boom sound
        if (sonicBoomAudioSource && sonicBoomSound)
        {
            sonicBoomAudioSource.PlayOneShot(sonicBoomSound);
        }
    }
    #endregion

    #region Damage System
    private void CalculateGForces()
    {
        Vector3 acceleration = (rb.linearVelocity - lastVelocity) / Time.deltaTime;
        currentGForce = acceleration.magnitude / 9.81f;
        lastVelocity = rb.linearVelocity;

        // Apply G-force damage
        if (currentGForce > gForceDamageThreshold)
        {
            float damage = Mathf.Pow(currentGForce - gForceDamageThreshold, 2) * Time.deltaTime;
            ApplyDamage(damage);
        }
    }

    public void ApplyDamage(float amount)
    {
        structuralIntegrity = Mathf.Clamp(structuralIntegrity - amount, 0, maxStructuralIntegrity);
        
        // Play damage sound
        if (damageAudioSource && damageSound && amount > 5f)
        {
            damageAudioSource.PlayOneShot(damageSound);
        }
        
        // System failure effects
        if (structuralIntegrity < maxStructuralIntegrity * 0.3f)
        {
            // Random system failures
            if (Random.value < 0.01f) currentThrust *= 0.7f; // Engine damage
            if (Random.value < 0.02f) flyByWireEnabled = false; // FBW failure
        }

        if (structuralIntegrity <= 0 && !isDestroyed)
        {
            DestroyAircraft();
        }
    }

    private void CheckStructuralFailure()
    {
        // Wing stress damage
        float stress = Mathf.Abs(controlInput.x) * rb.linearVelocity.magnitude * 0.01f;
        ApplyDamage(stress * Time.deltaTime);
    }

    private void DestroyAircraft()
    {
        isDestroyed = true;
        rb.linearDamping = damageDragMultiplier;
        rb.angularDamping = damageDragMultiplier;
        
        if (explosionPrefab) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        
        // Play explosion sound
        if (damageAudioSource && explosionSound)
        {
            damageAudioSource.PlayOneShot(explosionSound);
        }
        
        // Disable control surfaces
        foreach (ControlSurface surface in controlSurfaces)
        {
            if (surface.surfaceTransform.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }
        }
        
        // Stop all audio
        if (engineAudioSource) engineAudioSource.Stop();
        if (afterburnerAudioSource) afterburnerAudioSource.Stop();
        if (stallAudioSource) stallAudioSource.Stop();
        
        // Disable systems
        enabled = false;
    }
    #endregion

    #region Stall Warning System
    private void UpdateStallWarning()
    {
        bool newStallState = angleOfAttack > stallWarningThreshold;
        
        if (newStallState != stallWarningActive)
        {
            stallWarningActive = newStallState;
            
            if (stallAudioSource)
            {
                if (stallWarningActive)
                {
                    if (!stallAudioSource.isPlaying) stallAudioSource.Play();
                }
                else
                {
                    stallAudioSource.Stop();
                }
            }
        }
    }
    #endregion

    #region Audio Management
    private void HandleAfterburner()
    {
        if (!afterburnerEffect) return;
        
        if (afterburnerActive && throttleInput > 0.95f && !afterburnerEffect.isPlaying)
        {
            afterburnerEffect.Play();
            if (afterburnerAudioSource) afterburnerAudioSource.Play();
        }
        else if ((!afterburnerActive || throttleInput <= 0.95f) && afterburnerEffect.isPlaying)
        {
            afterburnerEffect.Stop();
            if (afterburnerAudioSource) afterburnerAudioSource.Stop();
        }
    }

    private void UpdateEngineAudio()
    {
        if (!engineAudioSource) return;
        
        // Calculate engine parameters based on throttle and speed
        float speedFactor = rb.linearVelocity.magnitude / 100f;
        float throttleFactor = throttleInput;
        
        // Target values based on throttle and speed
        enginePitchTarget = 0.8f + throttleFactor * 0.6f + speedFactor * 0.2f;
        engineVolumeTarget = 0.5f + throttleFactor * 0.3f;
        
        // Afterburner effects
        if (afterburnerActive && throttleInput > 0.95f)
        {
            enginePitchTarget += 0.3f;
            engineVolumeTarget += 0.2f;
        }
        
        // Smooth audio transitions
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, enginePitchTarget, 5f * Time.deltaTime);
        engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, engineVolumeTarget, 5f * Time.deltaTime);
        
        // Damage effects
        if (structuralIntegrity < maxStructuralIntegrity * 0.5f)
        {
            engineAudioSource.pitch *= 1f + Mathf.PerlinNoise(Time.time * 10f, 0) * 0.2f;
            engineAudioSource.volume *= 0.8f + Mathf.PerlinNoise(Time.time * 8f, 10) * 0.2f;
        }
    }
    #endregion

    private void FixedUpdate()
    {
        if (isDestroyed) return;

        CalculateAerodynamics();
        ApplyThrust();
        ApplyFlightAssistance();
    }

    #region Input Handling
    public void OnPitchRoll(InputAction.CallbackContext context)
    {
        controlInput = context.ReadValue<Vector2>();
    }

    public void OnYaw(InputAction.CallbackContext context)
    {
        // Optional yaw input implementation
    }

    public void OnThrottle(InputAction.CallbackContext context)
    {
        throttleInput = context.ReadValue<float>();
    }

    public void OnAfterburner(InputAction.CallbackContext context)
    {
        afterburnerActive = context.performed;
    }
    #endregion

    // Public API for UI systems
    public float GetSpeed() => rb.linearVelocity.magnitude * 3.6f; // km/h
    public float GetAltitude() => transform.position.y;
    public float GetThrottle() => throttleInput;
    public float GetGForce() => currentGForce;
    public float GetStructuralIntegrity() => structuralIntegrity / maxStructuralIntegrity;
    public float GetAoA() => angleOfAttack;
    public float GetFuelPercentage() => currentFuel / fuelCapacity;
    public float GetMachNumber() => currentMach;
    public bool IsStallWarning() => stallWarningActive;
}

// Shockwave Controller for Sonic Boom Effect
public class ShockwaveController : MonoBehaviour
{
    private float expansionRate = 200f;
    private float maxSize = 500f;
    private float fadeRate = 2f;
    private Color initialColor;
    private Renderer shockwaveRenderer;
    private Vector3 direction;

    public void Initialize(Vector3 moveDirection)
    {
        direction = moveDirection.normalized;
        shockwaveRenderer = GetComponent<Renderer>();
        if (shockwaveRenderer)
        {
            initialColor = shockwaveRenderer.material.color;
            shockwaveRenderer.material = new Material(Shader.Find("Standard"));
            shockwaveRenderer.material.color = new Color(0.2f, 0.5f, 1f, 0.7f);
            shockwaveRenderer.material.SetFloat("_Mode", 3); // Transparent mode
            shockwaveRenderer.material.EnableKeyword("_EMISSION");
            shockwaveRenderer.material.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.8f));
        }
        Destroy(gameObject, 10f); // Auto-destroy after 10 seconds
    }

    private void Update()
    {
        // Expand shockwave
        transform.localScale += Vector3.one * expansionRate * Time.deltaTime;
        
        // Move in direction of aircraft
        transform.position += direction * 300f * Time.deltaTime;
        
        // Fade out
        if (shockwaveRenderer)
        {
            Color currentColor = shockwaveRenderer.material.color;
            currentColor.a = Mathf.Clamp01(currentColor.a - fadeRate * Time.deltaTime);
            shockwaveRenderer.material.color = currentColor;
            
            // Also fade emission
            Color emissionColor = shockwaveRenderer.material.GetColor("_EmissionColor");
            emissionColor.a = currentColor.a;
            shockwaveRenderer.material.SetColor("_EmissionColor", emissionColor);
            
            if (currentColor.a <= 0.01f)
            {
                Destroy(gameObject);
            }
        }
        
        // Destroy when too large
        if (transform.localScale.x > maxSize)
        {
            Destroy(gameObject);
        }
    }
}

// Missile Controller
public class Missile : MonoBehaviour
{
    [SerializeField] private float speed = 150f;
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float damageRadius = 10f;
    [SerializeField] private float damageAmount = 100f;

    private Rigidbody rb;
    private float launchTime;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        launchTime = Time.time;
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector3 inheritedVelocity)
    {
        rb.linearVelocity = inheritedVelocity;
        rb.AddForce(transform.forward * speed, ForceMode.VelocityChange);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    private void Explode()
    {
        // Create explosion effect
        if (explosionEffect) Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Apply damage in radius
        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (Collider hit in hits)
        {
            AdvancedAircraftController aircraft = hit.GetComponentInParent<AdvancedAircraftController>();
            if (aircraft)
            {
                aircraft.ApplyDamage(damageAmount);
            }
        }

        Destroy(gameObject);
    }
}
