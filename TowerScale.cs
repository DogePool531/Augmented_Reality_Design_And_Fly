using UnityEngine;

public class TowerScale : MonoBehaviour
{
    public AeroPhysics aeroPhysics;
    public Vector3 scaling;
    public Vector3 startscaling;
   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        startscaling = transform.localScale;
        scaling = startscaling / aeroPhysics.Scale;
    }

    // Update is called once per frame
    void Update()
    {
        scaling = startscaling / aeroPhysics.Scale;
        transform.localScale = scaling;
    }
}
