using UnityEngine;

public class grabDebug_test : MonoBehaviour 
{
    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            GetComponent<Rigidbody>().AddForce(Vector3.up * 100);
        }
    }
}