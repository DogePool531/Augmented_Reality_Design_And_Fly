using UnityEngine;
using Oculus.Interaction; // Meta XR Interaction SDK

public class AxisClampJoystick : MonoBehaviour
{
    [Header("References")]
    public Grabbable grabbable;      // assign the Grabbable on this object

    [Header("Rotation")]
    public Vector3 localRotationAxis = Vector3.right; // X=pitch, Y=yaw, Z=roll (in THIS object’s local space)
    public float maxAngle = 30f;     // ±deg
    public float deadzoneDeg = 2f;   // small center deadzone
    public bool autoReturn = true;
    public float returnSpeedDegPerSec = 120f;

    [Header("Output")]
    [Range(-1f,1f)] public float value = 0f; // normalized [-1..1]
    public bool invert = false;

    // internals
    private Quaternion _baseWorldRot;
    private bool _isGrabbed;

    void Reset()
    {
        grabbable = GetComponent<Grabbable>();
    }

    void Awake()
    {
        if (!grabbable) grabbable = GetComponent<Grabbable>();
        _baseWorldRot = transform.rotation;
        if (grabbable) grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    void OnDestroy()
    {
        if (grabbable) grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }

    void OnPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            _isGrabbed = true;
            _baseWorldRot = transform.rotation; // capture a fresh center each grab
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            _isGrabbed = false;
        }
    }

    void Update()
    {
        // compute signed angle about axis from base to current
        float signed = GetSignedAngleAboutAxis(_baseWorldRot, transform.rotation, localRotationAxis);
        float clamped = Mathf.Clamp(signed, -maxAngle, maxAngle);
        if (Mathf.Abs(clamped) < deadzoneDeg) clamped = 0f;

        // enforce axis-only rotation (correct whatever the transformer did)
        ApplyAxisOnlyRotation(_baseWorldRot, clamped, localRotationAxis);

        // normalized output
        float norm = (maxAngle > 1e-4f) ? (clamped / maxAngle) : 0f;
        value = invert ? -norm : norm;

        // auto-return when not grabbed
        if (!_isGrabbed && autoReturn)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _baseWorldRot,
                returnSpeedDegPerSec * Time.deltaTime
            );
            // refresh output after return step
            float back = GetSignedAngleAboutAxis(_baseWorldRot, transform.rotation, localRotationAxis);
            if (Mathf.Abs(back) < deadzoneDeg) back = 0f;
            value = invert ? -(back / maxAngle) : (back / maxAngle);
        }
    }

    float GetSignedAngleAboutAxis(Quaternion baseRot, Quaternion currentRot, Vector3 localAxis)
    {
        Vector3 axisWorld = (baseRot * localAxis.normalized);
        // pick a reference vector not parallel to axis
        Vector3 refWorld = Mathf.Abs(Vector3.Dot(Vector3.forward, axisWorld)) > 0.95f ? Vector3.right : Vector3.forward;
        Vector3 baseDir = (baseRot * Vector3.ProjectOnPlane(refWorld, axisWorld).normalized);
        Vector3 currDir = (currentRot * Vector3.ProjectOnPlane(refWorld, axisWorld).normalized);
        return Vector3.SignedAngle(baseDir, currDir, axisWorld);
    }

    void ApplyAxisOnlyRotation(Quaternion baseRot, float angleDeg, Vector3 localAxis)
    {
        Vector3 axisWorld = (baseRot * localAxis.normalized);
        transform.rotation = baseRot * Quaternion.AngleAxis(angleDeg, axisWorld);
    }
}
