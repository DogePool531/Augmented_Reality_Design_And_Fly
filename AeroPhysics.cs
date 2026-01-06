using UnityEngine;

/// <summary>
/// AeroPhysics:
/// - Reads aerodynamic geometry & coefficients from AeroModel
/// - Reads control input / tilt from PivotAroundBaseTransformer
/// - Caches everything for later use by the actual flight physics
/// 
/// DOES NOT:
/// - Apply forces/torques yet
/// - Modify mass/inertia yet
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AeroPhysics : MonoBehaviour
{
    [Header("References")]

    public PhysicsMaterial wheel;
    public AeroModel aeroModel;                         // Geometry & aero data
    public Rigidbody rb;                                // Physics body
    public PivotAroundBaseTransformer controlStick;
    public ControlPanel controlPanel;     // Input source (joystick / pivot handle)

    [Header("Cached Aero Inputs (from AeroModel)")]
    // Wing

    public Transform wingCenter;
    public float wingArea;
    public float wingspan;
    public float wingAspectRatio;
    public float wingK;
    public float wingCl_a;
    public float wingCd_0;

    // Horizontal tail
    public Transform tailCenter;
    public float horiArea;
    public float horispan;
    public float horiAspectRatio;
    public float horik;
    public float horiCl_a;

    // Vertical tail
    public float vertArea;
    public float vertspan;
    public float vertAspectRatio;
    public float vertk;
    public float vertCl_a;

    public float CG;

    [Header("Cached Control Inputs (from PivotAroundBaseTransformer)")]
    // Raw joystick output from the pivot script (X = roll, Y = pitch, both -1..1)
    public Vector2 joystickInput;        // (roll, pitch)

    // Convenience decomposed values
    public float rollCommand;            // [-1, 1] from joystickInput.x
    public float pitchCommand;           // [-1, 1] from joystickInput.y

    // Extra debug info from the pivot
    public float currentTiltAngle;       // Deg, scalar
    public Vector3 tiltDirection;        // World-space tilt direction

    [Header("Future Physics Config")]
    public float referenceDensity = 1.225f;  // kg/m^3, example
    public float referenceSpeed = 20f;       // m/s, example
    public bool autoConfigureInertiaFromGeometry = true;

    public float aoa;
    public float WingCL;

    public float liftL;
    public float dragL;
    public float liftR;
    public float dragR;
    public float tailForce;
    public float tailDrag;


    public Vector3 startPos;
    public Quaternion startRot;
    public float RollAuthority = 1f;
    public float PitchAuthority = 10f;

    public float yawStabilityFactor = 0.1f;
    public float thrustScale = 0.7f;
    public Transform ThrottlePos;
    public float speed;

    public float Scale;
    public float ThrustPercentage;
    public float thrust;
    public float scaleRange = 20;
    public Vector3 I;
     public float incedence;

        private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        aeroModel = GetComponent<AeroModel>();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (aeroModel == null)
            aeroModel = GetComponent<AeroModel>();
    }

    private void Start()
    {
        // On start, grab a snapshot from the aero model.
        Vector3 I = rb.inertiaTensor;
        BakeFromModel();
        rb.AddForce(new Vector3(10,0,0));
        startPos = transform.localPosition;
        startRot = transform.localRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
       
        // We’ll configure inertia/mass later when we actually design that model.
        // ConfigurePhysicsFromModel();
    }

    /// <summary>
    /// Reads the current aerodynamic geometry and coefficients from AeroModel
    /// and caches them locally. Call this right before "launch" to lock in the
    /// physics configuration based on the edited airplane shape.
    /// </summary>
    public void BakeFromModel()
    {
        if (aeroModel == null)
        {
            Debug.LogWarning("AeroPhysics: No AeroModel assigned.");
            return;
        }

        // --- Wing ---
        wingArea        = aeroModel.wingArea;
        wingspan        = aeroModel.wingspan;
        wingAspectRatio = aeroModel.AspectRatio;
        wingK           = aeroModel.k;
        wingCl_a        = aeroModel.Cl_a;
        wingCd_0        = aeroModel.Cd_0;

        // --- Horizontal tail ---
        horiArea         = aeroModel.horiArea;
        horispan         = aeroModel.horispan;
        horiAspectRatio  = aeroModel.horiAspectRatio;
        horik            = aeroModel.horik;
        horiCl_a         = aeroModel.horiCl_a;
        incedence        = aeroModel.incedence;
       
        // --- Vertical tail ---
        vertArea         = aeroModel.vertArea;
        vertspan         = aeroModel.vertspan;
        vertAspectRatio  = aeroModel.vertAspectRatio;
        vertk            = aeroModel.vertk;
        vertCl_a         = aeroModel.vertCl_a;

        // -- other --
        
        Scale = scaleRange/(controlPanel.displacement * (scaleRange-1) + 1);
        rb.centerOfMass = new Vector3(0,0,aeroModel.CG);
        rb.mass = Mathf.Max(0.1f, aeroModel.totalMass) * Scale * Scale * Scale;
        
        //rb.inertiaTensor = I * Scale * Scale;       
    
    }

    /// <summary>
    /// Reads current control stick state from PivotAroundBaseTransformer and
    /// caches normalized commands (roll/pitch) for later use.
    /// </summary>
    public void SyncInputFromControlStick()
    {
        if (controlStick == null)
        {
            joystickInput = Vector2.zero;
            rollCommand   = 0f;
            pitchCommand  = 0f;
            currentTiltAngle = 0f;
            tiltDirection = Vector3.zero;
            return;
        }

        // Copy over values that PivotAroundBaseTransformer is already computing.
        joystickInput    = controlStick.joystickInput;
        rollCommand      = joystickInput.x;  // X = roll
        pitchCommand     = joystickInput.y;  // Y = pitch
        currentTiltAngle = controlStick.currentTiltAngle;
        tiltDirection    = controlStick.tiltDirection;
    }

    /// <summary>
    /// Future hook: configure mass / inertia tensor based on geometry.
    /// Not implemented yet – placeholder for your inertia model.
    /// </summary>
    public void ConfigurePhysicsFromModel()
    {
        if (!autoConfigureInertiaFromGeometry || rb == null)
            return;

        // TODO (later):
        // Use wingArea, wingspan, fuselage lengths, etc. to estimate Ix, Iy, Iz
        // rb.inertiaTensor = new Vector3(Ix, Iy, Iz);
        // Possibly adjust rb.mass based on overall size/volume.
    }
    
    private (float Cl, float Cd) NACA12(float aoa, float a_0)
    {
        float Cl = 0f;
        float aoaLimit = 20f;

    if (Mathf.Abs(aoa) > aoaLimit)
    {
        Cl = 0f;
    }
    else
    {
        Cl = aoa * a_0 * Mathf.Deg2Rad
         - Mathf.Sign(aoa) * (Mathf.Exp(0.000006f * Mathf.Pow(aoa, 4)) - 1f);
    }

    float Cd = Mathf.Min(
    0.3f * (Mathf.Exp(0.00001f * Mathf.Pow(aoa, 4f)) - 1f)
    + 0.007f
    + 0.0005f * Mathf.Abs(aoa),
    0.3f
    );
    return(Cl, Cd);
    }

    private void FixedUpdate()
    {   
        
        // Always keep inputs in sync with the control stick
        if (controlPanel.isPressed == true)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = startPos;
            rb.rotation = startRot;
        }

        SyncInputFromControlStick();
        BakeFromModel();
        Vector3 forward = transform.forward;
        Vector3 Velocity = rb.linearVelocity * Scale;
        Vector3 right = transform.right;
        Vector3 up = transform.up;
        //aoa working!!
        
        float halfSpan = wingspan * 0.25f;
        Vector3 localRight =  wingCenter.right * halfSpan;
        Vector3 localLeft  = -wingCenter.right * halfSpan;

        Vector3 wingRightPos = wingCenter.position + localRight;
        Vector3 wingLeftPos  = wingCenter.position + localLeft;

        aoa = Vector3.SignedAngle(forward, Velocity, right);
        float yawoa = Vector3.SignedAngle(forward, Velocity, up);
        float AirDensity = 1.225f;
        speed = Velocity.magnitude;
        //Vector3 Vleft  = Velocity + Vector3.Cross(localLeft, rb.angularVelocity);
        //Vector3 Vright = Velocity + Vector3.Cross(localRight, rb.angularVelocity);
        float aoaWingL = aoa;
        float aoaWingR = aoa;
        float aoaPitch = aoa + PitchAuthority * pitchCommand - incedence;
        var wingCeoffL = NACA12(aoaWingL, wingCl_a);
        var wingCeoffR = NACA12(aoaWingR, wingCl_a);
        var tailCoeff = NACA12(aoaPitch, horiCl_a);
        var verttailCoeff = NACA12(yawoa, vertCl_a);
        WingCL = wingCeoffL.Cl;
        float qinf = AirDensity * speed * speed * 0.5f;
        liftL = wingCeoffL.Cl * wingArea * qinf * 0.5f * Scale * Scale;
        liftR = wingCeoffR.Cl * wingArea * qinf * 0.5f * Scale * Scale;
        tailForce = tailCoeff.Cl * horiArea * qinf * Scale * Scale;
        dragL = wingCeoffL.Cd * wingArea * qinf * 0.5f * Scale * Scale;
        dragR = wingCeoffR.Cd * wingArea * qinf * 0.5f * Scale * Scale;
        tailDrag = (tailCoeff.Cd * horiArea + verttailCoeff.Cd * vertArea)  * qinf * Scale * Scale;
        float yawForce = verttailCoeff.Cl * qinf * vertArea * Scale * Scale;

        // local offsets from fuselage origin
        


        // then:

        float fuselageDrag = 0.001f * qinf * Scale * Scale;
        float liftYawFactor = yawStabilityFactor * Vector3.Dot(Velocity, transform.right)  ;
        
        //lift and drag
        rb.AddForceAtPosition(
            transform.up * liftR * (1 + liftYawFactor), // 0.25 because of half wing and 1/2 in script
            wingRightPos
        );
         rb.AddForceAtPosition(
            transform.up * liftL * (1 - liftYawFactor), // 0.25 because of half wing and 1/2 in script
            wingLeftPos
        );
         rb.AddForceAtPosition(
            transform.up * tailForce , // 0.25 because of half wing and 1/2 in script
            tailCenter.position
        );
        rb.AddForceAtPosition(
            -transform.right * yawForce, // 0.25 because of half wing and 1/2 in script
            tailCenter.position
        );
        rb.AddForceAtPosition(
            -Velocity.normalized * dragR, // 0.25 because of half wing and 1/2 in script
            wingRightPos
        );
         rb.AddForceAtPosition(
            -Velocity.normalized * dragL , // 0.25 because of half wing and 1/2 in script
            wingLeftPos
        );
         rb.AddForceAtPosition(
            -Velocity.normalized * tailDrag, // 0.25 because of half wing and 1/2 in script
            tailCenter.position
        );
         rb.AddForceAtPosition(
            -Velocity.normalized * fuselageDrag, // 0.25 because of half wing and 1/2 in script
            rb.position
        );
         rb.AddForce(
            new Vector3(0,-9.81f * aeroModel.totalMass * Scale * Scale,0) // 0.25 because of half wing and 1/2 in script
        );
        rb.AddTorque(transform.forward * rollCommand * RollAuthority * qinf * Scale * Scale);
        
        //Debug.DrawRay(tailCenter,  dbgWingLForce * 0.001f, Color.green);
        //Debug.DrawRay(wingRightPos, dbgWingRForce * 0.001f, Color.cyan);
        //Debug.DrawRay(tailCenter.position, dbgTailForce * 0.001f, Color.yellow);    
    
        ThrustPercentage = ThrottlePos.localPosition.z / 0.3f;
        thrust = Scale * Scale * Scale * thrustScale / Mathf.Pow(speed * speed + 1f, 0.5f);
         rb.AddForceAtPosition(
            transform.forward * thrust * ThrustPercentage, 
            rb.position 
        );
        
        if (ThrustPercentage > 0.01f)
        {
            wheel.dynamicFriction = 0f;
        }
        else
        {
            wheel.dynamicFriction = 0.3f;
        }
        
        

        // TODO (later):
        // - Use rb.velocity and rb.angularVelocity
        // - Compute AoA, sideslip, etc.
        // - Use wing/tail coefficients and rollCommand/pitchCommand to compute forces & moments
        // - rb.AddForce(...) / rb.AddTorque(...)
    }
}
