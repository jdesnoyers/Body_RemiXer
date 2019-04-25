/* Created 2019-03-24
 * Works with KinectBodyTracking.cs V2
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum RemixMode { off, average, exquisite, shiva, swap };


public class BodyRemixerController : MonoBehaviour
{

    public RemixMode remixMode = RemixMode.off;
    private RemixMode oldRemixMode = RemixMode.off;
    public bool shivaFirstPersonMode = false;
    public bool oldBody = false;
    public GameObject BodyPrefab;
    //public GameObject thirdPersonParent; //not used
    public GameObject VrHeadset;
    public float HeadRadius = 0.2f;

    [SerializeField] private KinectBodyTracking bodyTracker;

    public bool positionJoints = true;
    public bool scaleJoints = true;

    public int NumBodies { get; private set; } = 0;

    private GameObject thirdPersonBody;

    private bool thirdPersonOld;

    [ColorUsage(true, true)] public Color[] bodyColors = new Color[6];
    private Dictionary<ulong, Color> _bodyColors = new Dictionary<ulong, Color>();

    [HideInInspector] public UnityAction ikAction;

    private Dictionary<ulong, Dictionary<string, GameObject>> kinectJointMap = new Dictionary<ulong, Dictionary<string, GameObject>>();
    private Dictionary<ulong, GameObject> meshBodies = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, GameObject[]> meshJointMap = new Dictionary<ulong, GameObject[]>();
    private List<ulong> knownMeshIds = new List<ulong>();



    private GameObject[] _thirdPersonJointMap;
    private Dictionary<ulong, GameObject> shivaThirdBodies = new Dictionary<ulong, GameObject>();   //dictionary of Shiva Third Person joints
    private Dictionary<ulong, GameObject[]> shivaThirdJointMap = new Dictionary<ulong, GameObject[]>();   //dictionary of Shiva Third Person joints

    private Dictionary<ulong, GameObject> remixerBodies = new Dictionary<ulong, GameObject>(); //dictionary of average bodies to keep track of bodies used for exquisite corpse and averager
    private Dictionary<ulong, GameObject[]> remixerJointMap = new Dictionary<ulong, GameObject[]>();   //dictionary of average joints to access each joint

    private Dictionary<ulong, Dictionary<ulong, GameObject>> shivaBodies = new Dictionary<ulong, Dictionary<ulong, GameObject>>(); //2D dictionary to access each shiva body
    private Dictionary<ulong, Dictionary<ulong, GameObject[]>> shivaJointMap = new Dictionary<ulong, Dictionary<ulong, GameObject[]>>(); //2D dictionary to access each shiva body

    private Dictionary<ulong, ulong> swapBodies = new Dictionary<ulong, ulong>();

    private Transform kinectTransform;
    private Quaternion convertedRotation;
    private GameObject[] joints;
    private float _headRadSq;
    private Quaternion zeroQuaternion = new Quaternion(0, 0, 0, 0);
    private Vector3 flatScale = new Vector3(0, 1, 1);

    //for use with old body
    /*
    private string[] meshJointNamesOld =
    {
    "Neo",
        "Neo_Reference",
            "Neo_Hips",
                "Neo_LeftUpLeg",
                     "Neo_LeftLeg",
                         "Neo_LeftFoot",
                            "Neo_LeftToeBase",
                "Neo_RightUpLeg",
                    "Neo_RightLeg",
                        "Neo_RightFoot",
                            "Neo_RightToeBase",
                "Neo_Spine",
                    "Neo_Spine1",
                        "Neo_Spine2",
                            "Neo_LeftShoulder",
                                "Neo_LeftArm",
                                    "Neo_LeftForeArm",
                                        "Neo_LeftHand",
                            "Neo_RightShoulder",
                                "Neo_RightArm",
                                    "Neo_RightForeArm",
                                        "Neo_RightHand",
                            "Neo_Neck",
                                "Neo_Head",

    };*/

    private string[] meshJointNames =
    {
    "Root",
        "mixamorig",
            "mixamorig_Hips",
                "mixamorig_LeftUpLeg",
                     "mixamorig_LeftLeg",
                         "mixamorig_LeftFoot",
                            "mixamorig_LeftToeBase",
                "mixamorig_RightUpLeg",
                    "mixamorig_RightLeg",
                        "mixamorig_RightFoot",
                            "mixamorig_RightToeBase",
                "mixamorig_Spine",
                    "NOT_USED",
                        "mixamorig_Spine2",
                            //"mixamorig_LeftShoulder",
                                "mixamorig_LeftArm",
                                    "mixamorig_LeftForeArm",
                                        "mixamorig_LeftHand",
                                            "mixamorig_LeftHandIndex1",
                            //"mixamorig_RightShoulder",
                                "mixamorig_RightArm",
                                    "mixamorig_RightForeArm",
                                        "mixamorig_RightHand",
                                            "mixamorig_RightHandIndex1",
                            "mixamorig_Neck",
                                "mixamorig_Head",

    };

    //add ThumbLeft ThumbRight HandTipLeft and HandTipRight along with corresponding joints above
    private string[] kinectJointNames =
    {
    "SpineBase",
        "SpineBase",
            "SpineBase",
                "HipLeft",
                     "KneeLeft",
                         "AnkleLeft",
                            "FootLeft",
                "HipRight",
                     "KneeRight",
                         "AnkleRight",
                            "FootRight",
                "SpineMid", //moved up one
                    null,
                        "SpineShoulder", //added for MixamoRig
                            //null,
                                "ShoulderLeft",
                                    "ElbowLeft",
                                        "WristLeft",
                                            "HandLeft",
                            //null,
                                "ShoulderRight",
                                    "ElbowRight",
                                        "WristRight",
                                            "HandRight",
                            "Neck",
                                "Head",
    };


    // Start is called before the first frame update
    void Start()
    {

        ikAction += MeshBodyIK; //Set up inverse Kinematics - not currently working

        _headRadSq = HeadRadius * HeadRadius; //square radius for later use

        bodyTracker.DelegateBodies += UpdateRemixer; //add remixer to delegate function in body tracker


        thirdPersonBody = CreateThirdPersonBody();
        thirdPersonBody.GetComponent<MeshBakerManager>().vfxControl.IntializeVFX(thirdPersonBody, Color.black);
        thirdPersonBody.GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(true);

        //thirdPersonToggle.AddListener(thirdPersonBody.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive);

    }

    //Late Update is used to ensure we have the data from the Kinect updated first
    void LateUpdate()
    {

        //toggle event for third person view
        /*if (thirdPerson != thirdPersonOld)
        {
            thirdPersonToggle.Invoke(thirdPerson);
            thirdPersonOld = thirdPerson;
        }*/

        //toggle event for seeing bodies

        if (bodyTracker.trackedIds.Count != 0)
        {
            switch (remixMode)
            {
                case RemixMode.off:
                    {
                        break;
                    }
                case RemixMode.average:
                    {
                        UpdateAverageBody();
                        break;
                    }
                case RemixMode.exquisite:
                    {
                        UpdateExquisiteCorpse();
                        break;
                    }
                case RemixMode.shiva:
                    {
                        break;
                    }

            }

            if (remixMode != oldRemixMode)
            {
                switch (remixMode)
                {
                    case RemixMode.off:
                        {
                            List<ulong> trackIds = bodyTracker.trackedIds;
                            if (bodyTracker.trackedIds.Count > 1)
                            {
                                for (int i = 0; i < trackIds.Count; i += 2)
                                {

                                    ulong bodyA = trackIds[i];
                                    ulong bodyB = swapBodies[trackIds[i]];
                                    if (meshBodies[trackIds[i]] != null)
                                    {
                                        meshBodies[bodyA].GetComponent<MeshBakerManager>().vfxControl.SetTarget(meshBodies[bodyB]);
                                        meshBodies[bodyA].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyB]);
                                        meshBodies[bodyB].GetComponent<MeshBakerManager>().vfxControl.SetTarget(meshBodies[bodyA]);
                                        meshBodies[bodyB].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyA]);
                                    }
                                }

                                //if there is an odd number of bodies, reset the last one to self
                                if (trackIds.Count % 2 != 0)
                                {
                                    meshBodies[trackIds[trackIds.Count - 1]].GetComponent<MeshBakerManager>().vfxControl.ResetToSource();
                                }
                            }
                            else if (bodyTracker.trackedIds.Count == 1)
                            {
                                meshBodies[trackIds[0]].GetComponent<MeshBakerManager>().vfxControl.ResetToSource();
                            }

                            foreach (ulong trackingId in bodyTracker.trackedIds)
                            {
                                if (oldRemixMode == RemixMode.shiva)
                                {
                                    CleanUpShivaBodies(trackingId);
                                }
                                else if (oldRemixMode == RemixMode.average)
                                {
                                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(false);
                                }
                                /*else if (oldRemixMode == RemixMode.exquisite)
                                {
                                    if (remixerBodies.ContainsKey(trackingId))
                                    {
                                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.DeactivateVFX();
                                    }
                                }*/
                                CleanUpRemixerBodies(trackingId);

                            }

                            thirdPersonBody.SetActive(false);

                            break;
                        }
                    case RemixMode.average:
                        {

                            //activate the third-person body if it isn't currently
                            if (!thirdPersonBody.activeSelf)
                            {
                                thirdPersonBody.SetActive(true);
                            }

                            foreach (ulong trackingId in bodyTracker.trackedIds)
                            {
                                if (meshBodies.ContainsKey(trackingId))
                                {
                                    if (remixerBodies.ContainsKey(trackingId))
                                    {
                                        meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(remixerBodies[trackingId]);
                                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(meshBodies[trackingId]);
                                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);
                                    }

                                    if (oldRemixMode == RemixMode.shiva)
                                    {
                                        CleanUpShivaBodies(trackingId);//clean up other remixer bodies
                                    }
                                    /*else if (oldRemixMode == RemixMode.exquisite)
                                    {
                                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.DeactivateVFX();
                                    }*/
                                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(true);

                                }
                            }


                            thirdPersonBody.GetComponent<MeshBakerManager>().vfxControl.ActivateVFX();
                            //thirdPersonBody.GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(true); //shouldn't be needed since it should always be in average mode

                            break;
                        }
                    case RemixMode.exquisite:
                        {

                            //activate the third-person body if it isn't currently
                            if (!thirdPersonBody.activeSelf)
                            {
                                thirdPersonBody.SetActive(true);
                            }

                            foreach (ulong trackingId in bodyTracker.trackedIds)
                            {
                                if (meshBodies.ContainsKey(trackingId))
                                {

                                    if (remixerBodies.ContainsKey(trackingId))
                                    {
                                        meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(remixerBodies[trackingId]);
                                        meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.DisableLocalSDF();
                                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(thirdPersonBody);
                                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);
                                    }

                                    if (oldRemixMode == RemixMode.shiva)
                                    {
                                        CleanUpShivaBodies(trackingId);//clean up other remixer bodies
                                    }


                                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(false);
                                    //remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.ActivateVFX();

                                }

                            }
                            thirdPersonBody.GetComponent<MeshBakerManager>().vfxControl.DeactivateVFX();

                            break;
                        }
                    case RemixMode.shiva:
                        {
                            //deactivate third person body
                            if (thirdPersonBody.activeSelf)
                            {
                                thirdPersonBody.SetActive(false);
                            }

                            foreach (ulong trackingId in bodyTracker.trackedIds)
                            {
                                if (meshBodies.ContainsKey(trackingId) && shivaThirdBodies.ContainsKey(trackingId))
                                {
                                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(remixerBodies[trackingId]);
                                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);
                                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.DisableLocalSDF();
                                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(false);
                                    remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(shivaThirdBodies[trackingId]);
                                    remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);
                                }

                                //CleanUpRemixerBodies(trackingId);
                            }

                            if (thirdPersonBody.activeSelf)
                            {
                                thirdPersonBody.SetActive(false);
                            }

                            break;
                        }

                    case RemixMode.swap:
                        {
                            foreach (ulong trackingId in bodyTracker.trackedIds)
                            {

                                if (meshBodies.ContainsKey(trackingId))
                                {


                                    if (remixerBodies.ContainsKey(trackingId))
                                    {
                                        meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(remixerBodies[trackingId]);
                                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(meshBodies[trackingId]);
                                    }


                                    if (oldRemixMode == RemixMode.shiva)
                                    {
                                        CleanUpShivaBodies(trackingId);//clean up other remixer bodies
                                    }


                                    if (oldRemixMode == RemixMode.average)
                                    {
                                        meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(false);
                                    }
                                    /*else if (oldRemixMode == RemixMode.exquisite)
                                    {
                                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.DeactivateVFX();
                                    }*/
                                }
                            }

                            //if there's more than one tracked body, set target colors to paired body
                            if (bodyTracker.trackedIds.Count > 1)
                            {
                                List<ulong> trackIds = bodyTracker.trackedIds;
                                for (int i = 1; i < trackIds.Count; i += 2)
                                {
                                    if (swapBodies.ContainsKey(trackIds[i]))
                                    {
                                        ulong bodyA = trackIds[i];
                                        ulong bodyB = swapBodies[trackIds[i]];
                                        meshBodies[bodyA].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyB]);
                                        meshBodies[bodyB].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyA]);
                                        remixerBodies[bodyA].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyA]);
                                        remixerBodies[bodyB].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyB]);
                                    }
                                }

                            }

                            thirdPersonBody.SetActive(false);

                            break;

                        }

                }

                oldRemixMode = remixMode;
            }

            //update bodies depending on mode
            foreach (ulong trackingId in bodyTracker.trackedIds)
            {
                RefreshMeshBodyObject(trackingId);

                switch (remixMode)
                {
                    case RemixMode.off:
                        {
                            break;
                        }
                    case RemixMode.average:
                        {
                            UpdateRemixBodies(trackingId);
                            break;
                        }
                    case RemixMode.exquisite:
                        {
                            UpdateRemixBodies(trackingId);
                            break;
                        }
                    case RemixMode.shiva:
                        {

                            if (shivaFirstPersonMode)
                            {
                                foreach (ulong shivaId in bodyTracker.trackedIds)
                                {
                                    UpdateShivaBodies(trackingId, shivaId);

                                }

                            }

                            UpdateRemixBodiesFromSelf(trackingId);
                            UpdateShivaThird(trackingId);

                            break;
                        }
                    case RemixMode.swap:
                        {
                            if (swapBodies.ContainsKey(trackingId) && meshJointMap.ContainsKey(swapBodies[trackingId]))
                            {
                                UpdateSwapBodies(trackingId, swapBodies[trackingId]);
                            }
                            else
                            {
                                if (remixerBodies.ContainsKey(trackingId) && meshBodies.ContainsKey(trackingId))
                                {
                                    UpdateSwapBodies(trackingId, trackingId);

                                }
                            }
                            break;
                        }

                }
            }
        }

    }


    //delegate function called through Kinect body tracking to keep things synchronized
    void UpdateRemixer()
    {

        knownMeshIds = new List<ulong>(meshBodies.Keys);

        //update number of bodies for averagers
        NumBodies = bodyTracker.trackedIds.Count;

        // First delete untracked bodies
        foreach (ulong trackingId in knownMeshIds)
        {
            if (!bodyTracker.trackedIds.Contains(trackingId))
            {

                //if there is an entry in the swap bodies dictionary for this ID, clean them up and remove
                if (swapBodies.ContainsKey(trackingId))
                {
                    //if particles are exchanging between individual meshes 
                    if (remixMode == RemixMode.off)
                    {
                        meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.ResetToSource(); //might be redundant
                        meshBodies[swapBodies[trackingId]].GetComponent<MeshBakerManager>().vfxControl.ResetToSource();

                    }
                    swapBodies.Remove(swapBodies[trackingId]); //remove connected entry
                    swapBodies.Remove(trackingId);  //remove entry

                }

                //make sure that the last mesh gets reset - MIGHT BE ABLE TO REMOVE***
                //if (NumBodies == 1)
                //{
                //    meshBodies[knownMeshIds[0]].GetComponent<MeshBakerManager>().vfxControl.ResetToSource();
                //}

                Destroy(meshBodies[trackingId]);
                meshBodies.Remove(trackingId);
                meshJointMap.Remove(trackingId);


                _bodyColors.Remove(trackingId);

                CleanUpRemixerBodies(trackingId);

                CleanUpShivaBodies(trackingId);

            }
        }

        knownMeshIds = new List<ulong>(bodyTracker.trackedIds);

        //turn off third person body when there is nobody around
        if (bodyTracker.trackedIds.Count == 0 && thirdPersonBody.activeSelf)
        {
            thirdPersonBody.SetActive(false);
        }

        //then add newly tracked bodies
        //foreach (ulong trackingId in bodyTracker.trackedIds)
        for (int i = 0; i < bodyTracker.trackedIds.Count; i++)
        {
            ulong trackingId = bodyTracker.trackedIds[i];
            if (!meshBodies.ContainsKey(trackingId))
            {
                meshBodies[trackingId] = CreateMeshBody(trackingId);
                _bodyColors.Add(trackingId, bodyColors[i]);  //first color gets assigned twice on first entry of second body
                meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.IntializeVFX(meshBodies[trackingId], _bodyColors[trackingId]);
            }

            if (remixMode != RemixMode.off)
            {
                if (!remixerBodies.ContainsKey(trackingId))
                {
                    //set up the remixer body and initialize the VFX for that body
                    remixerBodies[trackingId] = CreateRemixerBody(trackingId);
                    remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.IntializeVFX(remixerBodies[trackingId], _bodyColors[trackingId]);

                    //set the target of the mesh body to the remixer body
                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(remixerBodies[trackingId]);
                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);

                    //activate the "Third Person Body" in average and exquisite corpse modes
                    if ((remixMode == RemixMode.average || remixMode == RemixMode.exquisite) && !thirdPersonBody.activeSelf)
                    {
                        thirdPersonBody.SetActive(true);
                    }

                    if (remixMode == RemixMode.average)
                    {
                        //turn on average color mode and set target of remixer body to the mesh body (reciprocal of the other VFX system)
                        thirdPersonBody.GetComponent<MeshBakerManager>().vfxControl.ActivateVFX();
                        meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(true);
                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(meshBodies[trackingId]);
                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);
                    }
                    else if (remixMode == RemixMode.exquisite)
                    {
                        //set target of remixer body to the third person body
                        meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.DisableLocalSDF();
                        thirdPersonBody.GetComponent<MeshBakerManager>().vfxControl.DeactivateVFX();
                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(thirdPersonBody);
                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);
                    }
                    else if (remixMode == RemixMode.swap)
                    {
                        //set target of remixer body to the mesh body (reciprocal of the other VFX system)
                        remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(meshBodies[trackingId]);
                    }
                    else if (remixMode == RemixMode.shiva)
                    {
                        if (shivaFirstPersonMode)
                        {
                            if (!shivaBodies.ContainsKey(trackingId))
                            {
                                shivaBodies.Add(trackingId, new Dictionary<ulong, GameObject>());

                                //add dictionary entry for each other new body and sub-dictionary entries for all

                                foreach (ulong trackId in bodyTracker.trackedIds)
                                {

                                    if (!shivaBodies[trackingId].ContainsKey(trackId))
                                    {
                                        shivaBodies[trackingId].Add(trackId, CreateShivaBody(trackingId, trackId));
                                    }
                                    if (!shivaBodies[trackId].ContainsKey(trackingId))
                                    {
                                        shivaBodies[trackId].Add(trackingId, CreateShivaBody(trackId, trackingId));
                                    }

                                }

                            }
                        }


                        //add dictionary for this new tracked body



                        //clean up other remixer bodies

                    }

                }

                if (!shivaThirdBodies.ContainsKey(trackingId) && remixMode == RemixMode.shiva)
                {
                    //set up the Shiva third person body and initialize the VFX
                    shivaThirdBodies[trackingId] = CreateThirdShiva(trackingId);
                    shivaThirdBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.IntializeVFX(shivaThirdBodies[trackingId], _bodyColors[trackingId]);

                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);
                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.DisableLocalSDF();
                    meshBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(false);

                    remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTarget(shivaThirdBodies[trackingId]);
                    remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[trackingId]);

                    remixerBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.AverageModeEnabled(false);
                    shivaThirdBodies[trackingId].GetComponent<MeshBakerManager>().vfxControl.DeactivateVFX();

                }
            }
            else
            {
                CleanUpRemixerBodies(trackingId);
                CleanUpShivaBodies(trackingId);
                thirdPersonBody.SetActive(false);
            }

        }

        //for direct body swap populate dictionary of pairs when possible
        if (bodyTracker.trackedIds.Count > 1)
        {
            List<ulong> trackIds = bodyTracker.trackedIds;
            for (int i = 1; i < trackIds.Count; i += 2)
            {
                if (!swapBodies.ContainsKey(trackIds[i]))
                {
                    swapBodies.Add(trackIds[i], trackIds[i - 1]);
                    swapBodies.Add(trackIds[i - 1], trackIds[i]);

                    ulong bodyA = trackIds[i];
                    ulong bodyB = swapBodies[trackIds[i]];
                    if (remixMode == RemixMode.off)
                    {
                        meshBodies[bodyA].GetComponent<MeshBakerManager>().vfxControl.SetTarget(meshBodies[bodyB]);
                        meshBodies[bodyA].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyB]);
                        meshBodies[bodyB].GetComponent<MeshBakerManager>().vfxControl.SetTarget(meshBodies[bodyA]);
                        meshBodies[bodyB].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyA]);
                    }
                    else if (remixMode == RemixMode.swap)
                    {
                        meshBodies[bodyA].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyB]);
                        meshBodies[bodyB].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyA]);
                        remixerBodies[bodyA].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyA]);
                        remixerBodies[bodyB].GetComponent<MeshBakerManager>().vfxControl.SetTargetColor(_bodyColors[bodyB]);

                    }
                }
            }

        }

    }

    //refreshes base mesh body objects
    //the data from each of these are copied into the other remixer bodies
    private void RefreshMeshBodyObject(ulong id)
    {

        /* Algorithm
         * 1. position and rotate joints based on kinect joints
         * 2. scale based on height? (based on distance between element and spine base)
         * 3. scale to 0 to remove untracked limbs
         * 
         */
        if (meshJointMap.ContainsKey(id))
        {
            joints = meshJointMap[id];
        }
        else
            Debug.Log(id + " was not present in mesh body joint map dictionary");

        Dictionary<string, GameObject> kinectJoints = kinectJointMap[id];

        //position all joints based on kinect transforms
        for (int i = 0; i < joints.Length; i++)
        {
            string findJoint = meshJointNames[i];

            if (kinectJoints.ContainsKey(findJoint))
            {
                Transform kinectTransform = kinectJoints[findJoint].transform;

                //joints[i].transform.position = kinectTransform.position;

                if (!kinectTransform.rotation.Equals(zeroQuaternion))    //don't rotate joints if we don't have a value from the kinect
                {
                    if (oldBody)
                    {
                        if (i < 2) //if its one of the main body components then the rotation is different from the rest of the joints
                        {
                            convertedRotation = Quaternion.AngleAxis(180, kinectTransform.up) * kinectTransform.rotation;

                        }
                        else if (i == 3 || i == 4 || i == 5 || i == 6) //rotations for left leg (except ankle)
                        {
                            convertedRotation = Quaternion.AngleAxis(-90, kinectTransform.forward) * kinectTransform.rotation;
                        }
                        else if (i == 7 || i == 8 || i == 9 || i == 10) //rotations for right leg (except ankle)
                        {
                            convertedRotation = Quaternion.AngleAxis(180, kinectTransform.up) * Quaternion.AngleAxis(90, kinectTransform.forward) * kinectTransform.rotation;
                        }
                        else if (i > 17 && i < 22) //rotations for 18-21 are inverted in the model
                        {
                            convertedRotation = Quaternion.AngleAxis(-90, kinectTransform.right) * Quaternion.AngleAxis(-90, kinectTransform.up) * kinectTransform.rotation;

                        }
                        else //default rotation for other joints
                        {
                            convertedRotation = Quaternion.AngleAxis(-90, kinectTransform.up) * Quaternion.AngleAxis(-90, kinectTransform.forward) * kinectTransform.rotation;

                        }

                    }
                    else
                    {
                        if ((i > 6 && i < 11) || (i > 17 && i < 22))
                        {
                            convertedRotation = Quaternion.AngleAxis(90, kinectTransform.up) * kinectTransform.rotation;
                        }
                        else if ((i > 13 && i < 18) || (i > 2 && i < 7))
                        {
                            convertedRotation = Quaternion.AngleAxis(-90, kinectTransform.up) * kinectTransform.rotation;
                        }
                        /*else if (i == 22)
                        {
                            convertedRotation = kinectJoints["mixamorig_Head"].transform.rotation;
                        }*/
                        /*else if (i == 23)
                        {
                            //FIX LATER: temporary fix for the neck use the head transform? -- need to fix as there is some mismatch between the kinect and mesh joints
                            //convertedRotation = Quaternion.identity;
                            convertedRotation = kinectTransform.rotation;
                        }*/
                        else if (i == 1) //rotate armature around Y axis only - FOR NOW JUST DON'T ROTATE
                        {
                            convertedRotation = Quaternion.Euler(0, 0, 0);
                        }
                        else
                        {
                            convertedRotation = kinectTransform.rotation;

                        }
                    }

                    joints[i].transform.rotation = convertedRotation;

                }
                else if (i < 23) //if its not the head and we don't hava a roation from the kinect, set it to match its parent's rotation
                {

                    joints[i].transform.rotation = joints[i].transform.parent.transform.rotation;
                }
                /*else //FIX LATER: temporary fix for the neck use the head transform? -- need to fix as there is some mismatch between the kinect and mesh joints
                {

                    joints[i].transform.rotation = joints[i].transform.GetChild(0).transform.rotation;
                }*/

                if (positionJoints || i < 3)
                {

                    if (i < 2)
                    {
                        joints[i].transform.position = new Vector3(kinectTransform.position.x, 0, kinectTransform.position.z);
                    }
                    else
                    {
                        joints[i].transform.position = kinectTransform.position;
                    }
                }


            }

            if (scaleJoints && ((i > 2 && i < 11) || (i > 13 && i < 22)))  //if its not one of the root joints or the head
            {
                if (kinectJointNames[i] != null)
                {
                    if (bodyTracker.JointTracked[id][kinectJointNames[i]])
                    {
                        joints[i].transform.localScale = Vector3.one;
                    }
                    else
                    {
                        joints[i].transform.localScale = flatScale;

                    }

                }

            }
            else if (i > 21) //if its the head or neck
            {
                //check if its close to the headset, hide if its too close to prevent effects from covering face
                if (VrHeadset != null)
                {
                    if (Vector3.SqrMagnitude(joints[23].transform.position - VrHeadset.transform.position) < _headRadSq)
                    {
                        joints[i].transform.localScale = Vector3.forward;
                    }
                    else
                    {
                        joints[i].transform.localScale = Vector3.one;

                    }

                }
            }
        }


    }

    #region remixer body creation methods
    private GameObject CreateThirdShiva(ulong id)
    {
        if (BodyPrefab == null)
        {
            return null;
        }
        GameObject body = Instantiate(BodyPrefab, GetComponent<Transform>());
        body.name = "ThirdShiva" + id;

        shivaThirdJointMap.Add(id, body.GetComponent<JointCollection>().jointArray); //copy array of game objects from prefab

        return body;

    }

    //no longer used - third person bodies just deactivated
    private GameObject CreateThirdPersonBody()
    {
        if (BodyPrefab == null)
        {
            return null;
        }
        GameObject body = Instantiate(BodyPrefab, GetComponent<Transform>());
        body.name = "ThirdPerson";

        _thirdPersonJointMap = body.GetComponent<JointCollection>().jointArray; //copy array of game objects from prefab

        return body;

    }

    private GameObject CreateRemixerBody(ulong id)
    {
        if (BodyPrefab == null)
        {
            return null;
        }
        GameObject body = Instantiate(BodyPrefab, Vector3.zero, Quaternion.identity);
        body.name = "BodyAverager" + id;

        remixerJointMap.Add(id, body.GetComponent<JointCollection>().jointArray); //copy array of game objects from prefab

        return body;
    }

    private GameObject CreateShivaBody(ulong id1, ulong id2)
    {
        if (BodyPrefab == null)
        {
            return null;
        }
        GameObject body = Instantiate(BodyPrefab, Vector3.zero, Quaternion.identity);
        body.name = "ShivaBody" + id1 + "_origin" + id2;

        if (!shivaJointMap.ContainsKey(id1))
        {
            shivaJointMap.Add(id1, new Dictionary<ulong, GameObject[]>());
        }

        if (!shivaJointMap[id1].ContainsKey(id2))
        {
            shivaJointMap[id1].Add(id2, body.GetComponent<JointCollection>().jointArray);
        }

        return body;
    }

    private GameObject CreateMeshBody(ulong id)
    {
        if (BodyPrefab == null)
        {
            return null;
        }

        GameObject body = Instantiate(BodyPrefab);
        body.name = "MeshBody:" + id;

        body.transform.position = Vector3.zero;
        body.transform.rotation = Quaternion.identity;

        meshJointMap.Add(id, body.GetComponent<JointCollection>().jointArray); //copy array of game objects from prefab

        Dictionary<string, GameObject> kinectDictionary = new Dictionary<string, GameObject>();
        for (int i = 0; i < kinectJointNames.Length; i++)   //add mapped kinect entries to dictionary to correlate one to the other
        {

            if (kinectJointNames[i] != null)
            {
                kinectDictionary.Add(meshJointNames[i], bodyTracker.GetBody(id).transform.Find(kinectJointNames[i]).gameObject);

            }
        }

        kinectJointMap.Add(id, kinectDictionary);  //apply dictionary to joint map by ID

        return body;
    }
    #endregion

    #region remixer body update methods

    void UpdateAverageBody()
    {

        Vector3[] positionAverager = new Vector3[_thirdPersonJointMap.Length];

        joints = _thirdPersonJointMap;

        for (int i = 0; i < joints.Length; i++)
        {
            Quaternion rotationAverage = Quaternion.identity;
            Vector4 quaternionCumulator = new Vector4(0, 0, 0, 0);
            int n = 0;

            for (int j = 0; j < knownMeshIds.Count; j++)
            {
                //Change to compare to previous average to help with stability
                //average position and rotation for body
                if (i < 22)
                {
                    if (meshJointMap[knownMeshIds[j]][i].transform.localScale != flatScale)
                    {
                        n++;
                        positionAverager[i] += meshJointMap[knownMeshIds[j]][i].transform.localPosition;
                        rotationAverage = Math3D.AverageQuaternion(ref quaternionCumulator, meshJointMap[knownMeshIds[j]][i].transform.localRotation, meshJointMap[knownMeshIds[j]][0].transform.localRotation, n);
                    }
                }
                else //for head
                {
                    if (meshJointMap[knownMeshIds[j]][i].transform.localScale != Vector3.forward)
                    {
                        n++;
                        positionAverager[i] += meshJointMap[knownMeshIds[j]][i].transform.localPosition;
                        rotationAverage = Math3D.AverageQuaternion(ref quaternionCumulator, meshJointMap[knownMeshIds[j]][i].transform.localRotation, meshJointMap[knownMeshIds[j]][0].transform.localRotation, n);
                    }

                }
            }

            //check if we successfully found any tracked joints - if not set this joint's scale to "flat"
            if (n == 0)
            {
                if (i < 22)
                {
                    joints[i].transform.localScale = flatScale;
                }
                else
                {
                    joints[i].transform.localScale = Vector3.forward; //if its the head zero out y too
                }
            }
            else
            {
                if (i < 3) //set spine base 
                {
                    joints[i].transform.localPosition = new Vector3(joints[i].transform.localPosition.x, positionAverager[i].y / knownMeshIds.Count, joints[i].transform.localPosition.z);
                }
                else if (i > 10 && i < 14) //skip spine for now to avoid issues with averaging quaternions
                {
                    joints[i].transform.localPosition = positionAverager[i] / knownMeshIds.Count;
                }
                else
                {
                    joints[i].transform.localPosition = positionAverager[i] / knownMeshIds.Count;
                    if (!(float.IsNaN(rotationAverage.w) || float.IsNaN(rotationAverage.x) || float.IsNaN(rotationAverage.y) || float.IsNaN(rotationAverage.z)))
                    {
                        joints[i].transform.localRotation = rotationAverage;
                    }
                }

                joints[i].transform.localScale = Vector3.one;
            }
        }

    }

    void UpdateExquisiteCorpse()
    {
        joints = _thirdPersonJointMap;

        for (int i = 0; i < joints.Length; i++)
        {

            if (i < 3)
            {
                joints[i].transform.localPosition = new Vector3(0, joints[i].transform.localPosition.y, 0);
            }
            else
            {

                //REVISIT - change to control which limbs are controlled by which player to simplify code
                //change control mapping based on how many players are present
                switch (knownMeshIds.Count)
                {
                    case 1:
                        {
                            joints[i].transform.localPosition = meshJointMap[knownMeshIds[0]][i].transform.localPosition;
                            joints[i].transform.localRotation = meshJointMap[knownMeshIds[0]][i].transform.localRotation;
                            joints[i].transform.localScale = meshJointMap[knownMeshIds[0]][i].transform.localScale;
                            break;
                        }
                    case 2:
                        {
                            if (i > 12) //player 1 controls upper body
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[0]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[0]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[0]][i].transform.localScale;
                            }
                            else //player 2 controls lower body
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[1]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[1]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[1]][i].transform.localScale;

                            }
                            break;
                        }
                    case 3:
                        {
                            if (i > 17)
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[0]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[0]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[0]][i].transform.localScale;
                            }
                            else if (i > 12)
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[1]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[1]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[1]][i].transform.localScale;

                            }
                            else //player 2 controls lower body
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[2]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[2]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[2]][i].transform.localScale;

                            }

                            break;
                        }
                    case 4:
                        {
                            if (i > 17) //player 1 controls right arm and head
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[0]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[0]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[0]][i].transform.localScale;

                            }
                            else if (i > 10) //player 2 controls torso and left arm
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[1]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[1]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[1]][i].transform.localScale;

                            }
                            else if (i > 6) //player 3 controls right leg
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[2]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[2]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[2]][i].transform.localScale;

                            }
                            else //player 4 controls left leg and lower torso
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[3]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[3]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[3]][i].transform.localScale;

                            }

                            break;
                        }
                    case 5:
                        {
                            if (i > 21) //player 1 controls head
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[0]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[0]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[0]][i].transform.localScale;
                            }
                            else if (i > 17) //player 2 controls right arm
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[1]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[1]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[1]][i].transform.localScale;

                            }
                            else if (i > 13) //player 3 controls left arm
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[2]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[2]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[2]][i].transform.localScale;

                            }
                            else if (i > 10) //player 1 controls torso
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[0]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[0]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[0]][i].transform.localScale;

                            }
                            else if (i > 6) //player 4 controls right leg
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[3]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[3]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[3]][i].transform.localScale;

                            }
                            else if (i > 2) //player 5 controls left leg
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[4]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[4]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[4]][i].transform.localScale;

                            }
                            else //player 1 controls torso
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[0]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[0]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[0]][i].transform.localScale;

                            }
                            break;
                        }
                    case 6:
                        {
                            if (i > 21) //player 1 controls head
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[0]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[0]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[0]][i].transform.localScale;
                            }
                            else if (i > 17) //player 2 controls right arm
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[1]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[1]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[1]][i].transform.localScale;

                            }
                            else if (i > 13) //player 3 controls left arm
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[2]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[2]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[2]][i].transform.localScale;

                            }
                            else if (i > 10) //player 4 controls torso
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[3]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[3]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[3]][i].transform.localScale;

                            }
                            else if (i > 6) //player 5 controls right leg
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[4]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[4]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[4]][i].transform.localScale;

                            }
                            else if (i > 2) //player 6 controls left leg
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[5]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[5]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[5]][i].transform.localScale;

                            }
                            else //player 4 controls torso
                            {
                                joints[i].transform.localPosition = meshJointMap[knownMeshIds[4]][i].transform.localPosition;
                                joints[i].transform.localRotation = meshJointMap[knownMeshIds[4]][i].transform.localRotation;
                                joints[i].transform.localScale = meshJointMap[knownMeshIds[4]][i].transform.localScale;

                            }
                            break;
                        }


                }
            }


        }
    }

    void UpdateRemixBodies(ulong id)
    {

        joints = remixerJointMap[id];

        for (int i = 0; i < joints.Length; i++)
        {
            if (i < 3)
            {
                joints[i].transform.position = meshJointMap[id][i].transform.position; //set equal to root joint by ID
                joints[i].transform.rotation = meshJointMap[id][i].transform.rotation; //set equal to root joint by ID
            }
            else
            {
                joints[i].transform.localScale = _thirdPersonJointMap[i].transform.localScale;
                joints[i].transform.localPosition = _thirdPersonJointMap[i].transform.localPosition;
                joints[i].transform.localRotation = _thirdPersonJointMap[i].transform.localRotation;

            }

        }

    }

    void UpdateRemixBodiesFromSelf(ulong id)
    {
        GameObject[] remixerJoints = remixerJointMap[id];
        GameObject[] meshJoints = meshJointMap[id];

        for (int i = 0; i < joints.Length; i++)
        {
            remixerJoints[i].transform.localScale = meshJoints[i].transform.localScale;
            remixerJoints[i].transform.localPosition = meshJoints[i].transform.localPosition;
            remixerJoints[i].transform.localRotation = meshJoints[i].transform.localRotation;

        }

    }


    void UpdateShivaThird(ulong id)
    {
        joints = shivaThirdJointMap[id];

        for (int i = 0; i < joints.Length; i++)
        {
            if (i < 3)
            {

            }
            else
            {
                joints[i].transform.localPosition = meshJointMap[id][i].transform.localPosition; //set position equal to target joint by ID
                joints[i].transform.localRotation = meshJointMap[id][i].transform.localRotation; //set rotation equal to target joint by ID
                joints[i].transform.localScale = meshJointMap[id][i].transform.localScale; //set scale equal to target joint by ID

            }
        }
    }

    //updates shiva body from mesh bodies - id1 is root body, id2 is target body
    void UpdateShivaBodies(ulong id1, ulong id2)
    {

        //to debug 2D dictionary
        /*if(shivaJointMap.ContainsKey(id1))
        {
            int count = 0;
            foreach (ulong key in shivaJointMap[id1].Keys)
            {
                Debug.Log(id1 + " - Second Level " + count + ": " + key);
                count++;
            }
            if (!shivaJointMap[id1].ContainsKey(id2))
            {
                Debug.Log(id1 + "is MISSING second component:" + id2);
            }
        }
        else
        {
            Debug.Log("MISSING first component" + id1);
        }*/

        joints = shivaJointMap[id1][id2];

        for (int i = 0; i < joints.Length; i++)
        {
            if (i < 3)
            {

                joints[i].transform.position = meshJointMap[id1][i].transform.position; //set equal to root joint by ID
                joints[i].transform.rotation = meshJointMap[id1][i].transform.rotation; //set equal to root joint by ID
            }
            else if (i > 21 || (i > 10 && i < 14))
            {
                joints[i].transform.localPosition = meshJointMap[id1][i].transform.localPosition; //set position equal to root joint by ID
                joints[i].transform.localRotation = meshJointMap[id1][i].transform.localRotation; //set rotation equal to root joint by ID
                joints[i].transform.localScale = meshJointMap[id1][i].transform.localScale; //set scale equal to root joint by ID
            }
            else
            {
                joints[i].transform.localPosition = meshJointMap[id2][i].transform.localPosition; //set position equal to target joint by ID
                joints[i].transform.localRotation = meshJointMap[id2][i].transform.localRotation; //set rotation equal to target joint by ID
                joints[i].transform.localScale = meshJointMap[id2][i].transform.localScale; //set scale equal to target joint by ID

            }
        }

    }

    //swap limbs of id2 body onto id1 body
    void UpdateSwapBodies(ulong id1, ulong id2)
    {

        joints = remixerJointMap[id1];

        for (int i = 0; i < joints.Length; i++)
        {
            if (i < 3)
            {

                joints[i].transform.position = meshJointMap[id1][i].transform.position; //set equal to root joint by ID
                joints[i].transform.rotation = meshJointMap[id1][i].transform.rotation; //set equal to root joint by ID
            }
            else if (i > 21 || (i > 10 && i < 14))
            {
                joints[i].transform.localPosition = meshJointMap[id1][i].transform.localPosition; //set position equal to root joint by ID
                joints[i].transform.localRotation = meshJointMap[id1][i].transform.localRotation; //set rotation equal to root joint by ID
                joints[i].transform.localScale = meshJointMap[id1][i].transform.localScale; //set scale equal to root joint by ID
            }
            else
            {
                joints[i].transform.localPosition = meshJointMap[id2][i].transform.localPosition; //set position equal to target joint by ID
                joints[i].transform.localRotation = meshJointMap[id2][i].transform.localRotation; //set rotation equal to target joint by ID
                joints[i].transform.localScale = meshJointMap[id2][i].transform.localScale; //set scale equal to target joint by ID

            }
        }

    }

    #endregion

    #region remixer body cleanup methods

    private void CleanUpShivaBodies(ulong id)
    {

        if (shivaFirstPersonMode)
        {
            if (shivaBodies.ContainsKey(id))
            {
                foreach (ulong trackId in knownMeshIds)
                {
                    if (shivaBodies.ContainsKey(trackId))
                    {
                        if (shivaBodies[trackId].ContainsKey(id))
                        {
                            Destroy(shivaBodies[trackId][id]); //destroy each instance of the untracked Shiva body from other tracked bodies

                            shivaBodies[trackId].Remove(id);   //remove each instance of untracked Shiva body from sub-dictionaries
                        }
                    }

                    if (shivaBodies[id].ContainsKey(trackId))
                    {
                        Destroy(shivaBodies[id][trackId]); //destroy all Shiva bodies from untracked body
                    }

                }

                shivaBodies.Remove(id); //remove untracked shiva body from dictionary

                if (shivaJointMap.ContainsKey(id))
                {
                    foreach (ulong trackId in knownMeshIds)
                    {
                        if (shivaJointMap.ContainsKey(trackId))
                        {
                            if (shivaJointMap[trackId].ContainsKey(id))
                            {
                                shivaJointMap[trackId].Remove(id);
                            }
                        }

                    }
                    shivaJointMap.Remove(id);
                }

            }
        }


        if (shivaThirdBodies.ContainsKey(id))
        {
            Destroy(shivaThirdBodies[id]);
            shivaThirdBodies.Remove(id);
            shivaThirdJointMap.Remove(id);

        }
    }

    private void CleanUpRemixerBodies(ulong id)
    {
        if (remixerBodies.ContainsKey(id))
        {
            Destroy(remixerBodies[id]);
            remixerBodies.Remove(id);
            remixerJointMap.Remove(id);

        }
    }


    #endregion

    //not used
    private void MeshBodyIK()
    {

        float t = (Time.time - bodyTracker.LastFrameTime()) * 30;

        List<ulong> knownIds = new List<ulong>(meshBodies.Keys);
        foreach (ulong trackingId in knownIds)
        {
            Animator animator = meshBodies[trackingId].GetComponentInChildren<Animator>();

            Transform rightHandGoal = bodyTracker.GetBody(trackingId).transform.Find("HandRight").transform;
            Transform leftHandGoal = bodyTracker.GetBody(trackingId).transform.Find("HandLeft").transform;
            Transform rightFootGoal = bodyTracker.GetBody(trackingId).transform.Find("FootRight").transform;
            Transform leftFootGoal = bodyTracker.GetBody(trackingId).transform.Find("FootLeft").transform;


            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, t);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandGoal.position);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, t);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGoal.position);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, t);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootGoal.position);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, t);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootGoal.position);

            Quaternion rightHandGoalRotation = Quaternion.AngleAxis(-90, rightHandGoal.right) * Quaternion.AngleAxis(-90, rightHandGoal.up) * rightHandGoal.rotation;
            Quaternion leftHandGoalRotation = Quaternion.AngleAxis(-90, leftHandGoal.up) * Quaternion.AngleAxis(-90, leftHandGoal.forward) * leftHandGoal.rotation;
            Quaternion rightFootGoalRotation = Quaternion.AngleAxis(-90, rightFootGoal.right) * Quaternion.AngleAxis(-90, rightFootGoal.up) * rightFootGoal.rotation;
            Quaternion leftFootGoalRotation = Quaternion.AngleAxis(-90, leftFootGoal.up) * Quaternion.AngleAxis(-90, leftFootGoal.forward) * leftFootGoal.rotation;

            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, t);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandGoal.rotation);

            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, t);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGoal.rotation);

        }
    }


}
