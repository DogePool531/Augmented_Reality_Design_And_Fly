using UnityEngine;

public class AeroModel : MonoBehaviour
{

    public Transform fuselageNose;
    public Transform fuselageCentral;
    public Transform wing;
    public Transform tail;

    public Transform wingCenter;
    public Transform tailCenter;
    
    // Scale Variables
    public Vector3 fuselageScale;
    public Vector3 wingScale;
    public Vector3 tailScale;

    // Flight performance Variables
    public float wingArea;
    public float wingspan;
    public float horiArea;
    public float horispan;
    public float vertArea;
    public float vertspan;

    public float AspectRatio;
    public float k;
    public float Cl_a;
    public float Cd_0;

    public float horiAspectRatio;
    public float horik;
    public float horiCl_a;
    

    public float vertAspectRatio;
    public float vertk;
    public float vertCl_a;

    public float CG;
    public float totalMass;
    public float incedence;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fuselageScale = fuselageCentral.localScale;
        wingScale = wing.localScale;
        tailScale = tail.localScale;

    }

    // Update is called once per frame
    void Update()
    {
        //Scale declarations
        fuselageScale = fuselageCentral.localScale;
        wingScale = wing.localScale;
        tailScale = tail.localScale;

        //tail area and span
        horiArea = tailScale.x * tailScale.y * 0.015f;
        vertArea = tailScale.z * tailScale.y * 0.0075f;
        horispan = 0.4f * tailScale.x;
        vertspan = 0.2f * tailScale.y;

        // wing declarations
        wingArea = wingScale.y * wingScale.x * 0.02f;
        wingspan = wingScale.x * 0.2f;
        AspectRatio = wingspan * wingspan / wingArea;
        k = 1/(3.1415f * 0.7f * AspectRatio);
        float a_0 = 2f*3.1415f;
        Cl_a = a_0/(1f + a_0*k);

        //wingArea = wingScale.y * wingScale.x * 0.02f;
        
        horiAspectRatio = horispan * horispan / horiArea;
        horik = 1/(3.1415f * 0.9f * horiAspectRatio);
        horiCl_a = a_0/(1f + a_0*horik);

        // vertical tail calcs
        
        vertAspectRatio = vertspan * vertspan / vertArea;
        vertk = 1/(3.1415f * 0.9f * vertAspectRatio);
        vertCl_a = a_0/(1f + a_0*vertk);

        // CG calculation

        
        float fuselageMass = 0.6f + fuselageScale.y;
        float wingMass = 0.3f * wing.localScale.x  * wing.localScale.y;
        float tailMass = 0.3f * tail.localScale.x  * tail.localScale.y * tail.localScale.z;
        totalMass = wingMass + tailMass + fuselageMass;
        float CGTotal = wingMass * wingCenter.localPosition.z + tailMass * tailCenter.localPosition.z + fuselageMass * (fuselageCentral.localPosition.z + 0.6f * fuselageCentral.localScale.z/2);
        CG = CGTotal/totalMass;
        totalMass = totalMass / 15;


        incedence = (horiCl_a * tailCenter.localPosition.z * horiArea + Cl_a * wingArea * wingCenter.localPosition.z) / (horiArea * horiCl_a * tailCenter.localPosition.z);

        
    }
}
