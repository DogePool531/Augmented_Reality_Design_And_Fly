using UnityEngine;
public class HelicopterFlight : MonoBehaviour
{
    public PivotAroundBaseTransformer joystick;

    void Update()
    {

        float maxTiltDeg = 50;
        float pitch = joystick.joystickInput.y * maxTiltDeg; // X rotation
        float roll  = joystick.joystickInput.x * maxTiltDeg; // Z rotation
        transform.localEulerAngles = new Vector3(pitch, 0f,  roll);
        
    }
}