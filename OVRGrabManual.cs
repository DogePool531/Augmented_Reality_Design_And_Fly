// File: OVRGrabManual.cs  (class name MUST match the file name)
using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class OVRGrabManual : OVRGrabbable
{
    public event Action<bool, OVRGrabber> OnGrabChanged;
    public OVRGrabber CurrentGrabber { get; private set; }

    private bool _lastGrabState;

    
    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var col = GetComponent<Collider>();
        col.isTrigger = false; // important: the handle’s collider should be solid
    }

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        var col = GetComponent<Collider>();
        Debug.Log($"[OVRGrabManual] Awake on '{name}'. RB(kin={rb.isKinematic}) Col(trigger={col.isTrigger})");
    }

    void OnEnable()
    {
        // Helpful boot log so you know the script is actually running on device
        Debug.Log($"[OVRGrabManual] Enabled on '{name}'.");
    }

    void Update()
    {
        // Edge-triggered log so you can see poll-based state too
        if (isGrabbed != _lastGrabState)
        {
            _lastGrabState = isGrabbed;
            Debug.Log($"[OVRGrabManual] {(isGrabbed ? "POLL: grabbed" : "POLL: released")} on '{name}' by {grabbedBy?.name}");
        }
    }

    public override void GrabBegin(OVRGrabber hand, Collider grabPoint)
    {
        base.GrabBegin(hand, grabPoint);

        if (grabbedRigidbody != null) grabbedRigidbody.isKinematic = true; // do NOT let OVR move it
        CurrentGrabber = hand;

        Debug.Log($"[OVRGrabManual] EVENT: GrabBegin on '{name}' by {hand?.name} via {grabPoint?.name}");
        OnGrabChanged?.Invoke(true, hand);
    }

    public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        base.GrabEnd(linearVelocity, angularVelocity);

        var last = CurrentGrabber;
        CurrentGrabber = null;

        Debug.Log($"[OVRGrabManual] EVENT: GrabEnd on '{name}' (vel={linearVelocity.magnitude:F2})");
        OnGrabChanged?.Invoke(false, last);
    }
}
