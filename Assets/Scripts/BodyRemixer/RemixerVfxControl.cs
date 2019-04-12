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

    public void IntializeVFX(GameObject source, GameObject target)
    {
        sourceBody = new VFXbody(source);
        targetBody = new VFXbody(target);
        
        if (visualFX == null)
            visualFX = GetComponent<VisualEffect>();

        visualFX.SetTexture("Source Position", sourceBody.position);
        visualFX.SetTexture("Source Velocity", sourceBody.velocity);
        visualFX.SetTexture("Target Position", targetBody.position);
        visualFX.SetVector3("SDF Position", sourceBody.body.transform.position);
    }

    public void IntializeVFX(GameObject source)
    {
        sourceBody = new VFXbody(source);

        visualFX.SetTexture("Source Position", sourceBody.position);
        visualFX.SetTexture("Source Velocity", sourceBody.velocity);
        visualFX.SetVector3("SDF Position",sourceBody.body.transform.position);

    }

    public class VFXbody
    {

        private MeshBakerManager manager;
        public GameObject body;
        public RenderTexture position;
        public RenderTexture velocity;
        public RenderTexture normal;

        public VFXbody(GameObject body)
        {
            manager = body.GetComponent<MeshBakerManager>();
            position = manager.positionMap;
            velocity = manager.velocityMap;
            normal = manager.normalMap;

        }

    }

}
