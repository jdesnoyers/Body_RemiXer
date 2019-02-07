using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//invert culling for mirror reflection cameras

public class InvertCulling : MonoBehaviour {

    void OnPreRender()
    {
        GL.invertCulling = true;
    }

    void OnPostRender()
    {
        GL.invertCulling = false;
    }
}
