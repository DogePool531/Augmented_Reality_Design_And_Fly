using UnityEngine;
using Oculus.Interaction;
using System.Collections.Generic;

[RequireComponent(typeof(Grabbable))]
public class PivotAroundBaseTransformer : MonoBehaviour, ITransformer
{
    [Header("Refs")]
    public Transform pivot;                 // Assign the Pivot object at the base
    public Transform targetOverride;        // Optional: custom target; otherwise uses this.transform

    [Header("Limits")]
    public float maxAngleDegrees = 30f;     // Max tilt from upright
    public bool limitX = true;              // Allow pitch (forward/back)
    public bool limitZ = true;              // Allow roll (left/right)

    [Header("Behavior")]
    public bool springBackOnRelease = true;
    public float springBackSpeed = 6f;      // degrees per second

    [Header("Output (READ)")]
    public Vector2 joystickInput;           // X (roll), Y (pitch), normalized -1..1
    public float currentTiltAngle;          // Scalar tilt from upright (deg)
    public Vector3 tiltDirection;           // World-space tilt dir (projected from pivot.up)

    // runtime
    private Grabbable _grabbable;
    private Transform _target;
    private Quaternion _restLocalRot;
    private bool _isTransforming;

    // cache the rest offset so we keep rotating around the pivot
    private Vector3 _restLocalOffsetFromPivot;

    void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _target = targetOverride != null ? targetOverride : transform;
        if (pivot == null) pivot = transform; // fallback, but assign in Inspector!

        _restLocalRot = _target.localRotation;
        _restLocalOffsetFromPivot = pivot.InverseTransformPoint(_target.position);
    }

    void Update()
    {
        // spring back when not held
        if (springBackOnRelease && !_isTransforming)
        {
            _target.localRotation = Quaternion.RotateTowards(
                _target.localRotation, _restLocalRot, springBackSpeed * Time.deltaTime);

            // re-lock position to pivot + saved offset
            _target.position = pivot.TransformPoint(_restLocalOffsetFromPivot);

            // decay outputs toward zero
            joystickInput = Vector2.MoveTowards(joystickInput, Vector2.zero, Time.deltaTime * (springBackSpeed / Mathf.Max(1f, maxAngleDegrees)));
            currentTiltAngle = Mathf.MoveTowards(currentTiltAngle, 0f, springBackSpeed * Time.deltaTime);
            tiltDirection = Vector3.zero;
        }
    }

    // ITransformer
    public void Initialize(IGrabbable grabbable) { /* not used, _grabbable is our own */ }

    public void BeginTransform()
    {
        _isTransforming = true;
        _restLocalRot = _target.localRotation;
        _restLocalOffsetFromPivot = pivot.InverseTransformPoint(_target.position);
    }

    public void UpdateTransform()
    {
        List<Pose> points = _grabbable.GrabPoints;
        if (points == null || points.Count == 0) return;

        Pose grabPose = points[0];
        Vector3 handPos = grabPose.position;

        // Build local direction from pivot to hand
        Vector3 up = pivot.up;               // define upright
        Vector3 basePos = pivot.position;
        Vector3 worldDir = (handPos - basePos);
        if (worldDir.sqrMagnitude < 1e-6f) return;

        // Convert to pivot local space to get clean pitch/roll
        Vector3 localDir = pivot.InverseTransformDirection(worldDir.normalized);

        // Pitch about +X (forward/back via +Z), Roll about +Z (left/right via -X)
        float tiltX = Mathf.Atan2(localDir.z,  localDir.y) * Mathf.Rad2Deg;  // pitch
        float tiltZ = Mathf.Atan2(-localDir.x, localDir.y) * Mathf.Rad2Deg;  // roll

        if (!limitX) tiltX = 0f;
        if (!limitZ) tiltZ = 0f;

        // Clamp individual axes
        tiltX = Mathf.Clamp(tiltX, -maxAngleDegrees, maxAngleDegrees);
        tiltZ = Mathf.Clamp(tiltZ, -maxAngleDegrees, maxAngleDegrees);

        // Compose world rotation from pivot basis
        Quaternion localTilt = Quaternion.Euler(tiltX, 0f, tiltZ);
        Quaternion targetWorldRot = pivot.rotation * localTilt;

        // Apply rotation smoothly (or directly if you prefer)
        _target.rotation = targetWorldRot;

        // Lock position to pivot + the cached rest offset (rotating around pivot)
        _target.position = pivot.TransformPoint(_restLocalOffsetFromPivot);

        // ----- OUTPUTS -----
        // normalized inputs in [-1, 1]
        joystickInput.x = (maxAngleDegrees > 0f) ? (tiltZ / maxAngleDegrees) : 0f; // roll
        joystickInput.y = (maxAngleDegrees > 0f) ? (tiltX / maxAngleDegrees) : 0f; // pitch

        // scalar tilt from upright (use magnitude of the two angles)
        currentTiltAngle = Mathf.Min(maxAngleDegrees, Mathf.Sqrt(tiltX * tiltX + tiltZ * tiltZ));

        // world-space tilt direction (projection of rotated up onto plane orthogonal to up)
        Vector3 rotatedUp = (_target.rotation * Vector3.up).normalized;
        tiltDirection = Vector3.ProjectOnPlane(rotatedUp, up).normalized;
    }

    public void EndTransform()
    {
        _isTransforming = false;
    }
}