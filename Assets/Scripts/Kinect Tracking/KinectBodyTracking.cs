/* Based on BodySourceView.cs from Microsoft Kinect Unity SDK 1.7
 * 
 * V1
 * Revised by John Desnoyers-Stewart
 * 2018-03-26
 *
 * V2
 * Updated by John Desnoyers-Stewart
 * 2019-03-24
 * - Mesh Body tracking removed and placed in "Body Remixer Controller" to better manage various applications to different meshes
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

    [SerializeField] private bool debugJoints = false;
    [SerializeField] private bool useUntrackedJoints = false;
    [SerializeField] private bool useInferredJoints = false;

    [SerializeField] private bool rotateToNextJoint = true;
    public GameObject jointOrienterObject;

    public bool positionFloorBool = true;

    private float lerpTime = 0f;

    private BodySourceManager _BodyManager;
    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, KinectJointFilter> _bodyFilters = new Dictionary<ulong, KinectJointFilter>();
    public Dictionary<ulong, Dictionary<string, bool>> JointTracked { get; private set; } = new Dictionary<ulong, Dictionary<string, bool>>();


    private Vector3 _avgHeadPosition;
    private List<Vector3> _headPositions = new List<Vector3>();
    private Quaternion zeroQuaternion = new Quaternion(0, 0, 0, 0);

    public List<ulong> trackedIds { get; private set; } = new List<ulong>();
    private List<ulong> knownIds = new List<ulong>();

    private Kinect.CameraSpacePoint[] filteredJoints;
    private Dictionary<Kinect.JointType, Quaternion> orientJoints = new Dictionary<Kinect.JointType, Quaternion>();
    private Vector3 filteredJointPos;
    private Kinect.Joint sourceJoint;
    private Kinect.Joint? targetJoint = null;
    private Vector3? filteredTargetJointPos = null;
    private Transform jointObj;
    private Dictionary<ulong, Dictionary<Kinect.JointType,Transform>> jointTransforms = new Dictionary<ulong, Dictionary<Kinect.JointType, Transform>>();
    
    private Floor floor;

    public delegate void DelegateFunction();
    public DelegateFunction DelegateBodies;
    

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

    public GameObject GetBody(ulong id)
    {
        return _Bodies[id];
    }

    public float LastFrameTime()
    {
        return _BodyManager.LastFrameTime;
    }

    void Start()
    {
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
                JointTracked.Remove(trackingId);
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


                RefreshBodyObject(body, _bodyFilters[body.TrackingId], _Bodies[body.TrackingId],body.TrackingId);
            }
        }

        floor = _BodyManager.GetFloor();
        if (floor != null)
        {
            if(positionFloorBool)
            {
                kinectSpaceObject.transform.localPosition = new Vector3(kinectSpaceObject.transform.localPosition.x, floor.W + floorOffset, kinectSpaceObject.transform.localPosition.z);
            }

            if (!float.IsNaN(floor.Tilt))
            {
                kinectSpaceObject.transform.localEulerAngles = new Vector3(-floor.Tilt, kinectSpaceObject.transform.localEulerAngles.y, kinectSpaceObject.transform.localEulerAngles.z);
            }
        }

        DelegateBodies();
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
                //GameObject jointDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject jointDisplay = GameObject.Instantiate(jointOrienterObject);
                jointDisplay.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
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
        JointTracked.Add(id, newJoints);


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
            else  //if joints are not computed in above, then point their Y value at the next joint while keeping their current Z value
            {
                if (_JointChildMap.ContainsKey(jt) && rotateToNextJoint)
                {
                    Vector3 childFilteredJointPos = GetVector3FromCameraSpacePoint(filteredJoints[(int)_JointChildMap[jt]]);
                    Vector3 y = childFilteredJointPos - filteredJointPos;
                    orientJoints[jt] = Quaternion.LookRotation(orientJoints[jt] * Vector3.forward, y);
                }
            }

            //check tracking state of joint to make sure it is tracked, turn off rendered objects if not.
            if (debugJoints)
            {

                switch (sourceJoint.TrackingState)
                {
                    case Kinect.TrackingState.NotTracked:
                        if (jointObj.GetChild(0).gameObject.activeSelf) jointObj.GetChild(0).gameObject.SetActive(useUntrackedJoints);
                        JointTracked[body.TrackingId][jt.ToString()] = useUntrackedJoints;

                        break;
                    case Kinect.TrackingState.Inferred:
                        if (jointObj.GetChild(0).gameObject.activeSelf) jointObj.GetChild(0).gameObject.SetActive(useInferredJoints);
                        jointObj.localPosition = filteredJointPos;
                        jointObj.localRotation = orientJoints[jt];
                        JointTracked[body.TrackingId][jt.ToString()] = useInferredJoints;

                        break;
                    case Kinect.TrackingState.Tracked:
                        if (!jointObj.GetChild(0).gameObject.activeSelf) jointObj.GetChild(0).gameObject.SetActive(true);
                        jointObj.localPosition = filteredJointPos;
                        jointObj.localRotation = orientJoints[jt];
                        JointTracked[body.TrackingId][jt.ToString()] = true;

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

                        JointTracked[body.TrackingId][jt.ToString()] = useUntrackedJoints;
                        break;

                    case Kinect.TrackingState.Inferred:

                        JointTracked[body.TrackingId][jt.ToString()] = useInferredJoints;
                        break;

                    case Kinect.TrackingState.Tracked:

                        JointTracked[body.TrackingId][jt.ToString()] = true;

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
