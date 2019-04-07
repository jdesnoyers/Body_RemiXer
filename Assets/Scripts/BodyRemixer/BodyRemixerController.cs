/* Created 2019-03-24
 * Works with KinectBodyTracking.cs V2
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum RemixMode { off, average, exquisite, shiva };

[System.Serializable]
public class BoolEvent : UnityEvent<bool>
{
}

public class BodyRemixerController : MonoBehaviour
{

    public RemixMode remixMode = RemixMode.off;
    public bool thirdPerson = false;
    public bool firstPerson = true;
    public GameObject thirdPersonBody;
    public GameObject BodyPrefab;
    public GameObject thirdPersonParent;
    public GameObject VrHeadset;
    public float HeadRadius = 0.2f;

    [SerializeField] private KinectBodyTracking bodyTracker;

    public bool positionJoints = true;
    public bool scaleJoints = true;

    public BoolEvent thirdPersonToggle;

    private bool thirdPersonOld;

    [HideInInspector] public UnityAction ikAction;

    private Dictionary<ulong, Dictionary<string, GameObject>> _KinectJointMap = new Dictionary<ulong, Dictionary<string, GameObject>>();
    private Dictionary<ulong, GameObject> _MeshBodies = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, GameObject[]> _MeshJointMap = new Dictionary<ulong, GameObject[]>();
    private List<ulong> knownMeshIds = new List<ulong>();



    private GameObject[] _thirdPersonJointMap;

    private Dictionary<ulong, GameObject> _averageBodies = new Dictionary<ulong, GameObject>(); //dictionary of average bodies to keep track of bodies used for exquisite corpse and averager
    private Dictionary<ulong, GameObject[]> _averageJointMap = new Dictionary<ulong, GameObject[]>();   //dictionary of average joints to access each joint

    private Dictionary<ulong, Dictionary<ulong, GameObject>> _shivaBodies = new Dictionary<ulong, Dictionary<ulong, GameObject>>(); //2D dictionary to access each shiva body
    private Dictionary<ulong, Dictionary<ulong, GameObject[]>> _shivaJointMap = new Dictionary<ulong, Dictionary<ulong, GameObject[]>>(); //2D dictionary to access each shiva body

    private Transform kinectTransform;
    private Quaternion convertedRotation;
    private GameObject[] joints;
    private float _headRadSq;
    private Quaternion zeroQuaternion = new Quaternion(0, 0, 0, 0);
    private Vector3 flatScale = new Vector3(0, 1, 1);

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

        ikAction += MeshBodyIK; //Set up inverse Kinematics - not currently working

        _headRadSq = HeadRadius * HeadRadius; //square radius for later use

        bodyTracker.DelegateBodies += UpdateRemixer; //add remixer to delegate function in body tracker

        //setup third person body
        if (thirdPersonBody == null)
        {
            thirdPersonBody = CreateThirdPersonBody();
        }
        else
        {
            thirdPersonBody = CreateThirdPersonBody(thirdPersonBody);
        }

        if (thirdPersonToggle == null)
            thirdPersonToggle = new BoolEvent();

        thirdPersonToggle.AddListener(thirdPersonBody.transform.GetChild(0).transform.GetChild(0).gameObject.SetActive);

    }

    // Update is called once per frame
    void LateUpdate()
    {
        

        if (thirdPerson != thirdPersonOld)
        {
            thirdPersonToggle.Invoke(thirdPerson);
            thirdPersonOld = thirdPerson;
        }

        switch (remixMode)
        {
            case RemixMode.off:
                {
                    break;
                }
            case RemixMode.average:
                {
                    AverageBody();
                    break;
                }
            case RemixMode.exquisite:
                {
                    ExquisiteCorpse();
                    break;
                }
            case RemixMode.shiva:
                {
                    break;
                }

        }

        foreach (ulong trackingId in bodyTracker.trackedIds)
        {
            RefreshMeshBodyObject(trackingId);

            if (_MeshBodies.Count == 1)
            {

            }
            else
            {

                switch (remixMode)
                {
                    case RemixMode.off:
                        {
                            break;
                        }
                    case RemixMode.average:
                        {
                            UpdateBodies(trackingId);
                            break;
                        }
                    case RemixMode.exquisite:
                        {
                            UpdateBodies(trackingId);
                            break;
                        }
                    case RemixMode.shiva:
                        {

                            foreach (ulong shivaId in bodyTracker.trackedIds)
                            {
                                ShivaBodies(trackingId, shivaId);

                            }
                            break;
                        }

                }

            }
        }

    }

    void AverageBody()
    {

        Vector3[] positionAverager = new Vector3[_thirdPersonJointMap.Length];

        joints = _thirdPersonJointMap;

        for (int i = 0; i < joints.Length; i++)
        {
            Quaternion rotationAverage = Quaternion.identity;
            Vector4 quaternionCumulator = new Vector4();
            int n = 0;

            for (int j = 0; j < knownMeshIds.Count; j++)
            {
                if (_MeshJointMap[knownMeshIds[j]][i].transform.localScale != flatScale)
                {
                    n++;
                    positionAverager[i] += _MeshJointMap[knownMeshIds[j]][i].transform.localPosition;
                    rotationAverage = Math3D.AverageQuaternion(ref quaternionCumulator, _MeshJointMap[knownMeshIds[j]][i].transform.localRotation, _MeshJointMap[knownMeshIds[j]][0].transform.localRotation, n);
                }
            }
            if (n == 0)
            {
                joints[i].transform.localScale = flatScale;
            }
            else
            {
                if(i == 0)
                {
                    joints[i].transform.localPosition = new Vector3(joints[i].transform.localPosition.x,positionAverager[i].y / knownMeshIds.Count, joints[i].transform.localPosition.z);
                }
                else
                {
                    joints[i].transform.localPosition = positionAverager[i] / knownMeshIds.Count;
                    joints[i].transform.localRotation = rotationAverage;
                }
                joints[i].transform.localScale = Vector3.one;
            }
        }

    }
    
    void ExquisiteCorpse()
    {
        joints = _thirdPersonJointMap;

        for(int i = 1; i < joints.Length; i++)
        {
            //REVISIT - change to control which limbs are controlled by which player to simplify code
            //change control mapping based on how many players are present
            switch (knownMeshIds.Count)
            {
                case 1:
                    {
                        joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[0]][i].transform.localPosition;
                        joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[0]][i].transform.localRotation;
                        joints[i].transform.localScale = _MeshJointMap[knownMeshIds[0]][i].transform.localScale;
                        break;
                    }
                case 2:
                    {
                        if (i > 12) //player 1 controls upper body
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[0]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[0]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[0]][i].transform.localScale;
                        }
                        else //player 2 controls lower body
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[1]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[1]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[1]][i].transform.localScale;

                        }
                        break;
                    }
                case 3:
                    {
                        if (i > 17)
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[0]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[0]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[0]][i].transform.localScale;
                        }
                        else if (i > 12)
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[1]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[1]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[1]][i].transform.localScale;

                        }
                        else //player 2 controls lower body
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[2]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[2]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[2]][i].transform.localScale;

                        }

                        break;
                    }
                case 4:
                    {
                        if (i > 17) //player 1 controls right arm and head
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[0]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[0]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[0]][i].transform.localScale;

                        }
                        else if (i > 10) //player 2 controls torso and left arm
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[1]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[1]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[1]][i].transform.localScale;

                        }
                        else if (i > 6) //player 3 controls right leg
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[2]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[2]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[2]][i].transform.localScale;

                        }
                        else //player 4 controls left leg and lower torso
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[3]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[3]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[3]][i].transform.localScale;

                        }

                        break;
                    }
                case 5:
                    {
                        if (i > 21) //player 1 controls head
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[0]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[0]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[0]][i].transform.localScale;
                        }
                        else if (i > 17) //player 2 controls right arm
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[1]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[1]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[1]][i].transform.localScale;

                        }
                        else if (i > 13) //player 3 controls left arm
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[2]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[2]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[2]][i].transform.localScale;

                        }
                        else if (i > 10) //player 1 controls torso
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[0]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[0]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[0]][i].transform.localScale;

                        }
                        else if (i > 6) //player 4 controls right leg
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[3]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[3]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[3]][i].transform.localScale;

                        }
                        else if (i > 2) //player 5 controls left leg
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[4]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[4]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[4]][i].transform.localScale;

                        }
                        else //player 1 controls torso
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[0]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[0]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[0]][i].transform.localScale;

                        }
                        break;
                    }
                case 6:
                    {
                        if (i > 21) //player 1 controls head
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[0]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[0]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[0]][i].transform.localScale;
                        }
                        else if (i > 17) //player 2 controls right arm
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[1]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[1]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[1]][i].transform.localScale;

                        }
                        else if (i > 13) //player 3 controls left arm
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[2]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[2]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[2]][i].transform.localScale;

                        }
                        else if (i > 10) //player 4 controls torso
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[3]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[3]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[3]][i].transform.localScale;

                        }
                        else if (i > 6) //player 5 controls right leg
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[4]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[4]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[4]][i].transform.localScale;

                        }
                        else if (i > 2) //player 6 controls left leg
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[5]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[5]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[5]][i].transform.localScale;

                        }
                        else //player 4 controls torso
                        {
                            joints[i].transform.localPosition = _MeshJointMap[knownMeshIds[4]][i].transform.localPosition;
                            joints[i].transform.localRotation = _MeshJointMap[knownMeshIds[4]][i].transform.localRotation;
                            joints[i].transform.localScale = _MeshJointMap[knownMeshIds[4]][i].transform.localScale;

                        }
                        break;
                    }

            }

        }
    }

    /*void UpdateBodiesOld(ulong id)
    {

        Vector3[] positionAverager = new Vector3[_averageJointMap[id].Length];

        joints = _averageJointMap[id];

        for (int i = 0; i < joints.Length; i++)
        {
            if (i < 3)
            {
                joints[i].transform.position = _MeshJointMap[id][i].transform.position; //set equal to root joint by ID
                joints[i].transform.rotation = _MeshJointMap[id][i].transform.rotation; //set equal to root joint by ID
            }
            else
            {
                Quaternion rotationAverage = Quaternion.identity;
                Vector4 quaternionCumulator = new Vector4();
                int n = 0;

                for (int j = 0; j < knownMeshIds.Count; j++)
                {
                    if (_MeshJointMap[knownMeshIds[j]][i].transform.localScale != flatScale)
                    {
                        n++;
                        positionAverager[i] += _MeshJointMap[knownMeshIds[j]][i].transform.localPosition;
                        rotationAverage = Math3D.AverageQuaternion(ref quaternionCumulator, _MeshJointMap[knownMeshIds[j]][i].transform.localRotation, _MeshJointMap[knownMeshIds[j]][0].transform.localRotation, n);
                    }
                }
                if (n == 0)
                {
                    joints[i].transform.localScale = flatScale;
                }
                else
                {
                    joints[i].transform.localScale = Vector3.one;
                    joints[i].transform.localPosition = positionAverager[i] / knownMeshIds.Count;
                    joints[i].transform.localRotation = rotationAverage;
                }
            }


        }

    }*/

    void UpdateBodies(ulong id)
    {
        
        joints = _averageJointMap[id];

        for (int i = 0; i < joints.Length; i++)
        {
            if (i < 3)
            {
                joints[i].transform.position = _MeshJointMap[id][i].transform.position; //set equal to root joint by ID
                joints[i].transform.rotation = _MeshJointMap[id][i].transform.rotation; //set equal to root joint by ID
            }
            else
            {
                joints[i].transform.localScale = _thirdPersonJointMap[i].transform.localScale;
                joints[i].transform.localPosition = _thirdPersonJointMap[i].transform.localPosition;
                joints[i].transform.localRotation = _thirdPersonJointMap[i].transform.localRotation;
                
            }

        }

    }


    //updates shiva body from mesh bodies - id1 is root body, id2 is target body
    void ShivaBodies(ulong id1, ulong id2)
    {

        //to debug 2D dictionary
        /*if(_shivaJointMap.ContainsKey(id1))
        {
            int count = 0;
            foreach (ulong key in _shivaJointMap[id1].Keys)
            {
                Debug.Log(id1 + " - Second Level " + count + ": " + key);
                count++;
            }
            if (!_shivaJointMap[id1].ContainsKey(id2))
            {
                Debug.Log(id1 + "is MISSING second component:" + id2);
            }
        }
        else
        {
            Debug.Log("MISSING first component" + id1);
        }*/

        joints = _shivaJointMap[id1][id2];

        for (int i = 0; i < joints.Length; i++)
        {
            if (i < 3)
            {

                joints[i].transform.position = _MeshJointMap[id1][i].transform.position; //set equal to root joint by ID
                joints[i].transform.rotation = _MeshJointMap[id1][i].transform.rotation; //set equal to root joint by ID
            }
            else if (i > 21 || (i > 10 && i < 14))
            {
                joints[i].transform.localPosition = _MeshJointMap[id1][i].transform.localPosition; //set position equal to root joint by ID
                joints[i].transform.localRotation = _MeshJointMap[id1][i].transform.localRotation; //set rotation equal to root joint by ID
                joints[i].transform.localScale = _MeshJointMap[id1][i].transform.localScale; //set scale equal to root joint by ID
            }
            else
            {
                joints[i].transform.localPosition = _MeshJointMap[id2][i].transform.localPosition; //set position equal to target joint by ID
                joints[i].transform.localRotation = _MeshJointMap[id2][i].transform.localRotation; //set rotation equal to target joint by ID
                joints[i].transform.localScale = _MeshJointMap[id2][i].transform.localScale; //set scale equal to target joint by ID

            }
        }
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
                _MeshJointMap.Remove(trackingId);

                if (_averageBodies.ContainsKey(trackingId))
                {
                    Destroy(_averageBodies[trackingId]);
                    _averageBodies.Remove(trackingId);
                    _averageJointMap.Remove(trackingId);
                }
                if (_shivaBodies.ContainsKey(trackingId))
                {
                    foreach (ulong trackId in knownMeshIds)
                    {
                        if (_shivaBodies.ContainsKey(trackId))
                        {
                            if (_shivaBodies[trackId].ContainsKey(trackingId))
                            {
                                Destroy(_shivaBodies[trackId][trackingId]); //destroy each instance of the untracked Shiva body from other tracked bodies

                                _shivaBodies[trackId].Remove(trackingId);   //remove each instance of untracked Shiva body from sub-dictionaries
                            }
                        }

                        if (_shivaBodies[trackingId].ContainsKey(trackId))
                        {
                            Destroy(_shivaBodies[trackingId][trackId]); //destroy all Shiva bodies from untracked body
                        }

                        if (_shivaJointMap[trackId].ContainsKey(trackingId))
                        {
                            _shivaJointMap[trackId].Remove(trackingId);
                        }
                    }

                    _shivaBodies.Remove(trackingId); //remove untracked shiva body from dictionary

                    if (_shivaJointMap.ContainsKey(trackingId))
                    {
                        _shivaJointMap.Remove(trackingId);
                    }

                }
            }
        }

        //then add newly tracked bodies
        foreach (ulong trackingId in bodyTracker.trackedIds)
        {
            if (!_MeshBodies.ContainsKey(trackingId))
            {
                _MeshBodies[trackingId] = CreateMeshBody(trackingId);
            }

            if ((remixMode == RemixMode.average || remixMode == RemixMode.exquisite) && !_averageBodies.ContainsKey(trackingId) && knownMeshIds.Count > 1)
            {
                _averageBodies[trackingId] = CreateMeshAverager(trackingId);
            }
            if ((remixMode == RemixMode.shiva) && !_shivaBodies.ContainsKey(trackingId) && knownMeshIds.Count > 1)
            {
                //add dictionary for this new tracked body
                if (!_shivaBodies.ContainsKey(trackingId))
                {
                    _shivaBodies.Add(trackingId, new Dictionary<ulong, GameObject>());
                }

                //add dictionary entry for each other new body and sub-dictionary entries for all
                foreach (ulong trackId in bodyTracker.trackedIds)
                {

                    if (!_shivaBodies.ContainsKey(trackId))
                    {
                        _shivaBodies.Add(trackId, new Dictionary<ulong, GameObject>());
                    }
                    if (!_shivaBodies[trackingId].ContainsKey(trackId))
                    {
                        _shivaBodies[trackingId].Add(trackId, CreateShivaBody(trackingId, trackId));
                    }
                    if (!_shivaBodies[trackId].ContainsKey(trackingId))
                    {
                        _shivaBodies[trackId].Add(trackingId, CreateShivaBody(trackId, trackingId));
                    }
                    if (!_shivaBodies[trackId].ContainsKey(trackId))
                    {
                        _shivaBodies[trackId].Add(trackId, CreateShivaBody(trackId, trackId));
                    }

                }
            }
        }

    }

    private GameObject CreateThirdPersonBody()
    {
        if (BodyPrefab == null)
        {
            return null;
        }
        GameObject body = Instantiate(BodyPrefab, Vector3.zero, Quaternion.identity, GetComponent<Transform>());
        body.name = "ThirdPerson";

        body = CreateThirdPersonBody(body);

        return body;

    }

    private GameObject CreateThirdPersonBody(GameObject body)
    {
        _thirdPersonJointMap = body.GetComponent<JointCollection>().jointArray; //copy array of game objects from prefab

        return body;
    }


    private GameObject CreateMeshAverager(ulong id)
    {
        if (BodyPrefab == null)
        {
            return null;
        }
        GameObject body = Instantiate(BodyPrefab, Vector3.zero, Quaternion.identity);
        body.name = "BodyAverager" + id;

        _averageJointMap.Add(id, body.GetComponent<JointCollection>().jointArray); //copy array of game objects from prefab

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

        if (!_shivaJointMap.ContainsKey(id1))
        {
            _shivaJointMap.Add(id1, new Dictionary<ulong, GameObject[]>());
        }

        if (!_shivaJointMap[id1].ContainsKey(id2))
        {
            _shivaJointMap[id1].Add(id2, body.GetComponent<JointCollection>().jointArray);
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

                if (positionJoints || i < 3)
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
