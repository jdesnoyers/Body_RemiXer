using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class RemixerVfxControl : MonoBehaviour
{

    public VFXbody sourceBody;
    public VFXbody targetBody;
    [SerializeField] private VisualEffect visualFX;

    // Start is called before the first frame update
    void Start()
    {
    }

    private void Update()
    {
        //update VFX transform and SDF converter offset to match the current target's position
        Vector3 targetPos = targetBody.body.transform.position;
        visualFX.SetVector3("SDF Position", targetPos);
        targetBody.meshSDF.offset = (-0.5f*targetPos) + new Vector3(0.5f,0.5f,0.5f);

    }

    public void IntializeVFX(GameObject source, GameObject target, Color sourceColor, Color targetColor)
    {
        sourceBody = new VFXbody(source);
        targetBody = new VFXbody(target);
        
        if (visualFX == null)
            visualFX = GetComponent<VisualEffect>();

        Vector3 targetPos = targetBody.body.transform.position;

        //set up VFX with textures and colours
        visualFX.SetTexture("Source Position", sourceBody.positionMap);
        visualFX.SetTexture("Source Velocity", sourceBody.velocityMap);
        visualFX.SetVector3("SDF Position", targetPos);
        visualFX.SetVector4("Source Color", sourceColor);
        visualFX.SetVector4("Target Color", targetColor);

        if(sourceBody.meshSDF == null)
        {
            sourceBody.meshSDF.vfxOutput = visualFX;
        }

        //set up SDF 
        targetBody.meshSDF.vfxOutput = visualFX;
        targetBody.meshSDF.offset = (-0.5f * targetPos) + new Vector3(0.5f, 0.5f, 0.5f);
    }

    public void IntializeVFX(GameObject source)
    {
        sourceBody = new VFXbody(source);

        visualFX.SetTexture("Source Position", sourceBody.positionMap);
        visualFX.SetTexture("Source Velocity", sourceBody.velocityMap);
        visualFX.SetVector3("SDF Position",sourceBody.body.transform.position);

        sourceBody.meshSDF.vfxOutput = visualFX;
    }

    //class to simplify calling relevant things from the target and source bodies
    public class VFXbody
    {

        private MeshBakerManager manager;
        public GameObject body;
        public RenderTexture positionMap;
        public RenderTexture velocityMap;
        public RenderTexture normalMap;
        public MeshToSDF meshSDF;

        public VFXbody(GameObject body)
        {
            manager = body.GetComponent<MeshBakerManager>();
            positionMap = manager.positionMap;
            velocityMap = manager.velocityMap;
            normalMap = manager.normalMap;
            meshSDF = body.GetComponent<MeshToSDF>();

        }

    }

}
