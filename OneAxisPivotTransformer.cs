using UnityEngine;
using Oculus.Interaction;
using System.Collections.Generic;

[RequireComponent(typeof(Grabbable))]
public class OneAxisPivotTransformer : MonoBehaviour, ITransformer
{
    public enum AllowedAxis { PitchX, RollZ }

    [Header("Refs")]
    [Tooltip("Pivot/hinge reference (usually the base). If null, uses this transform.")]
    public Transform pivot;
    [Tooltip("Optional: rotate this instead of the object with the script.")]
    public Transform targetOverride;

    [Header("Axis & Limits")]
    [Tooltip("Which single axis is allowed to move.")]
    public AllowedAxis allowedAxis = AllowedAxis.PitchX;
    [Tooltip("Maximum absolute angle from upright (deg).")]
    public float maxAngleDeg = 30f;
    [Tooltip("Invert the direction if needed.")]
    public bool invert = false;

    [Header("Behavior")]
    [Tooltip("Return to rest when released.")]
    public bool enableSpringBack = true;
    [Tooltip("Degrees per second to spring back.")]
    public float springBackSpeed = 6f;

    [Header("Output (READ)")]
    [Tooltip("Normalized single-axis input in [-1, 1].")]
    public float axisInput;                 // -1..1 along the chosen axis
    [Tooltip("Signed angle (deg) about the chosen axis.")]
    public float currentAngle;              // signed, clamped to [-maxAngleDeg, maxAngleDeg]
    [Tooltip("World-space tilt direction (projected from pivot.up).")]
    public Vector3 tiltDirection;           // helpful for visuals

    // runtime
    private Grabbable _grabbable;
    private Transform _target;
    private Quaternion _restLocalRot;
    private bool _isTransforming;

    // keep rotating around the pivot
    private Vector3 _restLocalOffsetFromPivot;

    void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _target = targetOverride != null ? targetOverride : transform;
        if (pivot == null) pivot = transform;

        _restLocalRot = _target.localRotation;
        _restLocalOffsetFromPivot = pivot.InverseTransformPoint(_target.position);
    }

    void Update()
    {
        // Spring back when released
        if (enableSpringBack && !_isTransforming)
        {
            _target.localRotation = Quaternion.RotateTowards(
                _target.localRotation, _restLocalRot, springBackSpeed * Time.deltaTime);

            // keep position locked to pivot + saved offset
            _target.position = pivot.TransformPoint(_restLocalOffsetFromPivot);

            // decay outputs
            axisInput = Mathf.MoveTowards(axisInput, 0f, 
                Time.deltaTime * (springBackSpeed / Mathf.Max(1f, maxAngleDeg)));
            currentAngle = Mathf.MoveTowards(currentAngle, 0f, springBackSpeed * Time.deltaTime);
            tiltDirection = Vector3.zero;
        }
    }

    // ITransformer
    public void Initialize(IGrabbable grabbable)
    {
        // not used; we already have our Grabbable via GetComponent
    }

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

        // Build direction from pivot to hand, analyzed in pivot-local space
        Vector3 up = pivot.up;
        Vector3 basePos = pivot.position;
        Vector3 worldDir = (handPos - basePos);
        if (worldDir.sqrMagnitude < 1e-6f) return;

        Vector3 localDir = pivot.InverseTransformDirection(worldDir.normalized);

        // Compute pitch (about X) and roll (about Z) angles from localDir
        float pitchDeg = Mathf.Atan2(localDir.z,  localDir.y) * Mathf.Rad2Deg;  // forward/back
        float rollDeg  = Mathf.Atan2(-localDir.x, localDir.y) * Mathf.Rad2Deg;  // left/right

        // Select single axis and clamp
        float signedDeg;
        if (allowedAxis == AllowedAxis.PitchX)
            signedDeg = Mathf.Clamp(pitchDeg, -maxAngleDeg, maxAngleDeg);
        else
            signedDeg = Mathf.Clamp(rollDeg,  -maxAngleDeg, maxAngleDeg);

        if (invert) signedDeg = -signedDeg;

        // Build the local tilt using ONLY the chosen axis
        Quaternion localTilt = (allowedAxis == AllowedAxis.PitchX)
            ? Quaternion.Euler(signedDeg, 0f, 0f)
            : Quaternion.Euler(0f, 0f, signedDeg);

        // Apply world rotation and lock position about the pivot
        _target.rotation = pivot.rotation * localTilt;
        _target.position = pivot.TransformPoint(_restLocalOffsetFromPivot);

        // ----- OUTPUTS -----
        currentAngle = signedDeg;
        axisInput = (maxAngleDeg > 0f) ? (signedDeg / maxAngleDeg) : 0f;

        // world-space tilt direction (projection of rotated up onto plane orthogonal to pivot.up)
        Vector3 rotatedUp = (_target.rotation * Vector3.up).normalized;
        tiltDirection = Vector3.ProjectOnPlane(rotatedUp, up).normalized;
    }

    public void EndTransform()
    {
        _isTransforming = false;
    }
}
