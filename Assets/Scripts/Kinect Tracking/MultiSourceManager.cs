/*Based on MultiSourceManager.cs from Microsoft Kinect Unity SDK 1.7
 * 
 *Revised by John Desnoyers-Stewart
 *2018-03-24
 * 
 */

using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class MultiSourceManager : MonoBehaviour {
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }
    
    private KinectSensor _Sensor;
    private MultiSourceFrameReader _Reader;
    private Texture2D _ColorTexture;
    private Color[] _colors;
    private ushort[] _DepthData;
    private byte[] _ColorData;
    private byte[] _BodyIndexData;

    public Texture2D GetColorTexture()
    {
        return _ColorTexture;
    }

    public Color[] GetColorData()
    {

        return _colors;
    }


    public ushort[] GetDepthData()
    {
        return _DepthData;
    }

    public byte[] GetBodyIndexData()
    {
        return _BodyIndexData;
    }

    void Start () 
    {
        _Sensor = KinectSensor.GetDefault();


        if (_Sensor != null) 
        {
            _Reader = _Sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);
            _Reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            var colorFrameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = colorFrameDesc.Width;
            ColorHeight = colorFrameDesc.Height;
            
            _ColorTexture = new Texture2D(colorFrameDesc.Width, colorFrameDesc.Height, TextureFormat.RGBA32, false);
            _ColorData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];
            _colors = new Color[colorFrameDesc.LengthInPixels];

            var depthFrameDesc = _Sensor.DepthFrameSource.FrameDescription;
            _DepthData = new ushort[depthFrameDesc.LengthInPixels];

            var indexFrameDesc = _Sensor.BodyIndexFrameSource.FrameDescription;
            _BodyIndexData = new byte[indexFrameDesc.LengthInPixels];
            
            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }
    
    void Update () 
    {

    }

    void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                var colorFrame = frame.ColorFrameReference.AcquireFrame();
                if (colorFrame != null)
                {
                    var depthFrame = frame.DepthFrameReference.AcquireFrame();
                    if (depthFrame != null)
                    {
                        var bodyIndexFrame = frame.BodyIndexFrameReference.AcquireFrame();
                        if (bodyIndexFrame != null)
                        {
                            //heavy cpu usage... rethink colour input

                            //colorFrame.CopyConvertedFrameDataToArray(_ColorData, ColorImageFormat.Rgba);
                            //_ColorTexture.LoadRawTextureData(_ColorData);
                            _ColorTexture.Apply();

                            depthFrame.CopyFrameDataToArray(_DepthData);

                            bodyIndexFrame.CopyFrameDataToArray(_BodyIndexData);

                            bodyIndexFrame.Dispose();
                            bodyIndexFrame = null;

                        }

                        depthFrame.Dispose();
                        depthFrame = null;


                    }

                    colorFrame.Dispose();
                    colorFrame = null;
                }

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


}
