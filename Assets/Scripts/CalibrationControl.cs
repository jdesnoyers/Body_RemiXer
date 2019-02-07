using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;

//for saving and adjusting calibration data between installation days

public class CalibrationControl : MonoBehaviour {

    //Transforms that can be calibrated using the keyboard
    public Transform keyboardOffset;
    public Transform kinectOffset;
    public Transform viveMirror;
    public Transform projectionReflection;
    public Transform fifthParallel;
    public FreezeRotation audioRotation;

    public KinectBodyTracking bodyTracker;

    public TextMeshPro frameRateDisplay;    //displays frame rate while adjusting calibration

    private bool unlocked = false;  //boolean to unlock the adjustment with a specific set of keys

    private float multiplier = 1.0f;
    
    
	void Update () {

        if (Input.GetKey(KeyCode.RightAlt))
        {
            //unlock the keyboard to allow transforms to be edited
            if (Input.GetKeyDown(KeyCode.Backslash))
            {
                unlocked = !unlocked;
                GetComponent<MeshRenderer>().enabled = unlocked;
                transform.GetChild(0).gameObject.SetActive(unlocked);
                
            }
        }

        //left shift switches to coarse adjustment (10x), right shift switches to ultra fine (0.1x)
        if (Input.GetKey(KeyCode.LeftShift))
        {
            multiplier = 10;
        }
        else if(Input.GetKey(KeyCode.RightShift))
        {
            multiplier = 0.1f;
        }
        else
        {
            multiplier = 1;
        }

        if(unlocked)
        {

            if(frameRateDisplay != null)
            {
                frameRateDisplay.text = "FPS:" + Mathf.RoundToInt(1f/Time.deltaTime);
            }

            //keys to control calibration, load and save calibrations and exit program
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }

            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    Debug.Log("Save");
                    Save();
                }

