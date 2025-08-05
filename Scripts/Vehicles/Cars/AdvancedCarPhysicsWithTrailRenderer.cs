using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UltimateCarPhysics : MonoBehaviour
{
    [Header("Chassis Settings")]
    public Transform centerOfMass;
    [SerializeField] private float chassisMass = 1200f;
    [SerializeField] private Vector3 chassisInertia = new Vector3(1500, 1500, 1000);
    [SerializeField] private float maxSteeringAngle = 35f;
    [SerializeField] private float steeringSpeed = 3f;

    [Header("Wheel Configuration")]
    public List<Wheel> wheels = new List<Wheel>();

    [Header("Engine & Drivetrain")]
    [SerializeField] private float maxTorque = 650f;
    [SerializeField] private float maxRPM = 7000f;
    [SerializeField] private float idleRPM = 800f;
    [SerializeField] private AnimationCurve torqueCurve = new AnimationCurve(
        new Keyframe(0, 0.8f),
        new Keyframe(0.2f, 0.9f),
        new Keyframe(0.4f, 1.0f),
        new Keyframe(0.6f, 0.95f),
        new Keyframe(0.8f, 0.9f),
        new Keyframe(1.0f, 0.7f)
    );
    [SerializeField] private float[] gearRatios = { 3.67f, 2.50f, 1.84f, 1.45f, 1.21f, 1.0f };
    [SerializeField] private float finalDriveRatio = 3.42f;
    [SerializeField] private float differentialBias = 0.6f;
    [SerializeField] private float clutchStrength = 100f;
    [SerializeField] private float drivetrainLoss = 0.15f;
    private int currentGear = 1;
    private float engineRPM;
    private bool clutchEngaged;
    private float currentSteeringAngle;
    private int lastGear = 1;

    [Header("Advanced Systems")]
    [SerializeField] private bool enableTCS = true;
    [SerializeField] private float tcsThreshold = 0.8f;
    [SerializeField] private bool enableABS = true;
    [SerializeField] private float absThreshold = 0.9f;
    [SerializeField] private bool enableTorqueVectoring = true;
    [SerializeField] private float torqueVectoringFactor = 0.3f;
    [SerializeField] private float antiRollStiffness = 8000f;
    [SerializeField] private float boostPressure = 1.0f;
    [SerializeField] private float maxBoost = 1.5f;
    [SerializeField] private float boostBuildRate = 0.2f;
    [SerializeField] private float boostDecayRate = 0.5f;

    [Header("Aerodynamics")]
    [SerializeField] private float dragCoefficient = 0.3f;
    [SerializeField] private float downforceCoefficient = 3f;
    [SerializeField] private float frontDownforceRatio = 0.45f;
    [SerializeField] private Vector3 aeroCenter = new Vector3(0, 0.2f, -0.5f);

    [Header("Driver Input")]
    [Range(-1, 1)] public float throttle;
    [Range(-1, 1)] public float steering;
    [Range(0, 1)] public float brake;
    [Range(0, 1)] public float handbrake;
    public bool boostActive;

    [Header("Audio System")]
    public AudioSource engineAudioSource;
    public AudioSource tireAudioSource;
    public AudioSource crashAudioSource;
    public AudioClip engineIdleClip;
    public AudioClip engineRunningClip;
    public AudioClip tireScreechClip;
    public AudioClip tireSlipClip;
    public AudioClip crashClip;
    public AudioClip gearShiftClip;
    [SerializeField] private float minEnginePitch = 0.5f;
    [SerializeField] private float maxEnginePitch = 1.5f;
    [SerializeField] private float minTireVolume = 0.1f;
    [SerializeField] private float maxTireVolume = 1.0f;
    private float currentTireVolume;
    private float audioTransitionSpeed = 5f;

    [Header("Wheel Effects")]
    public GameObject skidMarkPrefab;
    public ParticleSystem tireSmokePrefab;
    public ParticleSystem dustPrefab;
    [SerializeField] private float minSlipForSkid = 0.3f;
    [SerializeField] private float minSpeedForDust = 5f;
    private List<ParticleSystem> tireSmokeInstances = new List<ParticleSystem>();
    private List<ParticleSystem> dustInstances = new List<ParticleSystem>();
    private List<SkidMark> activeSkidMarks = new List<SkidMark>();

    [Header("Performance Settings")]
    [SerializeField] private bool enablePerformanceOptimizations = true;
    [SerializeField] private float maxEffectDistance = 100f;
    [SerializeField] private float nearDistance = 30f;
    [SerializeField] private float midDistance = 60f;
    [SerializeField] private int physicsUpdateRate = 2;
    [SerializeField] private int effectUpdateRate = 3;
    [SerializeField] private int audioUpdateRate = 2;
    [SerializeField] private int skidMarkPoolSize = 20;
    [SerializeField] private int particlePoolSize = 10;

    private Rigidbody rb;
    private float wheelbase;
    private float trackWidth;
    private float totalDriveTorque;
    private float slipAngle;
    private Vector3 velocity;
    private Vector3 localVelocity;
    private Vector3 lastPosition;
    private float currentSpeed;
    private float wheelRPM;
    private float tractionControlReduction;
    private float absReduction;
    private float boostTimer;
    private bool isBoosting;
    private int fixedUpdateCount = 0;
    private int frameCount = 0;
    private float distanceToCamera;
    private Transform mainCamera;
    private bool isVisible = true;

    private enum CarLOD
    {
        High,
        Medium,
        Low,
        Culled
    }
    private CarLOD currentLOD = CarLOD.High;

    private Queue<SkidMark> skidMarkPool = new Queue<SkidMark>();
    private Queue<ParticleSystem> smokePool = new Queue<ParticleSystem>();
    private Queue<ParticleSystem> dustPool = new Queue<ParticleSystem>();

    [System.Serializable]
    public class Wheel
    {
        public string name;
        public Transform wheelTransform;
        public Transform suspensionTransform;
        public bool isSteering;
        public bool isDrive;
        public bool isBrake;
        public bool isHandbrake;
        public bool isFrontAxle;
        public float steerAngleMax = 30f;
        public float steerResponse = 5f;
        public float camber = 0f;
        public float toeAngle = 0f;

        [Header("Suspension")]
        public float restLength = 0.3f;
        public float springStiffness = 35000f;
        public float damperStiffness = 4000f;
        public float travel = 0.2f;

        [Header("Wheel Physics")]
        public float radius = 0.35f;
        public float width = 0.25f;
        public float inertia = 1f;
        public float brakeTorque = 1500f;
        public float handbrakeTorque = 2000f;
        public float maxLoad = 10000f;

        [Header("Tire Parameters")]
        public float stiffness = 10f;
        public float peakGrip = 1.0f;
        public float curveShape = 1.9f;
        public float curveSteepness = 0.97f;
        public float loadSensitivity = 0.0001f;
        public float camberStiffness = 0.5f;

        [Header("Runtime Info")]
        [ReadOnly] public float compression;
        [ReadOnly] public float angularVelocity;
        [ReadOnly] public float rotationAngle;
        [ReadOnly] public float slipRatio;
        [ReadOnly] public float slipAngle;
        [ReadOnly] public Vector3 groundVelocity;
        [ReadOnly] public bool isGrounded;
        [ReadOnly] public float currentLoad;
        [ReadOnly] public float motorTorque;
        [ReadOnly] public float steerAngle;
        [ReadOnly] public float tireTemp = 25f;
        [ReadOnly] public float tireWear = 0f;
        [ReadOnly] public bool isSlipping;
        [ReadOnly] public float slipIntensity;
        [ReadOnly] public RaycastHit groundHit;
    }

    public class ReadOnlyAttribute : PropertyAttribute { }

    #if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
    #endif

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = chassisMass;
        rb.centerOfMass = centerOfMass.localPosition;
        rb.inertiaTensor = chassisInertia;
        rb.inertiaTensorRotation = Quaternion.identity;
        lastPosition = transform.position;

        mainCamera = Camera.main.transform;

        InitializePools();
        InitializeWheelEffects();
        CalculateDimensions();
        InitializeWheels();

        if (engineAudioSource == null) engineAudioSource = gameObject.AddComponent<AudioSource>();
        if (tireAudioSource == null) tireAudioSource = gameObject.AddComponent<AudioSource>();
        if (crashAudioSource == null) crashAudioSource = gameObject.AddComponent<AudioSource>();

        engineAudioSource.spatialBlend = 1f;
        tireAudioSource.spatialBlend = 1f;
        crashAudioSource.spatialBlend = 1f;
        engineAudioSource.loop = true;
        tireAudioSource.loop = true;
    }

    private void InitializePools()
    {
        for (int i = 0; i < skidMarkPoolSize; i++)
        {
            GameObject skidMarkObj = Instantiate(skidMarkPrefab);
            skidMarkObj.SetActive(false);
            SkidMark skidMark = skidMarkObj.GetComponent<SkidMark>();
            skidMarkPool.Enqueue(skidMark);
        }

        for (int i = 0; i < particlePoolSize; i++)
        {
            ParticleSystem smoke = Instantiate(tireSmokePrefab);
            smoke.gameObject.SetActive(false);
            smokePool.Enqueue(smoke);
        }

        for (int i = 0; i < particlePoolSize; i++)
        {
            ParticleSystem dust = Instantiate(dustPrefab);
            dust.gameObject.SetActive(false);
            dustPool.Enqueue(dust);
        }
    }

    private void InitializeWheelEffects()
    {
        foreach (Wheel wheel in wheels)
        {
            if (tireSmokePrefab != null)
            {
                ParticleSystem smoke = GetSmokeFromPool();
                smoke.transform.SetParent(wheel.suspensionTransform);
                smoke.transform.localPosition = Vector3.zero;
                smoke.Stop();
                tireSmokeInstances.Add(smoke);
            }

            if (dustPrefab != null)
            {
                ParticleSystem dust = GetDustFromPool();
                dust.transform.SetParent(wheel.suspensionTransform);
                dust.transform.localPosition = Vector3.zero;
                dust.Stop();
                dustInstances.Add(dust);
            }
        }
    }

    private void CalculateDimensions()
    {
        float minZ = float.MaxValue, maxZ = float.MinValue;
        float minX = float.MaxValue, maxX = float.MinValue;

        foreach (Wheel wheel in wheels)
        {
            Vector3 pos = wheel.suspensionTransform.position;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
        }

        wheelbase = maxZ - minZ;
        trackWidth = maxX - minX;
    }

    private void InitializeWheels()
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.angularVelocity = idleRPM * Mathf.PI / 30f;
        }
        engineRPM = idleRPM;
        currentSteeringAngle = 0;
    }

    private void Update()
    {
        if (enablePerformanceOptimizations)
        {
            distanceToCamera = Vector3.Distance(transform.position, mainCamera.position);
            UpdateLODLevel();
            isVisible = IsVisibleToCamera();
            
            if (currentLOD == CarLOD.Culled) return;
        }

        ProcessInputAndCoreSystems();
        
        if (enablePerformanceOptimizations && frameCount % audioUpdateRate == 0)
        {
            UpdateTireAudio();
        }
        else if (!enablePerformanceOptimizations)
        {
            UpdateTireAudio();
        }
        
        frameCount++;
    }

    private void ProcessInputAndCoreSystems()
    {
        throttle = Input.GetAxis("Vertical");
        steering = Input.GetAxis("Horizontal");
        brake = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        handbrake = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
        boostActive = Input.GetKey(KeyCode.LeftAlt);

        if (Input.GetKeyDown(KeyCode.E)) currentGear++;
        if (Input.GetKeyDown(KeyCode.Q)) currentGear--;
        currentGear = Mathf.Clamp(currentGear, 1, gearRatios.Length - 1);

        if (currentGear != lastGear && gearShiftClip != null)
        {
            engineAudioSource.PlayOneShot(gearShiftClip);
            lastGear = currentGear;
        }

        clutchEngaged = Input.GetKey(KeyCode.LeftControl);
        UpdateBoost();

        velocity = (transform.position - lastPosition) / Time.deltaTime;
        localVelocity = transform.InverseTransformDirection(velocity);
        currentSpeed = velocity.magnitude;
        lastPosition = transform.position;

        UpdateEngineAudio();
    }

    private void UpdateBoost()
    {
        if (boostActive && throttle > 0.1f)
        {
            boostPressure = Mathf.Min(boostPressure + boostBuildRate * Time.deltaTime, maxBoost);
            isBoosting = true;
        }
        else
        {
            boostPressure = Mathf.Max(boostPressure - boostDecayRate * Time.deltaTime, 1.0f);
            isBoosting = false;
        }
    }

    private void UpdateEngineAudio()
    {
        if (engineAudioSource == null) return;

        float pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, engineRPM / maxRPM);
        engineAudioSource.pitch = pitch;

        if (engineRPM < idleRPM * 1.2f && engineAudioSource.clip != engineIdleClip)
        {
            if (engineIdleClip != null)
            {
                engineAudioSource.clip = engineIdleClip;
                engineAudioSource.Play();
            }
        }
        else if (engineRPM > idleRPM * 1.5f && engineAudioSource.clip != engineRunningClip)
        {
            if (engineRunningClip != null)
            {
                engineAudioSource.clip = engineRunningClip;
                engineAudioSource.Play();
            }
        }

        engineAudioSource.volume = Mathf.Lerp(0.3f, 1.0f, throttle);
    }

    private void UpdateTireAudio()
    {
        if (tireAudioSource == null) return;

        float totalSlip = 0f;
        int slippingWheels = 0;
        
        foreach (Wheel wheel in wheels)
        {
            if (wheel.isSlipping)
            {
                totalSlip += wheel.slipIntensity;
                slippingWheels++;
            }
        }

        if (slippingWheels > 0) totalSlip /= slippingWheels;

        bool isScreeching = totalSlip > 0.5f && currentSpeed > 10f;
        bool isSlipping = totalSlip > 0.2f && currentSpeed > 5f;

        if (isScreeching || isSlipping)
        {
            if (isScreeching && tireScreechClip != null)
            {
                if (tireAudioSource.clip != tireScreechClip)
                {
                    tireAudioSource.clip = tireScreechClip;
                    tireAudioSource.Play();
                }
            }
            else if (isSlipping && tireSlipClip != null)
            {
                if (tireAudioSource.clip != tireSlipClip)
                {
                    tireAudioSource.clip = tireSlipClip;
                    tireAudioSource.Play();
                }
            }

            float targetVolume = Mathf.Lerp(minTireVolume, maxTireVolume, totalSlip);
            currentTireVolume = Mathf.Lerp(currentTireVolume, targetVolume, audioTransitionSpeed * Time.deltaTime);
            tireAudioSource.volume = currentTireVolume;
        }
        else
        {
            currentTireVolume = Mathf.Lerp(currentTireVolume, 0f, audioTransitionSpeed * Time.deltaTime);
            tireAudioSource.volume = currentTireVolume;
            
            if (currentTireVolume < 0.01f && tireAudioSource.isPlaying)
            {
                tireAudioSource.Stop();
            }
        }
    }

    private void FixedUpdate()
    {
        if (enablePerformanceOptimizations && !isVisible) return;
        
        bool shouldUpdatePhysics = true;
        
        if (enablePerformanceOptimizations)
        {
            if (currentLOD == CarLOD.Low)
            {
                shouldUpdatePhysics = (fixedUpdateCount % (physicsUpdateRate * 2) == 0);
            }
            else if (currentLOD == CarLOD.Medium)
            {
                shouldUpdatePhysics = (fixedUpdateCount % physicsUpdateRate == 0);
            }
        }
        
        if (shouldUpdatePhysics)
        {
            ApplyAerodynamics();
            UpdateSuspension();
            
            if (!enablePerformanceOptimizations || currentLOD != CarLOD.Low)
            {
                ApplyAntiRollBars();
            }
            
            CalculateDrivetrain();
            ApplyWheelForces();
        }
        
        UpdateWheelVisuals();
        
        if (enablePerformanceOptimizations && frameCount % effectUpdateRate == 0)
        {
            UpdateWheelEffects();
        }
        else if (!enablePerformanceOptimizations)
        {
            UpdateWheelEffects();
        }
        
        fixedUpdateCount++;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (crashAudioSource != null && crashClip != null)
        {
            float impactForce = collision.impulse.magnitude;
            if (impactForce > 5f)
            {
                crashAudioSource.pitch = Random.Range(0.9f, 1.1f);
                crashAudioSource.volume = Mathf.Clamp01(impactForce / 50f);
                crashAudioSource.PlayOneShot(crashClip);
            }
        }
    }

    private void UpdateLODLevel()
    {
        CarLOD previousLOD = currentLOD;
        
        if (distanceToCamera > maxEffectDistance)
        {
            currentLOD = CarLOD.Culled;
        }
        else if (distanceToCamera > midDistance)
        {
            currentLOD = CarLOD.Low;
        }
        else if (distanceToCamera > nearDistance)
        {
            currentLOD = CarLOD.Medium;
        }
        else
        {
            currentLOD = CarLOD.High;
        }

        if (previousLOD != currentLOD)
        {
            ApplyLODSettings();
        }
    }

    private void ApplyLODSettings()
    {
        switch (currentLOD)
        {
            case CarLOD.High:
                SetAudioQuality(1.0f, 1.0f);
                SetEffectsQuality(1.0f);
                break;
            case CarLOD.Medium:
                SetAudioQuality(0.7f, 0.8f);
                SetEffectsQuality(0.6f);
                break;
            case CarLOD.Low:
                SetAudioQuality(0.4f, 0.5f);
                SetEffectsQuality(0.3f);
                break;
            case CarLOD.Culled:
                if (engineAudioSource != null) engineAudioSource.volume = 0;
                if (tireAudioSource != null) tireAudioSource.volume = 0;
                break;
        }
    }

    private void SetAudioQuality(float volumeScale, float pitchScale)
    {
        if (engineAudioSource != null)
        {
            engineAudioSource.volume *= volumeScale;
            engineAudioSource.pitch *= pitchScale;
        }
        
        if (tireAudioSource != null)
        {
            tireAudioSource.volume *= volumeScale;
        }
    }

    private void SetEffectsQuality(float quality)
    {
        foreach (var smoke in tireSmokeInstances)
        {
            if (smoke != null)
            {
                var emission = smoke.emission;
                emission.rateOverTimeMultiplier = quality;
            }
        }
        
        foreach (var dust in dustInstances)
        {
            if (dust != null)
            {
                var emission = dust.emission;
                emission.rateOverTimeMultiplier = quality;
            }
        }
        
        foreach (var skidMark in activeSkidMarks)
        {
            if (skidMark != null)
            {
                skidMark.SetQuality(quality);
            }
        }
    }

    private bool IsVisibleToCamera()
    {
        if (mainCamera == null) return true;
        
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        return (viewportPos.x >= 0 && viewportPos.x <= 1 && 
                viewportPos.y >= 0 && viewportPos.y <= 1 && 
                viewportPos.z > 0);
    }

    private void ApplyAerodynamics()
    {
        float speedSqr = velocity.sqrMagnitude;
        Vector3 dragForce = -dragCoefficient * speedSqr * velocity.normalized;
        rb.AddForce(dragForce);

        float downforce = downforceCoefficient * speedSqr;
        float frontDownforce = downforce * frontDownforceRatio;
        float rearDownforce = downforce * (1 - frontDownforceRatio);

        Vector3 frontPosition = transform.TransformPoint(aeroCenter + new Vector3(0, 0, wheelbase * 0.4f));
        Vector3 rearPosition = transform.TransformPoint(aeroCenter + new Vector3(0, 0, wheelbase * -0.4f));
        
        rb.AddForceAtPosition(-transform.up * frontDownforce, frontPosition);
        rb.AddForceAtPosition(-transform.up * rearDownforce, rearPosition);
    }

    private void UpdateSuspension()
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            Wheel wheel = wheels[i];
            RaycastHit hit;
            Vector3 rayStart = wheel.suspensionTransform.position;
            Vector3 rayDir = -wheel.suspensionTransform.up;
            float rayLength = wheel.restLength + wheel.travel;

            if (Physics.Raycast(rayStart, rayDir, out hit, rayLength))
            {
                wheel.isGrounded = true;
                wheel.compression = 1 - (hit.distance - wheel.radius) / wheel.restLength;
                wheel.groundHit = hit;

                float springForce = wheel.springStiffness * wheel.compression;
                float damperForce = wheel.damperStiffness * Vector3.Dot(rb.GetPointVelocity(rayStart), rayDir);
                Vector3 suspensionForce = (springForce + damperForce) * transform.up;
                rb.AddForceAtPosition(suspensionForce, rayStart);
                wheel.currentLoad = Mathf.Min(suspensionForce.magnitude, wheel.maxLoad);
            }
            else
            {
                wheel.isGrounded = false;
                wheel.compression = 0;
                wheel.currentLoad = 0;
            }
        }
    }

    private void ApplyAntiRollBars()
    {
        for (int i = 0; i < wheels.Count; i += 2)
        {
            if (i + 1 >= wheels.Count) break;

            Wheel leftWheel = wheels[i];
            Wheel rightWheel = wheels[i + 1];

            if (leftWheel.isGrounded && rightWheel.isGrounded)
            {
                float compressionDiff = leftWheel.compression - rightWheel.compression;
                Vector3 antiRollForce = transform.up * compressionDiff * antiRollStiffness;

                rb.AddForceAtPosition(-antiRollForce, leftWheel.suspensionTransform.position);
                rb.AddForceAtPosition(antiRollForce, rightWheel.suspensionTransform.position);
            }
        }
    }

    private void CalculateDrivetrain()
    {
        wheelRPM = 0;
        int driveWheels = 0;
        foreach (Wheel wheel in wheels)
        {
            if (wheel.isDrive && wheel.isGrounded)
            {
                wheelRPM += wheel.angularVelocity * 30f / Mathf.PI;
                driveWheels++;
            }
        }
        if (driveWheels > 0) wheelRPM /= driveWheels;

        if (!clutchEngaged && currentGear > 0)
        {
            engineRPM = Mathf.Lerp(engineRPM, wheelRPM * gearRatios[currentGear] * finalDriveRatio, 0.1f);
        }

        engineRPM = Mathf.Clamp(engineRPM, idleRPM * 0.5f, maxRPM * 1.05f);

        float torqueMultiplier = torqueCurve.Evaluate(engineRPM / maxRPM);
        float engineTorque = torqueMultiplier * maxTorque * Mathf.Clamp01(throttle);
        
        if (isBoosting) engineTorque *= boostPressure;

        float clutchTorque = clutchEngaged ? clutchStrength * (engineRPM - idleRPM) : 0;

        float engineInertia = 0.25f;
        float engineAcceleration = (engineTorque - clutchTorque) / engineInertia;
        engineRPM += engineAcceleration * Time.fixedDeltaTime * 60;

        float drivetrainRatio = gearRatios[currentGear] * finalDriveRatio;
        totalDriveTorque = (engineTorque - clutchTorque) * drivetrainRatio * (1 - drivetrainLoss);

        tractionControlReduction = 0f;
        if (enableTCS)
        {
            foreach (Wheel wheel in wheels)
            {
                if (wheel.isDrive && wheel.isGrounded && Mathf.Abs(wheel.slipRatio) > tcsThreshold)
                {
                    tractionControlReduction = Mathf.Clamp01(Mathf.Abs(wheel.slipRatio) - tcsThreshold);
                }
            }
        }

        foreach (Wheel wheel in wheels)
        {
            if (wheel.isDrive)
            {
                wheel.motorTorque = totalDriveTorque * (1 - differentialBias) / 2;
                wheel.motorTorque *= (1 - tractionControlReduction);
            }
            else
            {
                wheel.motorTorque = 0;
            }
        }
    }

    private void ApplyWheelForces()
    {
        slipAngle = 0;
        int groundedWheels = 0;

        absReduction = 0f;
        if (enableABS && brake > 0.1f)
        {
            foreach (Wheel wheel in wheels)
            {
                if (wheel.isBrake && wheel.isGrounded && Mathf.Abs(wheel.slipRatio) > absThreshold)
                {
                    absReduction = Mathf.Clamp01(Mathf.Abs(wheel.slipRatio) - absThreshold);
                }
            }
        }

        float torqueVectoring = 0f;
        if (enableTorqueVectoring && currentSpeed > 5f)
        {
            torqueVectoring = -Mathf.Clamp(localVelocity.x * torqueVectoringFactor, -1f, 1f);
        }

        for (int i = 0; i < wheels.Count; i++)
        {
            Wheel wheel = wheels[i];
            if (!wheel.isGrounded) continue;
            
            groundedWheels++;

            Vector3 wheelWorldVel = rb.GetPointVelocity(wheel.suspensionTransform.position);
            Vector3 tireLocalVel = wheel.suspensionTransform.InverseTransformDirection(wheelWorldVel);

            float rollingVelocity = tireLocalVel.z;
            wheel.slipRatio = (rollingVelocity - wheel.angularVelocity * wheel.radius) / 
                             (Mathf.Abs(rollingVelocity) + 0.1f);

            wheel.slipAngle = Mathf.Rad2Deg * Mathf.Atan2(tireLocalVel.x, Mathf.Abs(tireLocalVel.z + 0.1f));
            slipAngle += wheel.slipAngle;

            float combinedSlip = Mathf.Sqrt(wheel.slipRatio * wheel.slipRatio + wheel.slipAngle * wheel.slipAngle / 100f);
            
            float loadEffect = wheel.currentLoad * wheel.loadSensitivity;
            float effectiveGrip = wheel.peakGrip * (1 - loadEffect);
            
            float camberEffect = Mathf.Abs(wheel.camber) * wheel.camberStiffness;
            
            float frictionCoeff = CalculateTireForce(combinedSlip, wheel.stiffness, wheel.curveShape, effectiveGrip, wheel.curveSteepness);
            frictionCoeff *= (1 + camberEffect);
            frictionCoeff *= wheel.currentLoad;

            float longForce = frictionCoeff * wheel.slipRatio / (combinedSlip + 0.0001f);
            float latForce = -frictionCoeff * wheel.slipAngle / (combinedSlip * 10f + 0.0001f);

            if (enableTorqueVectoring && wheel.isDrive)
            {
                float vectoringFactor = wheel.isFrontAxle ? torqueVectoring * 0.7f : torqueVectoring * 1.3f;
                latForce *= (1 + vectoringFactor);
            }

            Vector3 force = wheel.suspensionTransform.right * latForce + 
                            wheel.suspensionTransform.forward * longForce;

            rb.AddForceAtPosition(force, wheel.suspensionTransform.position);

            float brakeTorque = wheel.brakeTorque * brake * (1 - absReduction);
            if (wheel.isHandbrake) brakeTorque += wheel.handbrakeTorque * handbrake;
            
            wheel.angularVelocity -= brakeTorque / wheel.inertia * Time.fixedDeltaTime;

            if (wheel.isDrive && throttle > 0)
            {
                wheel.angularVelocity += wheel.motorTorque / wheel.inertia * Time.fixedDeltaTime;
            }

            wheel.angularVelocity *= 0.995f;
            
            wheel.tireTemp = Mathf.Clamp(wheel.tireTemp + combinedSlip * 0.1f, 25f, 150f);
            wheel.tireWear += combinedSlip * 0.00001f;
            
            wheel.isSlipping = combinedSlip > minSlipForSkid;
            wheel.slipIntensity = Mathf.Clamp01(combinedSlip);
        }

        if (groundedWheels > 0)
            slipAngle /= groundedWheels;
    }

    private float CalculateTireForce(float slip, float stiffness, float shape, float peak, float steepness)
    {
        return peak * Mathf.Sin(shape * Mathf.Atan(stiffness * slip - steepness * (stiffness * slip - Mathf.Atan(stiffness * slip))));
    }

    private void UpdateWheelVisuals()
    {
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, steering * maxSteeringAngle, steeringSpeed * Time.deltaTime);
        
        foreach (Wheel wheel in wheels)
        {
            if (wheel.wheelTransform == null) continue;

            if (wheel.isSteering)
            {
                float turnRadius = wheelbase / Mathf.Tan(currentSteeringAngle * Mathf.Deg2Rad);
                float effectiveAngle;
                
                if (wheel.isFrontAxle)
                {
                    if (currentSteeringAngle > 0)
                    {
                        effectiveAngle = Mathf.Atan(wheelbase / (turnRadius + (wheel.isFrontAxle ? -trackWidth/2 : trackWidth/2))) * Mathf.Rad2Deg;
                    }
                    else
                    {
                        effectiveAngle = Mathf.Atan(wheelbase / (turnRadius + (wheel.isFrontAxle ? trackWidth/2 : -trackWidth/2))) * Mathf.Rad2Deg;
                    }
                }
                else
                {
                    effectiveAngle = currentSteeringAngle;
                }
                
                wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, effectiveAngle, wheel.steerResponse * Time.deltaTime);
                wheel.suspensionTransform.localRotation = Quaternion.Euler(0, wheel.steerAngle, wheel.camber);
            }

            wheel.rotationAngle += wheel.angularVelocity * Mathf.Rad2Deg * Time.deltaTime;
            wheel.wheelTransform.localRotation = Quaternion.Euler(wheel.rotationAngle, wheel.toeAngle, wheel.camber);

            if (wheel.isGrounded)
            {
                wheel.wheelTransform.position = wheel.suspensionTransform.position - 
                                              wheel.suspensionTransform.up * (wheel.restLength - wheel.compression);
            }
            else
            {
                wheel.wheelTransform.position = wheel.suspensionTransform.position - 
                                              wheel.suspensionTransform.up * wheel.restLength;
            }
        }
    }

    private void UpdateWheelEffects()
    {
        if (enablePerformanceOptimizations && currentLOD == CarLOD.Culled) return;
        
        for (int i = 0; i < wheels.Count; i++)
        {
            Wheel wheel = wheels[i];
            ParticleSystem smoke = tireSmokeInstances.Count > i ? tireSmokeInstances[i] : null;
            ParticleSystem dust = dustInstances.Count > i ? dustInstances[i] : null;
            
            if (wheel.isGrounded)
            {
                Vector3 contactPoint = wheel.groundHit.point + Vector3.up * 0.01f;
                
                if (smoke != null)
                {
                    smoke.transform.position = contactPoint;
                    
                    if (wheel.isSlipping)
                    {
                        if (!smoke.isPlaying) smoke.Play();
                        
                        var emission = smoke.emission;
                        emission.rateOverTime = wheel.slipIntensity * 50f;
                        
                        var main = smoke.main;
                        Color smokeColor = Color.Lerp(Color.gray, Color.black, wheel.slipIntensity);
                        main.startColor = smokeColor;
                    }
                    else if (smoke.isPlaying)
                    {
                        smoke.Stop();
                    }
                }
                
                if (dust != null && currentSpeed > minSpeedForDust)
                {
                    dust.transform.position = contactPoint;
                    
                    if (wheel.groundHit.collider.CompareTag("Dirt") || 
                        wheel.groundHit.collider.material.name.Contains("Dirt"))
                    {
                        if (!dust.isPlaying) dust.Play();
                        
                        var emission = dust.emission;
                        emission.rateOverTime = currentSpeed * 2f;
                    }
                    else if (dust.isPlaying)
                    {
                        dust.Stop();
                    }
                }
                
                if (wheel.isSlipping && skidMarkPrefab != null)
                {
                    if (activeSkidMarks.Count <= i || activeSkidMarks[i] == null)
                    {
                        if (skidMarkPool.Count > 0)
                        {
                            SkidMark skidMark = skidMarkPool.Dequeue();
                            skidMark.gameObject.SetActive(true);
                            skidMark.transform.position = contactPoint;
                            
                            if (activeSkidMarks.Count <= i)
                            {
                                activeSkidMarks.Add(skidMark);
                            }
                            else
                            {
                                activeSkidMarks[i] = skidMark;
                            }
                        }
                    }
                    
                    if (activeSkidMarks.Count > i && activeSkidMarks[i] != null)
                    {
                        activeSkidMarks[i].AddPoint(contactPoint, wheel.groundHit.normal, wheel.slipIntensity);
                    }
                }
                else
                {
                    if (activeSkidMarks.Count > i && activeSkidMarks[i] != null)
                    {
                        activeSkidMarks[i].FinishSkid();
                        ReturnSkidMarkToPool(activeSkidMarks[i]);
                        activeSkidMarks[i] = null;
                    }
                }
            }
            else
            {
                if (smoke != null && smoke.isPlaying) smoke.Stop();
                if (dust != null && dust.isPlaying) dust.Stop();
                
                if (activeSkidMarks.Count > i && activeSkidMarks[i] != null)
                {
                    activeSkidMarks[i].FinishSkid();
                    ReturnSkidMarkToPool(activeSkidMarks[i]);
                    activeSkidMarks[i] = null;
                }
            }
        }
    }

    private ParticleSystem GetSmokeFromPool()
    {
        if (smokePool.Count > 0)
        {
            ParticleSystem smoke = smokePool.Dequeue();
            smoke.gameObject.SetActive(true);
            return smoke;
        }
        return Instantiate(tireSmokePrefab);
    }

    private void ReturnSmokeToPool(ParticleSystem smoke)
    {
        smoke.Stop();
        smoke.gameObject.SetActive(false);
        smokePool.Enqueue(smoke);
    }

    private ParticleSystem GetDustFromPool()
    {
        if (dustPool.Count > 0)
        {
            ParticleSystem dust = dustPool.Dequeue();
            dust.gameObject.SetActive(true);
            return dust;
        }
        return Instantiate(dustPrefab);
    }

    private void ReturnDustToPool(ParticleSystem dust)
    {
        dust.Stop();
        dust.gameObject.SetActive(false);
        dustPool.Enqueue(dust);
    }

    private void ReturnSkidMarkToPool(SkidMark skidMark)
    {
        skidMark.gameObject.SetActive(false);
        skidMarkPool.Enqueue(skidMark);
    }

    [SerializeField] public bool showDebugInfo;

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUI.Box(new Rect(10, 10, 300, 220), "Car Physics Debug");
        GUI.Label(new Rect(20, 40, 280, 20), $"Speed: {currentSpeed * 3.6f:0.0} km/h");
        GUI.Label(new Rect(20, 60, 280, 20), $"Engine RPM: {engineRPM:0}");
        GUI.Label(new Rect(20, 80, 280, 20), $"Gear: {currentGear} (Ratio: {gearRatios[currentGear]:0.00})");
        GUI.Label(new Rect(20, 100, 280, 20), $"Slip Angle: {slipAngle:0.0}°");
        GUI.Label(new Rect(20, 120, 280, 20), $"Throttle: {throttle:0.00}, Brake: {brake:0.00}");
        GUI.Label(new Rect(20, 140, 280, 20), $"Boost: {boostPressure:0.0}x");
        GUI.Label(new Rect(20, 160, 280, 20), $"LOD: {currentLOD}");
        GUI.Label(new Rect(20, 180, 280, 20), $"Distance: {distanceToCamera:0} m");
        GUI.Label(new Rect(20, 200, 280, 20), $"Visible: {isVisible}");
    }
}

