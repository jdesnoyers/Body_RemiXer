using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Smrvfx;

public class MeshBakerManager : MonoBehaviour
{

    [SerializeField] private SkinnedMeshRenderer skinnedMesh;
    [SerializeField] private RenderTexture renderTextureBase;
    

    public RenderTexture positionMap = null;
    public RenderTexture velocityMap = null;
    public RenderTexture normalMap = null;

    private bool meshActive = true;
    
    public void SetMeshActive(bool b)
    {
        meshActive = b;
        skinnedMesh.enabled = b;
    }

    void Start()
    {

        GetComponent<SkinnedMeshBaker>().Source = skinnedMesh; //link mesh to Skinned Mesh Baker

        //create new render textures to map vertices from skinned mesh to VFX Graph
        GetComponent<SkinnedMeshBaker>().PositionMap = new RenderTexture(renderTextureBase);
        GetComponent<SkinnedMeshBaker>().VelocityMap = new RenderTexture(renderTextureBase);
        GetComponent<SkinnedMeshBaker>().NormalMap = new RenderTexture(renderTextureBase);

        //link to Skinned Mesh Baker
        positionMap = GetComponent<SkinnedMeshBaker>().PositionMap;
        velocityMap = GetComponent<SkinnedMeshBaker>().VelocityMap;
        normalMap = GetComponent<SkinnedMeshBaker>().NormalMap;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
