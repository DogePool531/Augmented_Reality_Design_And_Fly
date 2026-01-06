using UnityEngine;
using Oculus.Interaction;
using static Oculus.Interaction.TransformerUtils;
public class ControlPanel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("References")]
    public Transform scaleSlider;
    public PokeInteractable poke;
    public OneGrabTranslateTransformer transformer;
    public float maxDisplacement;
    public float displacement;
    public Vector3 startPos;
    public bool isPressed;
    void Start()
    {
        var c = transformer.Constraints;
        isPressed = false;
        startPos = scaleSlider.localPosition;
        maxDisplacement =  c.MinX.Value - c.MaxX.Value;
        displacement = (scaleSlider.localPosition.x - startPos.x)/maxDisplacement;
        
    }

    // Update is called once per frame
    void Update()
    {
        displacement = (scaleSlider.localPosition.x - startPos.x)/maxDisplacement;
        if (poke.State == InteractableState.Select)
        {   
            isPressed = true;
        }
        else
        {
            isPressed = false;
        }

    }
}