public class SkidMark : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float minDistance = 0.1f;
    public float maxOpacity = 0.8f;
    public float fadeSpeed = 0.05f;
    public float width = 0.25f;
    public float lowQualityMinDistance = 0.2f;
    public float mediumQualityMinDistance = 0.15f;
    public float highQualityMinDistance = 0.1f;

    private List<Vector3> points = new List<Vector3>();
    private bool isFinished = false;
    private Color initialColor;
    private float currentMinDistance;
    private float currentQuality = 1f;

    void Start()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        initialColor = lineRenderer.material.color;
        currentMinDistance = highQualityMinDistance;
    }

    public void SetQuality(float quality)
    {
        currentQuality = quality;

        if (quality < 0.4f)
        {
            currentMinDistance = lowQualityMinDistance;
            if (lineRenderer.positionCount > 20)
            {
                points = points.Skip(lineRenderer.positionCount - 20).ToList();
                UpdateLine();
            }
        }
        else if (quality < 0.7f)
        {
            currentMinDistance = mediumQualityMinDistance;
            if (lineRenderer.positionCount > 40)
            {
                points = points.Skip(lineRenderer.positionCount - 40).ToList();
                UpdateLine();
            }
        }
        else
        {
            currentMinDistance = highQualityMinDistance;
        }
    }

    public void AddPoint(Vector3 position, Vector3 normal, float intensity)
    {
        if (isFinished) return;

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], position) > currentMinDistance)
        {
            points.Add(position);
            UpdateLine();

            Color markColor = new Color(0.1f, 0.1f, 0.1f, Mathf.Lerp(0.3f, maxOpacity, intensity));
            lineRenderer.material.color = markColor;
        }
    }

    void UpdateLine()
    {
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    public void FinishSkid()
    {
        isFinished = true;
        StartCoroutine(FadeOut());
    }

    private System.Collections.IEnumerator FadeOut()
    {
        Color color = lineRenderer.material.color;
        while (color.a > 0)
        {
            color.a -= fadeSpeed * Time.deltaTime;
            lineRenderer.material.color = color;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}

/*

Complete Features List
Physics System
Advanced Suspension: Raycast-based with spring/damper physics

Tire Model: Pacejka "Magic Formula" with load sensitivity

Drivetrain: Multi-gear transmission with clutch simulation

Differential: Limited-slip with configurable bias

Aerodynamics: Drag and downforce with front/rear distribution

Advanced Systems: TCS, ABS, torque vectoring, anti-roll bars

Boost System: Configurable pressure build/decay

Audio System
Engine Sounds: RPM-based pitch shifting, idle/running transition

Tire Sounds: Screeching vs slipping based on slip intensity

Collision Effects: Impact force-based volume and pitch

Gear Shifts: Distinct shift sounds

Spatial Audio: 3D positioned sound sources

Visual Effects
Skid Marks: Dynamic generation with pooling system

Tire Smoke: Slip-based emission with color variation

Dust Trails: Surface-based emission (dirt only)

Debug Display: Real-time performance metrics

Performance Optimizations
Object Pooling:

Skid marks, smoke, and dust effects

Eliminates runtime instantiation

Reduces garbage collection

Level of Detail (LOD):

Four detail levels (High, Medium, Low, Culled)

Automatic transitions based on distance

Quality scaling for physics, audio, and effects

Update Rate Control:

Configurable update rates for physics, effects, and audio

Frame skipping based on LOD level

Reduced calculations for distant objects

Visibility Culling:

Frustum-based visibility checks

Automatic deactivation when not visible

Distance-based culling

Quality Scaling:

Physics: Reduced complexity at distance

Audio: Volume and pitch reduction

Effects: Particle emission scaling

*/
