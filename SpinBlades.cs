using UnityEngine;

public class SpinBlades : MonoBehaviour
{
    [Header("Spin (RPM)")]
    public float targetRPM = 1200f;     // desired steady RPM
    public float accel = 800f;          // RPM/s ramp rate
    public Vector3 localAxis = Vector3.up; // axis in local space

    private float _currentRPM;

    void Update()
    {
        // Ramp toward target RPM (start/stop friendly)
        _currentRPM = Mathf.MoveTowards(_currentRPM, targetRPM, accel * Time.deltaTime);

        // Convert RPM -> deg/s  (rpm * 6 = deg/s)
        float degPerSecond = _currentRPM * 6f;

        // Apply in local space
        transform.Rotate(localAxis, degPerSecond * Time.deltaTime, Space.Self);
    }

    public void StartSpinning(float rpm) => targetRPM = rpm;
    public void StopSpinning() => targetRPM = 0f;
}
