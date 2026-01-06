using UnityEngine;
using Oculus.Interaction;

public class PhysicsOnFirstGrab : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private bool hasBeenGrabbed = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        
        // Start frozen in space
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }
    
    void Update()
    {
        if (!hasBeenGrabbed && grabbable != null && rb != null)
        {
            // Check if currently being grabbed (correct syntax)
            if (grabbable.SelectingPointsCount > 0)
            {
                hasBeenGrabbed = true;
                rb.isKinematic = false; // Enable physics permanently
            }
        }
    }
}