                if (Input.GetKeyDown(KeyCode.L))
                {
                    Debug.Log("Load");
                    Load();
                }
            }
            else if(Input.GetKey(KeyCode.Q)) //keyboard position
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 0.01f;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 0.01f;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 0.01f;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 0.01f;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 0.01f;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 0.01f;

                }

                keyboardOffset.localPosition += new Vector3(x, y, z);

            }
            else if (Input.GetKey(KeyCode.W)) //keyboard rotation
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 1f;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 1f;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 1f;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 1f;

                }

                keyboardOffset.localEulerAngles += new Vector3(x, y, z);

            }

            else if (Input.GetKey(KeyCode.E)) //kinect position
            {
                float x = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 0.1f;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 0.1f;

                }

                kinectOffset.position += new Vector3(x, 0, z);

            }

            else if (Input.GetKey(KeyCode.R)) //kinect rotation
            {
                float y = 0;
                float z = 0;

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 1;

                }

                kinectOffset.eulerAngles += new Vector3(0, y, z);

            }
            else if (Input.GetKey(KeyCode.T)) //Vive Mirror position
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 0.1f;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 0.1f;

                }

                viveMirror.position += new Vector3(x, y, z);

            }
            else if (Input.GetKey(KeyCode.Y)) //Vive Mirror rotation
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 1;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 1;

                }

                viveMirror.eulerAngles += new Vector3(x, y, z);

            }

            else if (Input.GetKey(KeyCode.U)) //Vive Mirror scale
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 0.1f;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 0.1f;

                }

                viveMirror.localScale += new Vector3(x, y, z);

            }


            else if (Input.GetKey(KeyCode.I)) //projection reflection position
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 0.1f;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 0.1f;

                }

                projectionReflection.position += new Vector3(x, y, z);

            }

            else if (Input.GetKey(KeyCode.O)) //projection reflection rotation
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 1;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 1;

                }

                projectionReflection.eulerAngles += new Vector3(x, y, z);

            }

            else if (Input.GetKey(KeyCode.P)) //reflection projection scale
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 0.1f;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 0.1f;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 1;

                }

                projectionReflection.GetComponent<ReflectionTracker>().projectionHeight += x;
                projectionReflection.GetComponent<ReflectionTracker>().projectionWidth += y;
                projectionReflection.GetComponent<ReflectionTracker>().cameraZoom += z;

            }

            else if (Input.GetKey(KeyCode.L)) //audio rotation
            {
                float x = 0;
                float y = 0;
                float z = 0;


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    x += multiplier * 1;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    x -= multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    y += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    y -= multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    z += multiplier * 1;

                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    z -= multiplier * 1;

                }

                audioRotation.xRotation += x;
                audioRotation.yRotation += y;
                audioRotation.zRotation += z;

            }
            

        }

	}

    //save the calibration in a serial binary file
    public void Save()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.persistentDataPath + "/calibration.dat", FileMode.OpenOrCreate);

        CalibrationData data = new CalibrationData();
        data.keyboardOffsetLocalPositionX = keyboardOffset.localPosition.x;
        data.keyboardOffsetLocalPositionY = keyboardOffset.localPosition.y;
        data.keyboardOffsetLocalPositionZ = keyboardOffset.localPosition.z;

        data.keyboardOffsetLocalEulerAnglesX = keyboardOffset.localEulerAngles.x;
        data.keyboardOffsetLocalEulerAnglesY = keyboardOffset.localEulerAngles.y;
        data.keyboardOffsetLocalEulerAnglesZ = keyboardOffset.localEulerAngles.z;

        data.kinectOffsetPositionX = kinectOffset.position.x;
        data.kinectOffsetPositionZ = kinectOffset.position.z;

        data.kinectOffsetEulerAnglesY = kinectOffset.eulerAngles.y;
        data.kinectOffsetEulerAnglesZ = kinectOffset.eulerAngles.z;

        data.kinectFloorOffset = bodyTracker.floorOffset;

        data.viveMirrorPositionX = viveMirror.position.x;
        data.viveMirrorPositionY = viveMirror.position.y;
        data.viveMirrorPositionZ = viveMirror.position.z;
        data.viveMirrorEulerAnglesX = viveMirror.eulerAngles.x;
        data.viveMirrorEulerAnglesY = viveMirror.eulerAngles.y;
        data.viveMirrorEulerAnglesZ = viveMirror.eulerAngles.z;
        data.viveMirrorLocalScaleX = viveMirror.localScale.x;
        data.viveMirrorLocalScaleY = viveMirror.localScale.y;
        data.viveMirrorLocalScaleZ = viveMirror.localScale.z;

        data.projectionReflectionPositionX = projectionReflection.position.x;
        data.projectionReflectionPositionY = projectionReflection.position.y;
        data.projectionReflectionPositionZ = projectionReflection.position.z;
        data.projectionReflectionEulerAnglesX = projectionReflection.eulerAngles.x;
        data.projectionReflectionEulerAnglesY = projectionReflection.eulerAngles.y;
        data.projectionReflectionEulerAnglesZ = projectionReflection.eulerAngles.z;
        data.projectionReflectionHeight = projectionReflection.GetComponent<ReflectionTracker>().projectionHeight;
        data.projectionReflectionWidth = projectionReflection.GetComponent<ReflectionTracker>().projectionWidth;
        data.projectionReflectionZoom = projectionReflection.GetComponent<ReflectionTracker>().cameraZoom;


        data.audioRotationLocalEulerAnglesX = audioRotation.xRotation;
        data.audioRotationLocalEulerAnglesY = audioRotation.yRotation;
        data.audioRotationLocalEulerAnglesZ = audioRotation.zRotation;

    bf.Serialize(file, data);
        file.Close();
    }

    //load calibration from a serial binary file
    public void Load()
    {
        if(File.Exists(Application.persistentDataPath + "/calibration.dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/calibration.dat",FileMode.Open);
            CalibrationData data = (CalibrationData) bf.Deserialize(file);
            file.Close();

            keyboardOffset.localPosition = new Vector3(data.keyboardOffsetLocalPositionX, data.keyboardOffsetLocalPositionY, data.keyboardOffsetLocalPositionZ);
            keyboardOffset.localEulerAngles = new Vector3(data.keyboardOffsetLocalEulerAnglesX, data.keyboardOffsetLocalEulerAnglesY, data.keyboardOffsetLocalEulerAnglesZ);

            kinectOffset.position = new Vector3(data.kinectOffsetPositionX, kinectOffset.position.y, data.kinectOffsetPositionZ);
            kinectOffset.eulerAngles = new Vector3(kinectOffset.eulerAngles.x, data.kinectOffsetEulerAnglesY, data.kinectOffsetEulerAnglesZ);

            bodyTracker.floorOffset = data.kinectFloorOffset;


            viveMirror.position = new Vector3(data.viveMirrorPositionX, data.viveMirrorPositionY, data.viveMirrorPositionZ);
            viveMirror.eulerAngles = new Vector3(data.viveMirrorEulerAnglesX, data.viveMirrorEulerAnglesY, data.viveMirrorEulerAnglesZ);
            viveMirror.localScale = new Vector3(data.viveMirrorLocalScaleX, data.viveMirrorLocalScaleY, data.viveMirrorLocalScaleZ);



            projectionReflection.position = new Vector3(data.projectionReflectionPositionX, data.projectionReflectionPositionY, data.projectionReflectionPositionZ);
            projectionReflection.eulerAngles = new Vector3(data.projectionReflectionEulerAnglesX, data.projectionReflectionEulerAnglesY, data.projectionReflectionEulerAnglesZ);

            projectionReflection.GetComponent<ReflectionTracker>().projectionHeight = data.projectionReflectionHeight;
            projectionReflection.GetComponent<ReflectionTracker>().projectionWidth = data.projectionReflectionWidth;
            projectionReflection.GetComponent<ReflectionTracker>().cameraZoom = data.projectionReflectionZoom;

            audioRotation.xRotation = data.audioRotationLocalEulerAnglesX;
            audioRotation.yRotation = data.audioRotationLocalEulerAnglesY;
            audioRotation.zRotation = data.audioRotationLocalEulerAnglesZ;

        }
    }
}

