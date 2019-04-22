using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Smrvfx;

public class RemixerVfxControl : MonoBehaviour
{

    public VFXbody sourceBody;
    public VFXbody targetBody;
    public VisualEffect visualFX;
    public float sdfHipOffset = -0.95157192f;
    [SerializeField] private BodyRemixerController remixer;
    private Vector3 sdfOffsetIntersect;
    private int numBodies = 0;
    
    // Start is called before the first frame update
    void Awake()
    {
        sdfOffsetIntersect = new Vector3(0.5f, 0.5f, 0.5f);
        if(remixer==null)
        {
            remixer = FindObjectOfType<BodyRemixerController>();
        }
    }

    private void Update()
    {
        if(visualFX.enabled)
        {
            //update VFX transform and SDF converter offset to match the current target's position
            if (targetBody != null)
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

            UpdateNumBodies(remixer.NumBodies);
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
        visualFX.SetTexture("Target Position", sourceBody.positionMap);
        visualFX.SetVector3("SDF Position", CalculateSdfPostion(sourceBody.hipTransform.position));
        visualFX.SetVector4("Source Color", color);
        visualFX.SetVector4("Target Color", color);

        sourceBody.meshSDF.vfxOutput = visualFX;
        sourceBody.meshSDF.offset = CalculateSdfOffset(sourceBody.armatureTransform.position);
    }

    public void SetTarget(GameObject target)
    {
        targetBody = new VFXbody(target);
        visualFX.SetVector3("SDF Position", CalculateSdfPostion(targetBody.hipTransform.position));
        visualFX.SetTexture("Target Position", targetBody.positionMap);

        //shut off the local SDF script if we're not using it -- CHECK FOR OFF MODE
        if (sourceBody.meshSDF.vfxOutput == visualFX)
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
    public void DisableLocalSDF()
    {
        sourceBody.meshSDF.enabled = false;
    }


    public void DeactivateVFX()
    {
        visualFX.enabled = false;
        //sourceBody.body.GetComponent<SkinnedMeshBaker>().enabled = false;
    }

    public void ActivateVFX()
    {
        visualFX.enabled = true;
        //sourceBody.body.GetComponent<SkinnedMeshBaker>().enabled = true;
    }
    public void ResetToSource()
    {
        //turn off meshSDF output and disable in target body to make sure our vfx don't get overridden
        //if(targetBody != null)
        //{
        //    if (targetBody.body != null)
        //    {
        //        targetBody.meshSDF.vfxOutput = null;
        //        targetBody.meshSDF.enabled = false;

                
        //    }

        //}

        targetBody = null; //then remove so we know that we don't have a target yet

        //enable source VFX and target at self
        if (sourceBody.meshSDF.vfxOutput != visualFX)
        {
            sourceBody.meshSDF.vfxOutput = visualFX;
            sourceBody.meshSDF.enabled = true;
        }

        //set to self
        visualFX.SetVector3("SDF Position", CalculateSdfPostion(sourceBody.hipTransform.position));
        visualFX.SetVector4("Target Color", visualFX.GetVector4("Source Color"));
        visualFX.SetTexture("Target Position", sourceBody.positionMap);

        sourceBody.meshSDF.vfxOutput = visualFX;
        sourceBody.meshSDF.offset = CalculateSdfOffset(sourceBody.armatureTransform.position);
    }

    public void SetTargetColor(Color color)
    {
        visualFX.SetVector4("Target Color", color);
    }

    public void AverageModeEnabled(bool mode)
    {
        visualFX.SetBool("Average Mode", mode);
    }

    public void UpdateNumBodies(int num)
    {
        visualFX.SetInt("Tracked Bodies", num);
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
