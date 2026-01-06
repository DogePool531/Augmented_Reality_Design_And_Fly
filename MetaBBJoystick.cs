using UnityEngine;

/// <summary>
/// Simple VR Joystick for Meta Building Blocks
/// Save this file as: MetaBBJoystick.cs
/// 
/// SETUP:
/// 1. Add this to your joystick stick object
/// 2. Create an empty GameObject at the base pivot point and assign it below
/// 3. Add BBJoystickConnector.cs to the child object with HandGrabInteractable
/// </summary>
public class MetaBBJoystick : MonoBehaviour 
{
    [Header("Required Setup")]
    [Tooltip("Create empty GameObject at base where joystick pivots from")]
    public Transform pivotPoint;
    
    [Header("Joystick Settings")]
    [Tooltip("Maximum tilt angle in degrees")]
    public float maxTiltAngle = 30f;
    
    [Tooltip("How fast joystick returns to center")]
    public float returnSpeed = 5f;
    
    [Tooltip("Smoothing for movement")]
    public float smoothSpeed = 10f;
    
    [Header("Output Values - READ ONLY")]
    [Tooltip("Current joystick input (-1 to 1 on each axis)")]
    public Vector2 joystickInput;
    
    private bool isGrabbed = false;
    private Transform grabberTransform;
    private Quaternion targetRotation;
    private Quaternion restRotation;
    
    void Start() 
    {
        // Store rest position
        restRotation = transform.localRotation;
        targetRotation = restRotation;
        
        // Validation
        if (pivotPoint == null) 
        {
            Debug.LogError("MetaBBJoystick: Pivot Point not assigned! Create an empty GameObject at the base.");
        }
        
        Debug.Log("MetaBBJoystick: Ready!");
    }
    
    void Update() 
    {
        if (isGrabbed && grabberTransform != null) 
        {
            // Calculate direction from pivot to grabber
            Vector3 direction = grabberTransform.position - pivotPoint.position;
            
            if (direction.magnitude > 0.01f) 
            {
                // Convert to local space
                Vector3 localDir = pivotPoint.InverseTransformDirection(direction.normalized);
                
                // Calculate tilt angles
                float tiltX = Mathf.Atan2(localDir.z, localDir.y) * Mathf.Rad2Deg - 90f;
                float tiltZ = Mathf.Atan2(-localDir.x, localDir.y) * Mathf.Rad2Deg - 90f;
                
                // Clamp to max angle
                tiltX = Mathf.Clamp(tiltX, -maxTiltAngle, maxTiltAngle);
                tiltZ = Mathf.Clamp(tiltZ, -maxTiltAngle, maxTiltAngle);
                
                // Set target rotation
                targetRotation = pivotPoint.rotation * Quaternion.Euler(tiltX, 0f, tiltZ);
                
                // Update input values
                joystickInput.x = tiltZ / maxTiltAngle;
                joystickInput.y = tiltX / maxTiltAngle;
            }
        }
        else 
        {
            // Return to rest position
            targetRotation = Quaternion.Lerp(targetRotation, restRotation, Time.deltaTime * returnSpeed);
            joystickInput = Vector2.Lerp(joystickInput, Vector2.zero, Time.deltaTime * returnSpeed);
        }
        
        // Apply smooth rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
    
    /// <summary>
    /// Call this when joystick is grabbed (called by BBJoystickConnector)
    /// </summary>
    public void OnJoystickGrabbed(Transform controllerTransform) 
    {
        isGrabbed = true;
        grabberTransform = controllerTransform;
        Debug.Log("MetaBBJoystick: Grabbed!");
    }
    
    /// <summary>
    /// Call this when joystick is released (called by BBJoystickConnector)
    /// </summary>
    public void OnJoystickReleased() 
    {
        isGrabbed = false;
        grabberTransform = null;
        Debug.Log("MetaBBJoystick: Released!");
    }
    
    // Debug visualization
    void OnDrawGizmos() 
    {
        if (pivotPoint != null) 
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivotPoint.position, 0.02f);
            Gizmos.DrawLine(pivotPoint.position, transform.position);
        }
    }
}