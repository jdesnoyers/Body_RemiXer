using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Smrvfx;

public class MeshBakerManager : MonoBehaviour
{

    [SerializeField] private SkinnedMeshRenderer skinnedMesh;
    [SerializeField] private RenderTexture renderTextureBase;

    public RemixerVfxControl vfxControl;
    public Transform armatureTransform;
    public Transform hipTransform;

    [HideInInspector] public RenderTexture positionMap = null;
    [HideInInspector] public RenderTexture velocityMap = null;
    [HideInInspector] public RenderTexture normalMap = null;

    private bool meshActive = true;
    
    public void SetMeshActive(bool b)
    {
        meshActive = b;
        skinnedMesh.enabled = b;
    }

    void Awake()
    {

        if(armatureTransform == null)
        {
            armatureTransform = transform.Find("Armature");
        }

        if (hipTransform == null)
        {
            hipTransform = transform.Find("Hip");
        }



        SkinnedMeshBaker baker = GetComponent<SkinnedMeshBaker>();
        baker.Source = skinnedMesh; //link mesh to Skinned Mesh Baker

        //create new render textures to map vertices from skinned mesh to VFX Graph
        baker.PositionMap = new RenderTexture(renderTextureBase);
        baker.PositionMap.name = "position";
        baker.VelocityMap = new RenderTexture(renderTextureBase);
        baker.VelocityMap.name = "velocity";
        baker.NormalMap = new RenderTexture(renderTextureBase);
        baker.NormalMap.name = "normal";

        //link to Skinned Mesh Baker
        positionMap = baker.PositionMap;
        velocityMap = baker.VelocityMap;
        normalMap = baker.NormalMap;
        
    }
}
