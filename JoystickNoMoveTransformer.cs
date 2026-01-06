using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// A transformer that blocks position changes but allows rotation
/// Save this as: JoystickNoMoveTransformer.cs
/// Add this to the same object as Grabbable
/// </summary>
public class JoystickNoMoveTransformer : MonoBehaviour, ITransformer
{
    private IGrabbable _grabbable;
    private Vector3 _lockedPosition;
    
    public void Initialize(IGrabbable grabbable)
    {
        _grabbable = grabbable;
    }

    public void BeginTransform()
    {
        // Lock the current position
        _lockedPosition = _grabbable.Transform.position;
    }

    public void UpdateTransform()
    {
        // Do nothing - let MetaBBJoystick handle all transformations
        // If we lock position here, it fights with our rotation script
    }

    public void EndTransform()
    {
        // Ensure position stays locked
        _grabbable.Transform.position = _lockedPosition;
    }
}
