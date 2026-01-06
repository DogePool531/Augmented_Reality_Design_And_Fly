using UnityEngine;
using Oculus.Interaction; // Meta XR Interaction SDK (com.meta.xr.sdk.interaction)

/// Put this on the pivot (the point you want to rotate about).
/// Assign your StickMesh's Grabbable in the inspector.
/// Add a OneGrabRotateTransformer (or similar) to the StickMesh so grabbing rotates it.
/// This script then clamps/locks rotation to one axis and computes [-1..1] value.
public class ARPivotJoystick : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Grabbable on the stick mesh (child).")]
    public Grabbable grabbable;     // REQUIRED: assign your StickMesh's Grabbable
    [Tooltip("Optional: the visible mesh (for gizmos or debugging).")]
    public Transform stickMesh;

    [Header("Rotation Settings")]
    [Tooltip("Axis in THIS pivot's local space to rotate about (X/Y/Z).")]
    public Vector3 localRotationAxis = Vector3.right;
    [Tooltip("Maximum deflection in degrees (±).")]
    public float maxAngle = 30f;
    [Tooltip("Dead zone in degrees around center.")]
    public float deadzoneDeg = 2f;

    [Header("Return-to-center")]
    [Tooltip("If true, returns to center when released.")]
    public bool autoReturn = true;
    [Tooltip("Degrees per second to return to center.")]
    public float returnSpeed = 120f;

    [Header("Output")]
    [Range(-1f, 1f)]
    public float value = 0f;
    public bool invert = false;

    // Internals
    private Quaternion _baseWorldRot;   // center rotation we measure angles from
    private bool _isGrabbed;

    void Awake()
    {
        _baseWorldRot = transform.rotation;

        if (grabbable == null)
        {
            grabbable = GetComponentInChildren<Grabbable>();
            if (grabbable == null)
            {
                Debug.LogError("[ARPivotJoystick] No Grabbable assigned/found. Assign your StickMesh's Grabbable.");
                enabled = false;
                return;
            }
        }

        // Subscribe to grab events (no pointer types needed)
        grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    void OnDestroy()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                _isGrabbed = true;
                // capture current center when grab begins (feels natural)
                _baseWorldRot = transform.rotation;
                break;

            case PointerEventType.Unselect:
                _isGrabbed = false;
                break;
        }
    }

    void Update()
    {
        // Compute current signed angle about the chosen axis, relative to _baseWorldRot.
        float signed = GetSignedAngleAboutAxis(_baseWorldRot, transform.rotation, localRotationAxis);

        // Clamp & deadzone
        float clamped = Mathf.Clamp(signed, -maxAngle, maxAngle);
        if (Mathf.Abs(clamped) < deadzoneDeg) clamped = 0f;

        // Enforce axis-only rotation and clamp
        ApplyAxisOnlyRotation(_baseWorldRot, clamped, localRotationAxis);

        // Output value
        float norm = (maxAngle > 0.0001f) ? (clamped / maxAngle) : 0f;
        value = invert ? -norm : norm;

        // Auto return when released
        if (!_isGrabbed && autoReturn)
        {
            // rotate back toward base
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _baseWorldRot,
                returnSpeed * Time.deltaTime
            );
            // Recompute value from the new pose:
            float back = GetSignedAngleAboutAxis(_baseWorldRot, transform.rotation, localRotationAxis);
            if (Mathf.Abs(back) < deadzoneDeg) back = 0f;
            value = invert ? -(back / maxAngle) : (back / maxAngle);
        }
    }

    /// Returns signed angle (deg) between baseRot and currentRot around the given LOCAL axis of this pivot.
    private float GetSignedAngleAboutAxis(Quaternion baseRot, Quaternion currentRot, Vector3 localAxis)
    {
        // Convert local axis to world
        Vector3 axisWorld = (baseRot * localAxis).normalized;

        // Choose a reference vector orthogonal to axis
        Vector3 refWorld = Vector3.forward;
        if (Mathf.Abs(Vector3.Dot(refWorld, axisWorld)) > 0.95f)
            refWorld = Vector3.right; // avoid near-parallel
        // Project ref onto plane orthogonal to axis
        Vector3 refOnPlane = Vector3.ProjectOnPlane(refWorld, axisWorld).normalized;

        // Build "reference" directions for base and current
        Vector3 baseDir = (baseRot * refOnPlane).normalized;
        Vector3 currDir = (currentRot * refOnPlane).normalized;

        // Compute signed angle about axis
        return Vector3.SignedAngle(baseDir, currDir, axisWorld);
    }

    /// Overwrite pivot rotation to be baseRot rotated only around localAxis by 'angleDeg'.
    private void ApplyAxisOnlyRotation(Quaternion baseRot, float angleDeg, Vector3 localAxis)
    {
        Vector3 axisWorld = (baseRot * localAxis.normalized);
        transform.rotation = baseRot * Quaternion.AngleAxis(angleDeg, axisWorld);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Visualize axis
        Transform t = transform;
        Quaternion b = Application.isPlaying ? _baseWorldRot : t.rotation;
        Vector3 axisWorld = (b * localRotationAxis.normalized);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(t.position, t.position + axisWorld * 0.25f);
    }
#endif
}
