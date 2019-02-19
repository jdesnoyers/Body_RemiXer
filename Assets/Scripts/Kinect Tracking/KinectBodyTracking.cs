/*Based on BodySourceView.cs from Microsoft Kinect Unity SDK 1.7
 * 
 *Revised by John Desnoyers-Stewart
 *2018-03-26
 */

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class KinectBodyTracking : MonoBehaviour
{
    public Material BoneMaterial;
    public GameObject BodySourceManager;
    public GameObject kinectSpaceObject;
    public GameObject BodyPrefab;
    public GameObject VrHeadset;
    public float HeadRadius =0.2f;
    public float floorOffset = 0.07f;
    
    [Tooltip("[0..1], lower values closer to raw data")]
    public float filterSmoothing = 0.25f;

    [Tooltip("[0..1], lower values slower to correct towards the raw data")]
    public float filterCorrection = 0.25f;

    [Tooltip("[0..n], the number of frames to predict into the future")]
    public float filterPrediction = 0.25f;

    [Tooltip("The radius in meters for jitter reduction")]
    public float filterJitterRadius = 0.25f;

    [Tooltip("The maximum radius in meters that filtered positions are allowed to deviate from raw data")]
    public float filterMaxDeviationRadius = 0.25f;

    [HideInInspector] public UnityAction ikAction;

    [SerializeField] private bool debugJoints = false;

    private float _headRadSq;

    private float lerpTime = 0f;

    private BodySourceManager _BodyManager;
    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, GameObject> _MeshBodies = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, GameObject[]> _NeoJointMap = new Dictionary<ulong, GameObject[]>();
    private Dictionary<ulong, Dictionary<string, GameObject>> _KinectJointMap = new Dictionary<ulong, Dictionary<string, GameObject>>();
    private Dictionary<ulong, KinectJointFilter> _bodyFilters = new Dictionary<ulong, KinectJointFilter>();
    private Dictionary<ulong, Dictionary<string, bool>> _jointTracked = new Dictionary<ulong, Dictionary<string, bool>>();


    private Vector3 _avgHeadPosition;
    private List<Vector3> _headPositions = new List<Vector3>();
    private Quaternion zeroQuaternion = new Quaternion(0, 0, 0, 0);

    private List<ulong> trackedIds = new List<ulong>();
    private List<ulong> knownMeshIds = new List<ulong>();
    private List<ulong> knownIds = new List<ulong>();

    private Kinect.CameraSpacePoint[] filteredJoints;
    private Dictionary<Kinect.JointType, Quaternion> orientJoints = new Dictionary<Kinect.JointType, Quaternion>();
    private Vector3 filteredJointPos;
    private Kinect.Joint sourceJoint;
    private Kinect.Joint? targetJoint = null;
    private Vector3? filteredTargetJointPos = null;
    private Transform jointObj;
    private Dictionary<ulong, Dictionary<Kinect.JointType,Transform>> jointTransforms = new Dictionary<ulong, Dictionary<Kinect.JointType, Transform>>();

    private Transform neoTransform;
    private Transform kinectTransform;
    private Quaternion convertedRotation;
    private GameObject[] joints;

    private Floor floor;

    private string[] neoJointNames =
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

    };

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
                null,
                    "SpineMid",
                        null,
                            null,
                                "ShoulderLeft",
                                    "ElbowLeft",
                                        "WristLeft",
                            null,
                                "ShoulderRight",
                                    "ElbowRight",
                                        "WristRight",
                            "Neck",
                                "Head",

    };

    

    

    //dictionary to access connected joints
    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },

        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },

        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },


        { Kinect.JointType.Head, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineBase },
    };
    

    private Dictionary<Kinect.JointType, Kinect.JointType> _JointChildMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        
        { Kinect.JointType.KneeLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.KneeLeft },
        
        { Kinect.JointType.KneeRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.HipRight, Kinect.JointType.KneeRight },
        
        { Kinect.JointType.WristLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.ElbowLeft },
        
        { Kinect.JointType.WristRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.WristRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.ElbowRight },


        //{ Kinect.JointType.Neck, Kinect.JointType.Head },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck},
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
    };

    //Get average head position of all tracked bodies
    public Vector3 GetAvgHeadPosition()
    {
        _avgHeadPosition = Vector3.zero;

            foreach (var item in _Bodies)
            {
                _avgHeadPosition += item.Value.transform.Find("Head").transform.position;
            }
            return _avgHeadPosition /= _Bodies.Count;
        

    }


    //Gett array of head positions
    public Vector3[] GetHeadPositions()
    {
        _headPositions.Clear();
        foreach (var item in _Bodies)
        {
            _headPositions.Add(item.Value.transform.Find("Head").transform.position);
        }
        return _headPositions.ToArray();

    }

    //function to obtain number of tracked bodies
    public int GetNumBodies()
    {
        return _Bodies.Count;
    }

    void Start()
    {
        ikAction += MeshBodyIK;
        _headRadSq = HeadRadius * HeadRadius;
    }

    void Update()
    {
        if (BodySourceManager == null)
        {
            return;
        }

        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }

        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }

        trackedIds.Clear();
        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                trackedIds.Add(body.TrackingId);
            }
        }

        

        knownIds = new List<ulong>(_Bodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownIds)
        {
            if (!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
                _bodyFilters.Remove(trackingId);
                _jointTracked.Remove(trackingId);
            }
        }

        knownMeshIds = new List<ulong>(_MeshBodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownMeshIds)
        {
            if (!trackedIds.Contains(trackingId))
            {
                Destroy(_MeshBodies[trackingId]);
                _MeshBodies.Remove(trackingId);
            }
        }

        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                if (!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                    _bodyFilters[body.TrackingId] = new KinectJointFilter(); //initialize filter
                    _bodyFilters[body.TrackingId].Init(filterSmoothing,filterCorrection,filterPrediction,filterJitterRadius,filterMaxDeviationRadius);  //set filter values here 
                }

                if (!_MeshBodies.ContainsKey(body.TrackingId))
                {
                    _MeshBodies[body.TrackingId] = CreateMeshBody(body.TrackingId);
                }

                RefreshBodyObject(body, _bodyFilters[body.TrackingId], _Bodies[body.TrackingId],body.TrackingId);
            }
        }

        floor = _BodyManager.GetFloor();
        if (floor != null)
        {
            kinectSpaceObject.transform.localPosition = new Vector3(kinectSpaceObject.transform.localPosition.x, floor.W + floorOffset, kinectSpaceObject.transform.localPosition.z);

            if (!float.IsNaN(floor.Tilt))
            {
                kinectSpaceObject.transform.localEulerAngles = new Vector3(-floor.Tilt, kinectSpaceObject.transform.localEulerAngles.y, kinectSpaceObject.transform.localEulerAngles.z);
            }
        }

    }
    private void LateUpdate()
    {

        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
        foreach (ulong trackingId in knownIds)
        {
            RefreshMeshBodyObject(_Bodies[trackingId], _MeshBodies[trackingId],trackingId);

        }

    }

    //set up the body game object in Unity
    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);
        body.transform.parent = kinectSpaceObject.transform;
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.identity;

        Dictionary<Kinect.JointType,Transform> jointTrans = new Dictionary<Kinect.JointType, Transform>();

        Dictionary<string, bool> newJoints = new Dictionary<string, bool>();

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            newJoints.Add(jt.ToString(), false);

            GameObject jointObject = new GameObject();

            jointObject.name = jt.ToString();
            jointObject.transform.parent = body.transform;
            jointObject.transform.localRotation = Quaternion.identity;

            if (debugJoints)
            {
                GameObject jointDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                jointDisplay.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                jointDisplay.transform.parent = jointObject.transform;
                jointDisplay.transform.localPosition = Vector3.zero;
                jointDisplay.transform.localRotation = Quaternion.identity;

                LineRenderer lr = jointObject.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = 2;
                lr.material = BoneMaterial;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
            }

            jointTrans.Add(jt, jointObject.transform);

        }

        jointTransforms.Add(id, jointTrans);
        _jointTracked.Add(id, newJoints);


        return body;
    }

    //update body from filtered data
    private void RefreshBodyObject(Kinect.Body body, KinectJointFilter bodyFilter, GameObject bodyObject,ulong id)
    {
        bodyFilter.UpdateFilter(body);

        filteredJoints = bodyFilter.GetFilteredJoints();

        orientJoints.Clear();

        orientJoints = CalculateJointRotations(body.JointOrientations);


        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            
            filteredJointPos = GetVector3FromCameraSpacePoint(filteredJoints[(int)jt]);

            sourceJoint = body.Joints[jt];

            targetJoint = null;
            filteredTargetJointPos = null;

            if (_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
                filteredTargetJointPos = GetVector3FromCameraSpacePoint(filteredJoints[(int)_BoneMap[jt]]);
            }

            //jointObj = bodyObject.transform.Find(jt.ToString());
            jointObj = jointTransforms[id][jt];


            //calculate orientations of end joints that are not captured by the kinect
            if (zeroQuaternion.Equals(orientJoints[jt]) && filteredTargetJointPos.HasValue)
            {
                Vector3 direction = filteredJointPos - filteredTargetJointPos.Value;

                if (jt == Kinect.JointType.AnkleLeft || jt == Kinect.JointType.AnkleRight) //the ankle roations have to be pointed at the foot to match the mesh
                {
                    if(jt == Kinect.JointType.AnkleLeft)
                    {
                        direction = GetVector3FromCameraSpacePoint(filteredJoints[(int)Kinect.JointType.FootLeft]) - filteredJointPos;

                    }
                    else
                    {
                        direction = GetVector3FromCameraSpacePoint(filteredJoints[(int)Kinect.JointType.FootRight]) - filteredJointPos;

                    }
                    Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
                    Vector3 normal = Vector3.Cross(direction, perpendicular);

                    if (normal.sqrMagnitude != 0 && direction.sqrMagnitude != 0)
                    {
                        orientJoints[jt] = Quaternion.LookRotation(normal, direction); //normal, direction

                    }
                    else
                    {
                        orientJoints[jt] = Quaternion.identity;
                    }
                }
                //else 
                if (jt == Kinect.JointType.ThumbLeft || jt == Kinect.JointType.ThumbRight) //the thumbs are along their parents forward vector so they are calculated
                {

                    Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
                    Vector3 normal = Vector3.Cross(perpendicular, direction);

                    if (normal.sqrMagnitude != 0 && direction.sqrMagnitude != 0)
                    {
                        orientJoints[jt] = Quaternion.LookRotation(normal, direction);

                    }
                    else
                    {
                        orientJoints[jt] = Quaternion.identity;
                    }

                }
                else //by default set the up axis to point away from the joint and forward axis towards the parent's forward axis
                {

                    Vector3 forward = orientJoints[_BoneMap[jt]] * Vector3.forward;
                    // calculate a rotation, Y forward for Kinect
                    if (direction.sqrMagnitude != 0)
                    {
                        orientJoints[jt] = Quaternion.LookRotation(forward, direction);

                    }
                    else
                    {
                        orientJoints[jt] = Quaternion.identity;
                    }

                }
            }


            //check tracking state of joint to make sure it is tracked, turn off rendered objects if not.
            if (debugJoints)
            {

                switch (sourceJoint.TrackingState)
                {
                    case Kinect.TrackingState.NotTracked:
                        if (jointObj.GetChild(0).gameObject.activeSelf) jointObj.GetChild(0).gameObject.SetActive(false);
                        _jointTracked[body.TrackingId][jt.ToString()] = false;

                        break;
                    case Kinect.TrackingState.Inferred:
                        if (jointObj.GetChild(0).gameObject.activeSelf) jointObj.GetChild(0).gameObject.SetActive(false);
                        jointObj.localPosition = filteredJointPos;
                        jointObj.localRotation = orientJoints[jt];
                        _jointTracked[body.TrackingId][jt.ToString()] = false;

                        break;
                    case Kinect.TrackingState.Tracked:
                        if (!jointObj.GetChild(0).gameObject.activeSelf) jointObj.GetChild(0).gameObject.SetActive(true);
                        jointObj.localPosition = filteredJointPos;
                        jointObj.localRotation = orientJoints[jt];
                        _jointTracked[body.TrackingId][jt.ToString()] = true;

                        break;
                    default:
                        break;
                }


                LineRenderer lr = jointObj.GetComponent<LineRenderer>();
                if (targetJoint.HasValue)
                {
                    lr.useWorldSpace = false;
                    lr.SetPosition(0, Vector3.zero);
                    lr.SetPosition(1, Quaternion.Inverse(jointObj.localRotation) * (filteredTargetJointPos.Value - filteredJointPos));
                    lr.startColor = GetColorForState(sourceJoint.TrackingState);
                    lr.endColor = GetColorForState(targetJoint.Value.TrackingState);
                }
                else
                {
                    lr.enabled = false;
                }
            }
            else
            {
                switch (sourceJoint.TrackingState)
                {
                    case Kinect.TrackingState.NotTracked:

                        _jointTracked[body.TrackingId][jt.ToString()] = false;
                        break;

                    case Kinect.TrackingState.Inferred:

                        _jointTracked[body.TrackingId][jt.ToString()] = false;
                        break;

                    case Kinect.TrackingState.Tracked:

                        _jointTracked[body.TrackingId][jt.ToString()] = true;

                        break;

                    default:

                        break;
                }

                jointObj.localPosition = filteredJointPos;
                jointObj.localRotation = orientJoints[jt];

                

            }
            /*if(jointObj.localRotation == zeroQuaternion)
            {
                Vector3 perpendicular = Vector3.Cross(jointObj.localPosition, Vector3.up);
                Vector3 normal = Vector3.Cross(perpendicular, jointObj.localPosition);

                // calculate a rotation
                jointObj.rotation.SetLookRotation(normal, jointObj.localPosition);
            }*/
        }
    }

    private GameObject CreateMeshBody(ulong id)
    {
        if (BodyPrefab == null)
        {
            return CreateBodyObject(id);
        }
        
        GameObject body = Instantiate(BodyPrefab);
        body.name = "MeshBody:" + id;

        body.transform.position = Vector3.zero;
        body.transform.rotation = Quaternion.identity;

        _NeoJointMap.Add(id, body.GetComponent<JointCollection>().jointArray); //copy array of game objects from prefab

        Dictionary<string, GameObject> kinectDictionary = new Dictionary<string, GameObject>();
        for (int i = 0; i < kinectJointNames.Length; i++)   //add mapped kinect entries to dictionary to correlate one to the other
        {

            if(kinectJointNames[i]!=null)
            {
                kinectDictionary.Add(neoJointNames[i], _Bodies[id].transform.Find(kinectJointNames[i]).gameObject);

            }
        }

        _KinectJointMap.Add(id, kinectDictionary);  //apply dictionary to joint map by ID
        

        return body;
    }

    private void RefreshMeshBodyObject(GameObject kinectObject, GameObject bodyObject, ulong id)
    {

        /* Algorithm
         * 1. position and rotate joints based on kinect joints
         * 2. scale based on height? (based on distance between element and spine base)
         * 3. scale to 0 to remove untracked limbs
         * 
         */

        joints = _NeoJointMap[id];

        Dictionary<string, GameObject> kinectJoints = _KinectJointMap[id];


        //float t = (Time.time - _BodyManager.LastFrameTime) * 30;

        /*float t = (Time.time - lerpTime) * 30;

        if(t >= 1)
        {
            lerpTime = Time.time;
        }*/

        //Debug.Log(t);


        //position all joints based on kinect transforms
        for (int i = 0; i< joints.Length; i++)
        {
            string findJoint = neoJointNames[i];

            if (kinectJoints.ContainsKey(findJoint))
            {
                //neoTransform = joints[i].transform;
                kinectTransform = kinectJoints[findJoint].transform;

                //joints[i].transform.position = Vector3.Lerp(neoTransform.position, kinectTransform.position, t);
                joints[i].transform.position = kinectTransform.position;

                if (!kinectTransform.rotation.Equals(zeroQuaternion))    //don't rotate joints if we don't have a value from the kinect
                {
                    if (i < 2) //if its one of the main body components then the rotation is different from the rest of the joints
                    {
                        convertedRotation = Quaternion.AngleAxis(180, kinectTransform.up) * kinectTransform.rotation;

                    }
                    else if (i == 3 || i == 4 || i == 5 || i == 6) //rotations for left leg (except ankle)
                    {
                        convertedRotation = Quaternion.AngleAxis(-90, kinectTransform.forward) * kinectTransform.rotation;
                    }
                    /*else if (i == 5) //rotation for left ankle
                    {
                        convertedRotation = kinectTransform.rotation;
                    }*/
                    else if (i == 7 || i == 8 || i == 9 || i == 10) //rotations for right leg (except ankle)
                    {
                        convertedRotation = Quaternion.AngleAxis(180, kinectTransform.up) * Quaternion.AngleAxis(90, kinectTransform.forward) * kinectTransform.rotation;
                    }
                    /*else if (i == 9)
                    {
                        convertedRotation = Quaternion.AngleAxis(180, kinectTransform.right) * kinectTransform.rotation;
                    }*/
                    else if (i > 17 && i < 22) //rotations for 18-21 are inverted in the model
                    {
                        convertedRotation = Quaternion.AngleAxis(-90, kinectTransform.right) * Quaternion.AngleAxis(-90, kinectTransform.up) * kinectTransform.rotation;

                    }
                    else //default rotation for other joints
                    {
                        convertedRotation = Quaternion.AngleAxis(-90, kinectTransform.up) * Quaternion.AngleAxis(-90, kinectTransform.forward) * kinectTransform.rotation;

                    }

                    //joints[i].transform.rotation = Quaternion.Slerp(neoTransform.rotation, convertedRotation, t);
                    joints[i].transform.rotation = convertedRotation;

                }
                else
                {
                    joints[i].transform.rotation = joints[i].transform.parent.transform.rotation;
                }
                 
                joints[i].transform.position = kinectTransform.position;   


            }
            
            if((i>2 && i < 11) || (i > 13 && i < 22))  //if its not one of the root joints or the head
            {
                if (kinectJointNames[i] != null)
                {
                    if (_jointTracked[id][kinectJointNames[i]])
                    {
                        joints[i].transform.localScale = new Vector3(1, 1, 1);
                    }
                    else
                    {
                        joints[i].transform.localScale = new Vector3(0, 1, 1);

                    }

                }

            }
            else if(i > 21) //if its the head or neck
            {
                //check if its close to the headset, hide if its too close to prevent effects from covering face
                if(Vector3.SqrMagnitude(joints[23].transform.position - VrHeadset.transform.position)<_headRadSq)
                {
                    joints[i].transform.localScale = new Vector3(0, 0, 1);
                }
                else
                {
                    joints[i].transform.localScale = new Vector3(1, 1, 1);

                }
            }
        }


        
        
        

    }


    private void MeshBodyIK()
    {

        float t = (Time.time - _BodyManager.LastFrameTime) * 30;

        List<ulong> knownIds = new List<ulong>(_MeshBodies.Keys);
        foreach (ulong trackingId in knownIds)
        {
            Animator animator = _MeshBodies[trackingId].GetComponentInChildren<Animator>();

            Transform rightHandGoal = _Bodies[trackingId].transform.Find("HandRight").transform;
            Transform leftHandGoal = _Bodies[trackingId].transform.Find("HandLeft").transform;
            Transform rightFootGoal = _Bodies[trackingId].transform.Find("FootRight").transform;
            Transform leftFootGoal = _Bodies[trackingId].transform.Find("FootLeft").transform;


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
            animator.SetIKRotation(AvatarIKGoal.RightHand,rightHandGoal.rotation);

            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, t);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGoal.rotation);

        }
    }



    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
            case Kinect.TrackingState.Tracked:
                return Color.green;

            case Kinect.TrackingState.Inferred:
                return Color.red;

            default:
                return Color.black;
        }
    }

    private Dictionary<Kinect.JointType, Quaternion> CalculateJointRotations(Dictionary<Kinect.JointType, Kinect.JointOrientation> orientations)
    {
        Dictionary<Kinect.JointType, Quaternion> rotations = new Dictionary<Kinect.JointType, Quaternion>();

        
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Quaternion rotation = Quaternion.identity;

            if (_JointChildMap.ContainsKey(jt))
            {
                rotation = GetQuaternionFromJointOrientation(orientations[_JointChildMap[jt]]);
            }
            else
            {
                rotation = zeroQuaternion;
            }

            rotations.Add(jt, rotation);




        }

        return rotations;
    }

    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(-joint.Position.X, joint.Position.Y, joint.Position.Z);

    }

    private static Vector3 GetVector3FromCameraSpacePoint(Kinect.CameraSpacePoint point)
    {
        return new Vector3(-point.X, point.Y, point.Z);

    }


    private static Quaternion GetQuaternionFromJointOrientation(Kinect.JointOrientation joint,bool flip =true)
    {
        if(flip)
        {
            return new Quaternion(joint.Orientation.X, -joint.Orientation.Y, -joint.Orientation.Z, joint.Orientation.W);
        }
        else
        {
            return new Quaternion(joint.Orientation.X, joint.Orientation.Y, joint.Orientation.Z, joint.Orientation.W);
        }
        
    }

}
