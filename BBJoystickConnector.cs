using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

/// <summary>
/// Connects Meta Building Blocks HandGrabInteractable to MetaBBJoystick
/// Save this file as: BBJoystickConnector.cs
/// 
/// SETUP:
/// Add this to the CHILD object that has HandGrabInteractable component
/// (the child created by the Building Blocks Grab Interaction wizard)
/// </summary>
public class BBJoystickConnector : MonoBehaviour 
{
    private MetaBBJoystick joystick;
    private HandGrabInteractable handGrabInteractable;
    
    void Start() 
    {
        // Find the joystick script on parent
        joystick = GetComponentInParent<MetaBBJoystick>();
        
        if (joystick == null) 
        {
            Debug.LogError("BBJoystickConnector: MetaBBJoystick not found on parent! Make sure this script is on the child with HandGrabInteractable.");
            enabled = false;
            return;
        }
        
        // Find HandGrabInteractable on this object
        handGrabInteractable = GetComponent<HandGrabInteractable>();
        
        if (handGrabInteractable == null) 
        {
            Debug.LogError("BBJoystickConnector: HandGrabInteractable not found! Make sure you ran the Grab Interaction Building Block wizard.");
            enabled = false;
            return;
        }
        
        // CRITICAL: Disable the movement provider so Building Blocks doesn't move the object
        // We need to use reflection because MovementProvider is private
        DisableMovementProvider();
        
        // Subscribe to grab events
        handGrabInteractable.WhenSelectingInteractorViewAdded += HandleGrab;
        handGrabInteractable.WhenSelectingInteractorViewRemoved += HandleRelease;
        
        Debug.Log("BBJoystickConnector: Successfully connected to Meta Building Blocks!");
    }
    
    private void DisableMovementProvider()
    {
        // Find and disable the MoveTowardsTargetProvider component that was auto-created
        var movementProvider = GetComponent<Oculus.Interaction.MoveTowardsTargetProvider>();
        if (movementProvider != null)
        {
            Destroy(movementProvider);
            Debug.Log("BBJoystickConnector: Disabled automatic movement provider");
        }
        
        // DON'T disable Grabbable - instead replace its transformer with our no-op transformer
        var grabbable = joystick.GetComponent<Oculus.Interaction.Grabbable>();
        if (grabbable != null)
        {
            // Add our no-move transformer if not already present
            var noMoveTransformer = joystick.GetComponent<JoystickNoMoveTransformer>();
            if (noMoveTransformer == null)
            {
                noMoveTransformer = joystick.gameObject.AddComponent<JoystickNoMoveTransformer>();
            }
            
            // Inject it as both one-grab and two-grab transformer
            grabbable.InjectOptionalOneGrabTransformer(noMoveTransformer);
            grabbable.InjectOptionalTwoGrabTransformer(noMoveTransformer);
            
            Debug.Log("BBJoystickConnector: Replaced Grabbable transformer with no-move transformer");
        }
    }
    
    void OnDestroy() 
    {
        // Unsubscribe from events
        if (handGrabInteractable != null) 
        {
            handGrabInteractable.WhenSelectingInteractorViewAdded -= HandleGrab;
            handGrabInteractable.WhenSelectingInteractorViewRemoved -= HandleRelease;
        }
    }
    
    private void HandleGrab(IInteractorView interactor) 
    {
        // Get controller transform from the interactor
        Transform controllerTransform = null;
        
        if (interactor is MonoBehaviour mono) 
        {
            controllerTransform = mono.transform;
        }
        
        if (controllerTransform != null) 
        {
            joystick.OnJoystickGrabbed(controllerTransform);
        }
        else
        {
            Debug.LogWarning("BBJoystickConnector: Could not get controller transform from interactor");
        }
    }
    
    private void HandleRelease(IInteractorView interactor) 
    {
        joystick.OnJoystickReleased();
    }
}