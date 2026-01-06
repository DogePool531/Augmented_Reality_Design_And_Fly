using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HelicopterInBox : MonoBehaviour
{
    [Header("Controls")]
    public OneAxisPivotTransformer joystick;   // your existing stick script
    [Range(0f, 2f)] public float collective = 1f; // lift throttle 0..1
    public float maxThrust = 40f;                  // N (tune to mass)
    public float linearDamping = 0.3f;             // simple air drag
    public bool useJoystickTiltForDirection = true; // thrust along heli's up (recommended)

    [Header("Bounds (world space)")]
    public Vector3 boxCenter = Vector3.zero;
    public Vector3 boxSize   = new Vector3(10f, 6f, 10f); // width, height, depth
    public float wallBounceDamping = 0.2f; // 0 = reflect perfectly, 1 = kill all speed on hit

    [Header("Gizmos")]
    public bool drawBounds = true;
    public Color boundsColor = new Color(0f, 0.7f, 1f, 0.25f);

    private Rigidbody _rb;
    private Vector3 _half;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearDamping = 0.2f;           // we’ll do our own simple damping
        _rb.angularDamping = 0.2f;
        _half = boxSize * 0.5f;
    }

    void FixedUpdate()
    {
        // 1) Lift/thrust
        Vector3 thrustDir = transform.up; // if your heli matches the stick tilt, this is perfect
        if (!useJoystickTiltForDirection && joystick && joystick.pivot)
        {
            // Alternative: use stick base up-axis regardless of heli rotation
            thrustDir = joystick.pivot.up;
        }
        collective = 1 - joystick.axisInput;
        float thrust = collective * maxThrust;
        float gravity = maxThrust;
        Vector3 gravityVetor = new Vector3(0f, -gravity, 0f);
        _rb.AddForce(thrustDir * thrust, ForceMode.Force);
        _rb.AddForce(gravityVetor, ForceMode.Force);

        // crude air drag
        _rb.AddForce(-_rb.linearVelocity * linearDamping, ForceMode.Force);

        // 2) Constrain to bounds
        ConstrainToBox();
    }

    private void ConstrainToBox()
    {
        Vector3 pos = _rb.position;
        Vector3 vel = _rb.linearVelocity;

        // Compute min/max
        Vector3 min = boxCenter - _half;
        Vector3 max = boxCenter + _half;

        bool hit = false;

        // X
        if (pos.x < min.x) { pos.x = min.x; vel.x = -vel.x * (1f - wallBounceDamping); hit = true; }
        else if (pos.x > max.x) { pos.x = max.x; vel.x = -vel.x * (1f - wallBounceDamping); hit = true; }

        // Y
        if (pos.y < min.y) { pos.y = min.y; vel.y = -vel.y * (1f - wallBounceDamping); hit = true; }
        else if (pos.y > max.y) { pos.y = max.y; vel.y = -vel.y * (1f - wallBounceDamping); hit = true; }

        // Z
        if (pos.z < min.z) { pos.z = min.z; vel.z = -vel.z * (1f - wallBounceDamping); hit = true; }
        else if (pos.z > max.z) { pos.z = max.z; vel.z = -vel.z * (1f - wallBounceDamping); hit = true; }

        if (hit)
        {
            _rb.position = pos;   // snap inside
            _rb.linearVelocity = vel;   // reflect & damp
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawBounds) return;
        Gizmos.color = boundsColor;
        Gizmos.DrawCube(boxCenter, boxSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
