using UnityEngine;

public class MirrorLocalTransform : MonoBehaviour
{
    [Header("Source to Mirror")]
    [Tooltip("The transform whose local position and scale will be mirrored.")]
    public Transform source;

    [Header("Position Settings")]
    [Tooltip("If true, local position will be mirrored.")]
    public bool mirrorPosition = true;

    [Tooltip("Invert these axes when mirroring local position.")]
    public Vector3 positionAxisSign = new Vector3(1f, 1f, 1f); // set to -1 on any axis to flip

    [Tooltip("Offset applied after mirroring local position (in local space).")]
    public Vector3 positionOffset = Vector3.zero;

    [Header("Scale Settings")]
    [Tooltip("If true, local scale will be mirrored.")]
    public bool mirrorScale = true;

    [Tooltip("Invert these axes when mirroring local scale.")]
    public Vector3 scaleAxisSign = new Vector3(1f, 1f, 1f); // set to -1 on any axis to flip

    [Tooltip("Multiply scale after mirroring (1 = no change).")]
    public Vector3 scaleMultiplier = Vector3.one;

    private void LateUpdate()
    {
        if (source == null) return;

        // Mirror local position
        if (mirrorPosition)
        {
            Vector3 srcPos = source.localPosition;
            Vector3 mirroredPos = new Vector3(
                srcPos.x * positionAxisSign.x,
                srcPos.y * positionAxisSign.y,
                srcPos.z * positionAxisSign.z
            );

            transform.localPosition = mirroredPos + positionOffset;
        }

        // Mirror local scale
        if (mirrorScale)
        {
            Vector3 srcScale = source.localScale;
            Vector3 mirroredScale = new Vector3(
                srcScale.x * scaleAxisSign.x,
                srcScale.y * scaleAxisSign.y,
                srcScale.z * scaleAxisSign.z
            );

            mirroredScale = Vector3.Scale(mirroredScale, scaleMultiplier);
            transform.localScale = mirroredScale;
        }
    }
}