//class to save calibratio parameters
[Serializable]
class CalibrationData
{
    public float keyboardOffsetLocalPositionX;
    public float keyboardOffsetLocalPositionY;
    public float keyboardOffsetLocalPositionZ;
    public float keyboardOffsetLocalEulerAnglesX;
    public float keyboardOffsetLocalEulerAnglesY;
    public float keyboardOffsetLocalEulerAnglesZ;


    public float kinectOffsetPositionX;
    public float kinectOffsetPositionZ;
    public float kinectOffsetEulerAnglesY;
    public float kinectOffsetEulerAnglesZ;

    public float kinectFloorOffset;

    public float viveMirrorPositionX;
    public float viveMirrorPositionY;
    public float viveMirrorPositionZ;
    public float viveMirrorEulerAnglesX;
    public float viveMirrorEulerAnglesY;
    public float viveMirrorEulerAnglesZ;
    public float viveMirrorLocalScaleX;
    public float viveMirrorLocalScaleY;
    public float viveMirrorLocalScaleZ;

    public float projectionReflectionPositionX;
    public float projectionReflectionPositionY;
    public float projectionReflectionPositionZ;
    public float projectionReflectionEulerAnglesX;
    public float projectionReflectionEulerAnglesY;
    public float projectionReflectionEulerAnglesZ;
    public float projectionReflectionHeight;
    public float projectionReflectionWidth;
    public float projectionReflectionZoom;

    public float audioRotationLocalEulerAnglesX;
    public float audioRotationLocalEulerAnglesY;
    public float audioRotationLocalEulerAnglesZ;

    
}