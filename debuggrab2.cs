using UnityEngine;

public class GrabDebugListener : MonoBehaviour
{
    public OVRGrabManual grabbable;  // drag GrabHandle here

    void OnEnable()
    {
        if (grabbable != null)
            grabbable.OnGrabChanged += HandleGrabChanged;
    }

    void OnDisable()
    {
        if (grabbable != null)
            grabbable.OnGrabChanged -= HandleGrabChanged;
    }

    private void HandleGrabChanged(bool grabbed, OVRGrabber hand)
    {
        Debug.Log($"[GrabDebug] {(grabbed ? "Grabbed" : "Released")} by {hand?.name}");
    }
}
