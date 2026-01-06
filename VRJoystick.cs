using UnityEngine;

/// <summary>
/// Dead simple VR joystick - just attach to your joystick stick object
/// Works with any grab system - just needs OnSelectEntered/OnSelectExited calls
/// </summary>
public class SimpleVRJoystick : MonoBehaviour 
{
    [Header("Setup")]
    public Transform pivotPoint; // Empty GameObject at base
    public float maxTiltAngle = 30f;
    public float returnSpeed = 5f;
    
    [Header("Output")]
    public Vector2 input; // Read this for joystick input (-1 to 1)
    
    private bool grabbed = false;
    private Vector3 grabOffset;
    
    void Update() 
    {
        if (grabbed) 
        {
            // Get mouse position in world (for testing in editor)
            // In VR, you'd pass controller position differently
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 targetPos = ray.GetPoint(0.3f); // Adjust distance as needed
            
            TiltToward(targetPos);
        }
        else 
        {
            // Return to center
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * returnSpeed);
            input = Vector2.Lerp(input, Vector2.zero, Time.deltaTime * returnSpeed);
        }
    }
    
    void TiltToward(Vector3 worldPosition) 
    {
        if (pivotPoint == null) return;
        
        Vector3 direction = worldPosition - pivotPoint.position;
        Vector3 localDir = pivotPoint.InverseTransformDirection(direction.normalized);
        
        float angleX = Mathf.Atan2(localDir.z, localDir.y) * Mathf.Rad2Deg - 90f;
        float angleZ = Mathf.Atan2(-localDir.x, localDir.y) * Mathf.Rad2Deg - 90f;
        
        angleX = Mathf.Clamp(angleX, -maxTiltAngle, maxTiltAngle);
        angleZ = Mathf.Clamp(angleZ, -maxTiltAngle, maxTiltAngle);
        
        transform.localRotation = Quaternion.Euler(angleX, 0f, angleZ);
        
        input.x = angleZ / maxTiltAngle;
        input.y = angleX / maxTiltAngle;
    }
    
    // Call from ISDK or any grab system
    public void StartGrab() 
    {
        grabbed = true;
    }
    
    public void EndGrab() 
    {
        grabbed = false;
    }
    
    // For testing without VR
    void OnMouseDown() 
    {
        StartGrab();
    }
    
    void OnMouseUp() 
    {
        EndGrab();
    }
}