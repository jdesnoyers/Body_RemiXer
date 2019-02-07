/*Based on BodySourceManager.cs from Microsoft Kinect Unity SDK 1.7
 * 
 *Revised by John Desnoyers-Stewart
 *2018-03-24
 * 
 */


using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class BodySourceManager : MonoBehaviour
{
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
    private Body[] _Data = null;
    private Floor _bodyFrameFloor;

    public UnityEngine.Vector4 floorVector;

    public Floor GetFloor()
    {
        return _bodyFrameFloor;
    }

    public Body[] GetData()
    {
        return _Data;
    }


    private float _lastFrameTime;

    public float LastFrameTime
    {
        get { return _lastFrameTime; }
    }

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.BodyFrameSource.OpenReader();

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
            _Reader.FrameArrived += Reader_FrameArrived;
        }
    }

    

    void Update()
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                if (_Data == null)
                {
                    _Data = new Body[_Sensor.BodyFrameSource.BodyCount];
                }

                frame.GetAndRefreshBodyData(_Data);

                if (frame != null)
                {
                    _bodyFrameFloor = new Floor(frame.FloorClipPlane);
                    floorVector = new UnityEngine.Vector4(frame.FloorClipPlane.X, frame.FloorClipPlane.Y, frame.FloorClipPlane.Z, frame.FloorClipPlane.W);
                }
                
                frame.Dispose();
                frame = null;
            }


        }

    }

    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }

    void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
    {
        //Debug.Log(Time.time-_lastFrameTime);
        _lastFrameTime = Time.time;
        
    }
}




//From Vos Pterneas Floor.cs from https://pterneas.com/2017/09/10/floor-kinect/

public class Floor
{
    public float X { get; internal set; }
    public float Y { get; internal set; }
    public float Z { get; internal set; }
    public float W { get; internal set; }
    public Vector3 position { get; internal set; }

    public Floor(Windows.Kinect.Vector4 floorClipPlane)
    {
        X = floorClipPlane.X;
        Y = floorClipPlane.Y;
        Z = floorClipPlane.Z;
        W = floorClipPlane.W;
        position = new Vector3(X, Y, Z);
    }

    public float Height
    {
        get { return W; }
    }

    public float Tilt
    {
        get { return Mathf.Atan(Z / Y) * (180.0f / Mathf.PI); }
    }

    public float DistanceFrom(CameraSpacePoint point)
    {
        float numerator = X * point.X + Y * point.Y + Z * point.Z + W;
        float denominator = Mathf.Sqrt(X * X + Y * Y + Z * Z);

        return numerator / denominator;
    }
}