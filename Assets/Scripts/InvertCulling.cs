using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//invert culling for mirror reflection cameras

public class InvertCulling : MonoBehaviour {

    private Camera _camera;
    private CommandBuffer _invertCull;
    private CommandBuffer _deInvertCull;

    private bool oldCulling;

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        _invertCull =  new CommandBuffer { name = "Invert Culling" };
        //_deInvertCull = new CommandBuffer { name = "Uninvert Culling" };
        _invertCull.SetInvertCulling(true);
        //_deInvertCull.SetInvertCulling(false);

        _camera.AddCommandBuffer(CameraEvent.BeforeDepthTexture, _invertCull);
        //_camera.AddCommandBuffer(CameraEvent.AfterFinalPass, _deInvertCull);

    }

    private void OnDisable()
    {
        if(_camera != null)
        {
            if (_invertCull != null)
                _camera.RemoveCommandBuffer(CameraEvent.BeforeDepthTexture, _invertCull);
            //if (_deInvertCull != null)
                //_camera.RemoveCommandBuffer(CameraEvent.BeforeDepthTexture, _deInvertCull);
        }
    }

    //added above to try to fix culling issue with Post Processing in 2018.3 to no avail...

    void OnPreRender()
    {
        oldCulling = GL.invertCulling;
        GL.invertCulling = true;
    }

    void OnPostRender()
    {
        GL.invertCulling = oldCulling;
    }
}
