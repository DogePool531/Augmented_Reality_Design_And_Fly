using Oculus.Interaction;
using UnityEngine;

public class JoystickLimiter : MonoBehaviour
{
    public Transform stick;           // the visible mesh
    public float maxAngle = 20f;      // degrees
    private bool _isGrabbed;

    private Grabbable _grabbable;
    private Quaternion _restLocalRot;

    void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _restLocalRot = stick.localRotation;
    }

    void OnEnable() => _grabbable.WhenPointerEventRaised += OnEvt;
    void OnDisable() => _grabbable.WhenPointerEventRaised -= OnEvt;

    void Update()
    {
        if (!_isGrabbed) return;

        // Read the current world orientation and convert to local
        var local = Quaternion.Inverse(transform.rotation) * stick.rotation;

        // Clamp tilt around local X/Y, keep Z zeroed (no twist)
        local.ToAngleAxis(out float angle, out Vector3 axis);
        angle = Mathf.Min(angle, maxAngle);

        // Recompose with clamp toward the axis projected on local X/Y
        Vector3 localAxis = transform.InverseTransformDirection(axis);
        localAxis.z = 0f;
        localAxis.Normalize();
        stick.localRotation = Quaternion.AngleAxis(angle, localAxis);
    }

    private void OnEvt(PointerEvent e)
    {
        if (e.Type == PointerEventType.Select) _isGrabbed = true;
        if (e.Type == PointerEventType.Unselect)
        {
            _isGrabbed = false;
            stick.localRotation = _restLocalRot;  // spring back
        }
    }
}