using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class RemixerVfxControl : MonoBehaviour
{

    public VFXbody sourceBody;
    public VFXbody targetBody;
    public VisualEffect visualFX;
    public float sdfHipOffset = -0.95157192f;
    private Vector3 sdfOffsetIntersect;
    
    // Start is called before the first frame update
    void Awake()
    {
        sdfOffsetIntersect = new Vector3(0.5f, 0.5f, 0.5f);
    }

    private void Update()
    {
        //update VFX transform and SDF converter offset to match the current target's position
        if(targetBody != null)
        {
            //Vector3 targetPos = targetBody.armatureTransform.position;
            visualFX.SetVector3("SDF Position", CalculateSdfPostion(targetBody.hipTransform.position));
            targetBody.meshSDF.offset = CalculateSdfOffset(targetBody.armatureTransform.position);
        }
        else //if there is no target body then set the SDF to self
        {
            //Vector3 sourcePos = sourceBody.rootTransform.position;
            visualFX.SetVector3("SDF Position", CalculateSdfPostion(sourceBody.hipTransform.position));
            sourceBody.meshSDF.offset = CalculateSdfOffset(sourceBody.armatureTransform.position);
        }

    }

    public void IntializeVFX(GameObject source, GameObject target, Color sourceColor, Color targetColor)
    {
        sourceBody = new VFXbody(source);
        targetBody = new VFXbody(target);
        
        if (visualFX == null)
            visualFX = GetComponent<VisualEffect>();

        //Vector3 targetPos = targetBody.rootTransform.position;

        //set up VFX with textures and colours
        visualFX.SetTexture("Source Position", sourceBody.positionMap);
        visualFX.SetTexture("Source Velocity", sourceBody.velocityMap);
        visualFX.SetVector3("SDF Position", CalculateSdfPostion(targetBody.hipTransform.position));
        visualFX.SetVector4("Source Color", sourceColor);
        visualFX.SetVector4("Target Color", targetColor);

        if(sourceBody.meshSDF == null)
        {
            sourceBody.meshSDF.vfxOutput = visualFX;
        }

        //set up SDF 
        targetBody.meshSDF.vfxOutput = visualFX;
        targetBody.meshSDF.offset = CalculateSdfOffset(targetBody.armatureTransform.position);
    }

    public void IntializeVFX(GameObject source, Color color)
    {
        sourceBody = new VFXbody(source);

        //Vector3 sourcePos = CalculateSdfPostion(sourceBody.rootTransform.position);

        visualFX.SetTexture("Source Position", sourceBody.positionMap);
        visualFX.SetTexture("Source Velocity", sourceBody.velocityMap);
        visualFX.SetVector3("SDF Position", CalculateSdfPostion(sourceBody.hipTransform.position));
        visualFX.SetVector4("Source Color", color);
        visualFX.SetVector4("Target Color", color);

        sourceBody.meshSDF.vfxOutput = visualFX;
        sourceBody.meshSDF.offset = CalculateSdfOffset(sourceBody.armatureTransform.position);
    }

    public void SetTarget(GameObject target)
    {
        targetBody = new VFXbody(target);
        visualFX.SetVector3("SDF Position", CalculateSdfPostion(sourceBody.hipTransform.position));

        //shut off the local SDF script if we're not using it
        if (sourceBody.meshSDF.vfxOutput = visualFX)
        {
            sourceBody.meshSDF.vfxOutput = null;
            sourceBody.meshSDF.enabled = false;
        }

        //if the target SDF was shut off, turn it on again
        if (!targetBody.meshSDF.enabled)
        {
            targetBody.meshSDF.enabled = true;
        }
        targetBody.meshSDF.vfxOutput = visualFX;
        targetBody.meshSDF.offset = CalculateSdfOffset(targetBody.armatureTransform.position);

    }

    public void SetTargetColor(Color color)
    {
        visualFX.SetVector4("Target Color", color);
    }

    public void AverageModeEnabled(bool mode)
    {
        visualFX.SetBool("Average Mode", mode);
    }

    private Vector3 CalculateSdfOffset(Vector3 pos)
    {
        Vector3 posXZ = new Vector3(pos.x, 0, pos.z);
        return (-0.5f * posXZ) + sdfOffsetIntersect;
    }

    private Vector3 CalculateSdfPostion(Vector3 pos)
    {
        Vector3 correctedPos = new Vector3(pos.x, pos.y + sdfHipOffset, pos.z);
        return correctedPos;
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
        public Transform hipTransform;
        public Transform armatureTransform;

        public VFXbody(GameObject bod)
        {
            body = bod;
            manager = body.GetComponent<MeshBakerManager>();
            positionMap = manager.positionMap;
            velocityMap = manager.velocityMap;
            normalMap = manager.normalMap;
            meshSDF = body.GetComponent<MeshToSDF>();
            armatureTransform = manager.armatureTransform;
            hipTransform = manager.hipTransform;

        }

    }

}
