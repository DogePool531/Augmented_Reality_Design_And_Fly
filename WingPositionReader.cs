using UnityEngine;

public class WingPositionReader : MonoBehaviour
{
    [Header("References")]
    public Transform wingBase;
    public Transform wingtipGrab;

    [Header("Output (Read-Only)")]
    public Vector3 wingBasePos;
    public Vector3 wingtipPos;

    void Start()
    {
        // Automatically find children if not assigned
        if (wingBase == null)
            wingBase = transform.Find("Wing_Base");
        if (wingtipGrab == null)
            wingtipGrab = transform.Find("Wingtip_Grab"); 
    }

    void Update()
    {
        if (wingBase != null)
            wingBasePos = wingBase.localPosition; // world position

        if (wingtipGrab != null)
            wingtipPos = wingtipGrab.localPosition; // world position
    }
}
