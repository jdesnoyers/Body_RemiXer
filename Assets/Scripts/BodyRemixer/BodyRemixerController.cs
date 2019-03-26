/* Created 2019-03-24
 * Works with KinectBodyTracking.cs V2
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum RemixMode {off, average, exquisite, shiva};

public class BodyRemixerController : MonoBehaviour
{

    public RemixMode remixMode = RemixMode.off;
    public bool thirdPerson = false;
    public GameObject BodyPrefab;
    public GameObject thirdPersonParent;
    public GameObject VrHeadset;
    public float HeadRadius = 0.2f;

    [SerializeField] private KinectBodyTracking bodyTracker;

    public bool positionJoints = true;
    public bool scaleJoints = true;


    [HideInInspector] public UnityAction ikAction;

    private Dictionary<ulong, Dictionary<string, GameObject>> _KinectJointMap = new Dictionary<ulong, Dictionary<string, GameObject>>();
    private Dictionary<ulong, GameObject> _MeshBodies = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, GameObject[]> _MeshJointMap = new Dictionary<ulong, GameObject[]>();
    private List<ulong> knownMeshIds = new List<ulong>();


    private Dictionary<ulong, GameObject> _averageBodies = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, GameObject[]> _averageJointMap = new Dictionary<ulong, GameObject[]>();

    private Transform kinectTransform;
    private Quaternion convertedRotation;
    private GameObject[] joints;
    private float _headRadSq;
    private Quaternion zeroQuaternion = new Quaternion(0, 0, 0, 0);

    private string[] _meshJointNames =
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


    // Start is called before the first frame update
    void Start()
    {

        ikAction += MeshBodyIK;

        _headRadSq = HeadRadius * HeadRadius;

        bodyTracker.createDelegateBodies += UpdateRemixer;
    }
    
    // Update is called once per frame
    void LateUpdate()
    {

        
        foreach (ulong trackingId in bodyTracker.trackedIds)
        {
            RefreshMeshBodyObject(trackingId);

            if (_MeshBodies.Count == 1)
            {

            }
            else
            {
                for (int i = 0; i < _MeshBodies.Count; i += 2)
                {
                }

                switch (remixMode)
                    {
                        case RemixMode.off:
                            {
                                break;
                            }
                        case RemixMode.average:
                            {
                                AverageBodies(trackingId);
                                break;
                            }
                        case RemixMode.exquisite:
                            {
                                //ExquisiteCorpse(_MeshBodies[knownMeshIds[i]], _MeshBodies[knownMeshIds[i - 1]]);
                                break;
                            }
                        case RemixMode.shiva:
                            {

                                //ShivaBodies(_MeshBodies[knownMeshIds[i]], _MeshBodies[knownMeshIds[i - 1]]);
                                break;
                            }

                    }

            }
        }

    }

    void AverageBodies(ulong id)
    {

        Vector3[] positionAverager = new Vector3[_averageJointMap[id].Length];
        Vector3[] rotationAverager = new Vector3[_averageJointMap[id].Length];

        joints = _averageJointMap[id];

        for (int i = 0; i <2; i++)
        {
            joints[i].transform.position = _MeshJointMap[id][i].transform.position; //set equal to root joint by ID
            joints[i].transform.rotation = _MeshJointMap[id][i].transform.rotation; //set equal to root joint by ID
        }

        for (int i = 2; i < joints.Length; i++)
        {
            foreach (ulong trackingId in knownMeshIds)
            {
                positionAverager[i] += _MeshJointMap[trackingId][i].transform.localPosition;
                //rotationAverager[i] += _MeshJointMap[trackingId][i].transform.localEulerAngles;
            }
            _averageJointMap[id][i].transform.localPosition = positionAverager[i] / knownMeshIds.Count;
            //_averageJointMap[id][i].transform.localEulerAngles = rotationAverager[i] / knownMeshIds.Count;
        }

    }

    void ExquisiteCorpse(GameObject body1, GameObject body2)
    {

    }

    void ShivaBodies(GameObject body1, GameObject body2)
    {

    }

    void UpdateRemixer()
    {

        knownMeshIds = new List<ulong>(_MeshBodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownMeshIds)
        {
            if (!bodyTracker.trackedIds.Contains(trackingId))
            {
                Destroy(_MeshBodies[trackingId]);
                _MeshBodies.Remove(trackingId);

                if(_averageBodies.ContainsKey(trackingId))
                {
                    Destroy(_averageBodies[trackingId]);
                    _MeshBodies.Remove(trackingId);
                }
            }
        }

        foreach (ulong trackingId in bodyTracker.trackedIds)
        {
            if (!_MeshBodies.ContainsKey(trackingId))
            {
                _MeshBodies[trackingId] = CreateMeshBody(trackingId);
            }

            if (remixMode == RemixMode.average && !_averageBodies.ContainsKey(trackingId) && knownMeshIds.Count>1)
            {
                _averageBodies[trackingId] = CreateMeshAverager(trackingId);
            }
        }

    }

    private GameObject CreateMeshAverager(ulong id)
    {
        if (BodyPrefab == null)
        {
            return null;
        }
        GameObject body = Instantiate(BodyPrefab);
        body.name = "BodyAverager" + id;

        body.transform.position = Vector3.zero;
        body.transform.rotation = Quaternion.identity;

        _averageJointMap.Add(id, body.GetComponent<JointCollection>().jointArray); //copy array of game objects from prefab

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

        _MeshJointMap.Add(id, body.GetComponent<JointCollection>().jointArray); //copy array of game objects from prefab

        Dictionary<string, GameObject> kinectDictionary = new Dictionary<string, GameObject>();
        for (int i = 0; i < kinectJointNames.Length; i++)   //add mapped kinect entries to dictionary to correlate one to the other
        {

            if (kinectJointNames[i] != null)
            {
                kinectDictionary.Add(_meshJointNames[i], bodyTracker.GetBody(id).transform.Find(kinectJointNames[i]).gameObject);

            }
        }

        _KinectJointMap.Add(id, kinectDictionary);  //apply dictionary to joint map by ID

        return body;
    }

    private void RefreshMeshBodyObject(ulong id)
    {

        /* Algorithm
         * 1. position and rotate joints based on kinect joints
         * 2. scale based on height? (based on distance between element and spine base)
         * 3. scale to 0 to remove untracked limbs
         * 
         */

        joints = _MeshJointMap[id];

        Dictionary<string, GameObject> kinectJoints = _KinectJointMap[id];

        //position all joints based on kinect transforms
        for (int i = 0; i < joints.Length; i++)
        {
            string findJoint = _meshJointNames[i];

            if (kinectJoints.ContainsKey(findJoint))
            {
                Transform kinectTransform = kinectJoints[findJoint].transform;

                //joints[i].transform.position = kinectTransform.position;

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

                    joints[i].transform.rotation = convertedRotation;

                }
                else
                {
                    joints[i].transform.rotation = joints[i].transform.parent.transform.rotation;
                }

                if(positionJoints || i<2)
                {
                    joints[i].transform.position = kinectTransform.position;
                }


            }

            if (scaleJoints && ((i > 2 && i < 11) || (i > 13 && i < 22)))  //if its not one of the root joints or the head
            {
                if (kinectJointNames[i] != null)
                {
                    if (bodyTracker.JointTracked[id][kinectJointNames[i]])
                    {
                        joints[i].transform.localScale = new Vector3(1, 1, 1);
                    }
                    else
                    {
                        joints[i].transform.localScale = new Vector3(0, 1, 1);

                    }

                }

            }
            else if (i > 21) //if its the head or neck
            {
                //check if its close to the headset, hide if its too close to prevent effects from covering face
                if (Vector3.SqrMagnitude(joints[23].transform.position - VrHeadset.transform.position) < _headRadSq)
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

        float t = (Time.time - bodyTracker.LastFrameTime()) * 30;

        List<ulong> knownIds = new List<ulong>(_MeshBodies.Keys);
        foreach (ulong trackingId in knownIds)
        {
            Animator animator = _MeshBodies[trackingId].GetComponentInChildren<Animator>();

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
