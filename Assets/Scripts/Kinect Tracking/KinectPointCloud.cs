//Based on Unity And Kinect V2 Point Cloud by yjiro0403
//Revised by John Desnoyers-Stewart
//2018-03-20

using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class KinectPointCloud : MonoBehaviour
{
    public GameObject MultiSourceManager;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;

    private MultiSourceManager _MultiManager;

    FrameDescription depthFrameDesc;
    FrameDescription colorFrameDesc;
    CameraSpacePoint[] cameraSpacePoints;
    ColorSpacePoint[] colorSpacePoints;
    byte[] colorFrameData;
    ushort[] depthFrameData;

    private int depthWidth;
    private int depthHeight;

    private int colorWidth;
    private int colorHeight;

    private int bytesPerPixel = 4;

    ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] particles;

    public Color color = Color.white;
    public float size = 0.2f;
    public float scale = 10f;

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;

            depthFrameDesc = _Sensor.DepthFrameSource.FrameDescription;
            colorFrameDesc = _Sensor.ColorFrameSource.FrameDescription;

            depthWidth = depthFrameDesc.Width;
            depthHeight = depthFrameDesc.Height;

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }

            colorWidth = colorFrameDesc.Width;
            colorHeight = colorFrameDesc.Height;
            // allocate space to put the pixels being received
            colorFrameData = new byte[colorWidth * colorHeight * bytesPerPixel];

            particles = new ParticleSystem.Particle[depthWidth * depthHeight];

            cameraSpacePoints = new CameraSpacePoint[depthWidth * depthHeight];
        }
    }


    void Update()
    {


        if (_Sensor == null) return;
        if (MultiSourceManager == null) return;

        _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();

        if (_MultiManager == null) return;

        depthFrameData = _MultiManager.GetDepthData();

        _Mapper.MapDepthFrameToCameraSpace(depthFrameData, cameraSpacePoints);
        //_Mapper.MapDepthFrameToColorSpace(depthFrameData, colorSpacePoints);


        int particleCount = 0;

        for (int y = 0; y < depthHeight; y += 2)
        {
            for (int x = 0; x < depthWidth; x += 2)
            {
                int depthIndex = (y * depthWidth) + x;
                CameraSpacePoint p = cameraSpacePoints[depthIndex];
                /*ColorSpacePoint colorPoint = colorSpacePoints[depthIndex];

                byte r = 0;
                byte g = 0;
                byte b = 0;
                byte a = 0;

                int colorX = (int)System.Math.Floor(colorPoint.X + 0.5);
                int colorY = (int)System.Math.Floor(colorPoint.Y + 0.5);

                if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                {
                    int colorIndex = ((colorY * colorWidth) + colorX) * bytesPerPixel;
                    int displayIndex = depthIndex * bytesPerPixel;
                    b = colorFrameData[colorIndex++];
                    g = colorFrameData[colorIndex++];
                    r = colorFrameData[colorIndex++];
                    a = colorFrameData[colorIndex++];
                }*/

                if (!(double.IsInfinity(p.X)) && !(double.IsInfinity(p.Y)) && !(double.IsInfinity(p.Z)))
                {
                    //if (p.X < 3.0 && p.Y < 3.0 && p.Z < 3.0)
                    //{
                    particles[particleCount].position = new Vector3(p.X * scale, p.Y * scale, p.Z * scale);
                    //particles[particleCount].startColor = new Color(r / 255F, g / 255F, b / 255F, a / 255F);
                    particles[particleCount].startColor = color;
                    particles[particleCount].startSize = size;
                    particleCount++;
                    //}
                }
            }

            _particleSystem = gameObject.GetComponent<ParticleSystem>();
            _particleSystem.SetParticles(particles, particles.Length);
        }

        StartCoroutine("Delay");

    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(1.0f);
    }


    void OnApplicationQuit()
    {
        if (_Mapper != null)
        {
            _Mapper = null;
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