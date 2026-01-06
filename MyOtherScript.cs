using UnityEngine;
public class MyOtherScript : MonoBehaviour
{
    public PivotAroundBaseTransformer joystick;
    public float speed = 0.01f;

    void Update()
    {
        Vector2 input = joystick.joystickInput;
        Debug.Log($"Joystick: X={input.x}, Y={input.y}");

        // Use it to control something
    
        transform.Translate(input.x * (-speed), 0, input.y * (speed));
    }
}