using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;
using System;
using UnityEngine.Experimental.Rendering.HDPipeline;

/* John Desnoyers-Stewart
 * 2018-04-05
 * tracks the position of kinect bodies' heads to change the perspective of a reflection camera used to project reflections of the virtual space on large wall projections.
 * 
 */

public class ReflectionTracker : MonoBehaviour
{

    public Transform mirrorNormal; //center of the mirror plane
    public GameObject cameraObject;     //the camera to move in sync with the head
    public Transform viveHead;          //the object to track when no bodies are detected
    public KinectBodyTracking bodyTracker;  //the source for the kinect body data
    public float projectionHeight = 1;      //height of the projection - can be scaled to make the projection larger than life
    public float projectionWidth = 1.7778f; //width of the projection - can be scaled to make the projection larger than life
    public float cameraZoom = 1.0f;         //camera zoom changes how far the camera is from the reflection plane to flatten the scale of objects in the room, or exaggerate proportions of objects which are closer and farther

    //variables for possible lighting fix in HDRP
    private HDAdditionalCameraData data;
    private HDAdditionalCameraData.NonObliqueProjectionGetter proj;

    private Matrix4x4 reflectionMat;

    private Camera mirrorCamera;

    // Use this for initialization
    void Start()
    {
        if (mirrorNormal == null)
        {
            mirrorNormal = transform;
        }
        if (cameraObject == null)
        {
            cameraObject = transform.GetChild(0).gameObject;

        }

        mirrorCamera = cameraObject.GetComponent<Camera>();


        //need to fix to get lighting to work in reflected matrix
        data = cameraObject.GetComponent<HDAdditionalCameraData>();
        proj = new HDAdditionalCameraData.NonObliqueProjectionGetter(FromCamera);
        
    }


    //Late update called after update so that bodies have been filtered
    void LateUpdate()
    {

        Vector3 headPosition = new Vector3();
        
        if (bodyTracker.GetNumBodies() > 0)
        {

            headPosition = bodyTracker.GetAvgHeadPosition(); //get the average position of all tracked bodies' heads

            
        }
        else if (viveHead != null)
        {
            headPosition = viveHead.position;   //get headset position
        }


        float offset = 0.07f;
        Vector3 mirroNormalUp = mirrorNormal.rotation * Vector3.forward;

        float d = -Vector3.Dot(mirrorNormal.position, mirroNormalUp) - offset;
        Vector4 reflectionPlane = new Vector4(mirroNormalUp.x, mirroNormalUp.y, mirroNormalUp.z, d);

        // get reflection matrix
        reflectionMat = Matrix4x4.zero;
        CalculateReflectionMatrix(ref reflectionMat, reflectionPlane);

        // set mirror camera position
        Vector3 reflectedPos = reflectionMat.MultiplyPoint(headPosition);

        mirrorCamera.transform.position = reflectedPos;

        mirrorCamera.transform.localPosition = new Vector3(mirrorCamera.transform.localPosition.x, mirrorCamera.transform.localPosition.y, mirrorCamera.transform.localPosition.z*cameraZoom);

        //mirrorCamera.transform.LookAt(mirrorNormal.position);
        //mirrorCamera.transform.position = Vector3.Reflect(headPosition - mirrorNormal.position, mirrorNormal.forward) + mirrorNormal.position;
        Debug.DrawLine(headPosition, mirrorNormal.position);
        Debug.DrawLine(mirrorNormal.position, Vector3.Reflect(Vector3.Reflect(headPosition - mirrorNormal.position, mirrorNormal.forward), mirrorNormal.up) + mirrorNormal.position);


        //expanded upon the thread at https://www.reddit.com/r/Unity3D/comments/3upfqf/need_your_help_creating_a_frustum/
        Vector3 cameraPos = mirrorCamera.transform.localPosition;
        
        //sets the camera frustrum equal to the projection size, puts the clipping plane at the projection plane
        float left = (projectionWidth/2) - cameraPos.x;
        float right = -(projectionWidth / 2) - cameraPos.x;
        float top = (projectionHeight / 2) - cameraPos.y;
        float bottom = -(projectionHeight / 2) - cameraPos.y;
        mirrorCamera.nearClipPlane = -cameraPos.z;

        //sets the projection matrix based on the location of the projection
        Matrix4x4 m = PerspectiveOffCenter(left, right, bottom, top, mirrorCamera.nearClipPlane, mirrorCamera.farClipPlane);
        mirrorCamera.projectionMatrix = m;


        //Beginning of fix for lighting in HDRP (not sure how to use)
        FromCamera(mirrorCamera);
        data.GetNonObliqueProjection(mirrorCamera);
    }

    public void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 normal)
    {
        reflectionMat.m00 = (1.0f - 2.0f * normal[0] * normal[0]);
        reflectionMat.m01 = (-2.0f * normal[0] * normal[1]);
        reflectionMat.m02 = (-2.0f * normal[0] * normal[2]);
        reflectionMat.m03 = (-2.0f * normal[3] * normal[0]);

        reflectionMat.m10 = (-2.0f * normal[1] * normal[0]);
        reflectionMat.m11 = (1.0f - 2.0f * normal[1] * normal[1]);
        reflectionMat.m12 = (-2.0f * normal[1] * normal[2]);
        reflectionMat.m13 = (-2.0f * normal[3] * normal[1]);

        reflectionMat.m20 = (-2.0f * normal[2] * normal[0]);
        reflectionMat.m21 = (-2.0f * normal[2] * normal[1]);
        reflectionMat.m22 = (1.0f - 2.0f * normal[2] * normal[2]);
        reflectionMat.m23 = (-2.0f * normal[3] * normal[2]);

        reflectionMat.m30 = 0.0f;
        reflectionMat.m31 = 0.0f;
        reflectionMat.m32 = 0.0f;
        reflectionMat.m33 = 1.0f;
    }

    //Beginning of fix for lighting in HDRP
    public Matrix4x4 FromCamera(Camera camera)
    {
        return camera.projectionMatrix;
    }

    /* From Unity Camera Projection Matrix sample 
     * https://docs.unity3d.com/ScriptReference/Camera-projectionMatrix.html
     */


    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }
}
