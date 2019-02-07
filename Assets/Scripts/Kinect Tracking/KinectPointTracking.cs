using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

public class KinectPointTracking : MonoBehaviour
{

    public GameObject MultiSourceManager;
    public Transform vrHeadset;
    public float headRadius =2.0f;
    public bool bodiesOnly = false;
    public bool noBodies = true;
    public bool createParticles;
    public int downsample = 4;
    public float size = 0.2f;
    public float scale = 10f;
    public Color color = Color.white;

    
    FrameDescription depthFrameDesc;
    FrameDescription colorFrameDesc;
    FrameDescription indexFrameDesc;

    CameraSpacePoint[] cameraSpacePoints;
    ColorSpacePoint[] colorSpacePoints;

    Color[] colorData;
    byte[] colorFrameData;
    ushort[] depthFrameData;
    byte[] bodyIndexData;

    private int depthWidth;
    private int depthHeight;

    private int colorWidth;
    private int colorHeight;

    private int indexWidth;
    private int indexHeight;

    private float headRadiusSquare;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;

    private MultiSourceManager _MultiManager;
    private MultiSourceFrameReader multiFrameSourceReader;

    private float particleSystemSelfDestruct= 0.1f;
    private float particleSystemTimer = 0.0f;
    
    private int bytesPerPixel = 4;

    private ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] particleArray;
    private ParticleSystem.Particle particleZ;
    private List<ParticleSystem.Particle> particles = new List<ParticleSystem.Particle>();

    void Start()
    {
        
        
        _Sensor = KinectSensor.GetDefault();
        if(_Sensor !=null)
        {

            multiFrameSourceReader = _Sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex);

            multiFrameSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            _Mapper = _Sensor.CoordinateMapper;

            depthFrameDesc = _Sensor.DepthFrameSource.FrameDescription;
            colorFrameDesc = _Sensor.ColorFrameSource.FrameDescription;
            indexFrameDesc = _Sensor.BodyIndexFrameSource.FrameDescription;

            depthWidth = depthFrameDesc.Width;
            depthHeight = depthFrameDesc.Height;
            colorWidth = colorFrameDesc.Width;
            colorHeight = colorFrameDesc.Height;
            indexWidth = indexFrameDesc.Width;
            indexHeight = indexFrameDesc.Height;

        
            colorFrameData = new byte[colorWidth * colorHeight * bytesPerPixel];

            cameraSpacePoints = new CameraSpacePoint[depthWidth * depthHeight];
            colorSpacePoints = new ColorSpacePoint[depthWidth * depthHeight];

            bodyIndexData = new byte[indexWidth * indexHeight];
            
            _particleSystem = gameObject.GetComponent<ParticleSystem>();

            _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();

            particleArray = new ParticleSystem.Particle[(depthWidth / downsample) * (depthHeight / downsample)];


            for(int i = 0; i < particleArray.Length; i++)
            {
                particleArray[i] = new ParticleSystem.Particle();
                particleArray[i].position = Vector3.zero;

            }
            //Debug.Log(particleArray[42].position);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }

            headRadiusSquare = headRadius*headRadius;

            particleZ = new ParticleSystem.Particle();
            
        }
    }


    void Update()
    {
        if (Time.time - particleSystemTimer > particleSystemSelfDestruct) _particleSystem.Clear();

    }  
    
    /* Based on Kinect Coordinate Mapping Basics
     *  Modified by John Densoyers-Stewart
     *  2018-03-28
     */
    
    void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
    {
        particles.Clear();

        if (MultiSourceManager == null) return;

        if (_MultiManager == null) return;

        int bodyCount = _Sensor.BodyFrameSource.BodyCount;

        depthFrameData = _MultiManager.GetDepthData();
        bodyIndexData = _MultiManager.GetBodyIndexData();
        

        _Mapper.MapDepthFrameToCameraSpace(depthFrameData, cameraSpacePoints);
        _Mapper.MapDepthFrameToColorSpace(depthFrameData, colorSpacePoints);
        

        if (createParticles)
        {
            for (int x = 0; x < depthWidth; x += downsample)
            {
                for (int y = 0; y < depthHeight; y += downsample)
                {
                    int i = x + (depthWidth * y);
                    CameraSpacePoint p = cameraSpacePoints[i];

                    if (!float.IsNegativeInfinity(p.X) && !float.IsNegativeInfinity(p.Y) && !float.IsNegativeInfinity(p.Z))
                    {
                        if (bodiesOnly)
                        {
                            if (bodyIndexData[i] < bodyCount)
                            {
                                //need to combine this with the other color stuff below to make it work?
                                /*
                                ColorSpacePoint colorPoint = colorSpacePoints[i];

                                byte r = 0;
                                byte g = 0;
                                byte b = 0;
                                byte a = 0;

                                int colorX = (int)System.Math.Floor(colorPoint.X + 0.5);
                                int colorY = (int)System.Math.Floor(colorPoint.Y + 0.5);

                                if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                                {
                                    int colorIndex = ((colorY * colorWidth) + colorX) * bytesPerPixel;
                                    b = colorFrameData[colorIndex++];
                                    g = colorFrameData[colorIndex++];
                                    r = colorFrameData[colorIndex++];
                                    a = colorFrameData[colorIndex++];
                                }*/

                                Vector3 particlePos = GetVector3FromCameraSpacePoint(p);

                                if (Vector3.SqrMagnitude(particlePos - transform.InverseTransformPoint(vrHeadset.position)) > headRadiusSquare)
                                {
                                    //particleArray[i].position = particlePos;
                                    //particleArray[i].startColor = color;
                                    //particleArray[i].startSize = size;
                                    //particleArray[i].startLifetime = 1.0f;
                                    ParticleSystem.Particle particle = new ParticleSystem.Particle();
                                    particle.position = particlePos;
                                    particle.startColor = color; // new Color32(r,g,b,a);
                                    particle.startSize = size;
                                    particles.Add(particle);
                                }
                            }

                        }
                        else if (noBodies)
                        {
                            if (bodyIndexData[i] > bodyCount)
                            {

                                Vector3 particlePos = GetVector3FromCameraSpacePoint(p);

                                if (Vector3.SqrMagnitude(particlePos - transform.InverseTransformPoint(vrHeadset.position)) > headRadiusSquare)
                                {
                                    //particleArray[i].position = particlePos;
                                    //particleArray[i].startColor = color;
                                    //particleArray[i].startSize = size;

                                    ParticleSystem.Particle particle = new ParticleSystem.Particle();
                                    particle.position = particlePos;
                                    particle.startColor = color;
                                    particle.startSize = size;
                                    particles.Add(particle);
                                }
                            }
                        }
                        else
                        {
                            Vector3 particlePos = GetVector3FromCameraSpacePoint(p);

                            if (Vector3.SqrMagnitude(particlePos - transform.InverseTransformPoint(vrHeadset.position)) > headRadiusSquare)
                            {
                                //particleArray[i].position = particlePos;
                                //Debug.Log(particleArray[i].position);
                                //particleArray[i].startColor = color;
                                //particleArray[i].startSize = size;

                                ParticleSystem.Particle particle = new ParticleSystem.Particle();
                                particle.position = particlePos;
                                particle.startColor = color;
                                particle.startSize = size;
                                particles.Add(particle);

                            }
                        }
                    }
                }
                /*
               float colorMappedToDepthX = colorSpacePoints[colorIndex].X;
               float colorMappedToDepthY = colorSpacePoints[colorIndex].Y;

               CameraSpacePoint p = cameraSpacePoints[colorIndex];

               if (!float.IsNegativeInfinity(colorMappedToDepthX) && !float.IsNegativeInfinity(colorMappedToDepthY))
               {
                   int depthX = (int)(colorMappedToDepthX + 0.5f);
                   int depthY = (int)(colorMappedToDepthY + 0.5f);

                   // If the point is not valid, there is no body index there.
                   if ((depthX >= 0) && (depthX < depthWidth) && (depthY >= 0) && (depthY < depthHeight))
                   {
                       int depthIndex = (depthY * depthWidth) + depthX;

                       //if (bodyIndexData[depthIndex] < bodyCount)
                       //{
                       if (!float.IsNegativeInfinity(p.X) && !float.IsNegativeInfinity(p.Y) && !float.IsNegativeInfinity(p.Z))
                       {
                           ParticleSystem.Particle particle = new ParticleSystem.Particle();
                           particle.position = new Vector3(-p.X, p.Y, p.Z) * scale;
                           particle.startColor = color;
                           particle.startSize = size;
                           particles.Add(particle);
                           //}
                       }
                   }
               }*/

            }

                
            
            //Debug.Log(_particleSystem.particleCount);


            //particleSystemTimer = Time.time;
            //_particleSystem.SetParticles(particleArray, particleArray.Length);

            if (particles.Count > 0)
            {
                //_particleSystem.SetParticles(particleArray, particleArray.Length);
                _particleSystem.SetParticles(particles.ToArray(), particles.Count);
                particleSystemTimer = Time.time;
            }
        }

        /*if (createMesh)
        {
            KinectMesh.GetComponent<Renderer>().material.mainTexture = _MultiManager.GetColorTexture();

            RefreshData(_MultiManager.GetDepthData(), _MultiManager.ColorWidth, _MultiManager.ColorHeight);
        }*/
        

    }

    /*  CreateMesh and Refresh Data based on DepthSourceView.cs from Microsoft Kinect Unity SDK 1.7
     *  Modified by John Densoyers-Stewart
     *  2018-03-28
     *
     
    private void CreateMesh(int width, int height)
    {
        _Mesh = new Mesh();
        KinectMesh.GetComponent<MeshFilter>().mesh = _Mesh;

        _Vertices = new Vector3[width * height];
        _UV = new Vector2[width * height];
        _Triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                _Vertices[index] = new Vector3(x, -y, 0);
                _UV[index] = new Vector2(((float)x / (float)width), ((float)y / (float)height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }

        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
    }


    private void RefreshData(ushort[] depthData, int colorWidth, int colorHeight)
    {
        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];
        _Mapper.MapDepthFrameToColorSpace(depthData, colorSpace);

        for (int y = 0; y < frameDesc.Height; y += downsample)
        {
            for (int x = 0; x < frameDesc.Width; x += downsample)
            {
                int indexX = x / downsample;
                int indexY = y / downsample;
                int i = (indexY * (frameDesc.Width / downsample)) + indexX;

                CameraSpacePoint p = cameraSpacePoints[i];

                if (!float.IsNegativeInfinity(p.X) && !float.IsNegativeInfinity(p.Y) && !float.IsNegativeInfinity(p.Z))
                {
                    _Vertices[i] = new Vector3(-p.X, p.Y, p.Z);

                }
                else
                {
                    _Vertices[i] = Vector3.zero;
                }

                
                // Update UV mapping with CDRP
                var colorSpacePoint = colorSpace[(y * frameDesc.Width) + x];
                _UV[i] = new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight);
            }
        }

        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
    }*/

    private static Vector3 GetVector3FromCameraSpacePoint(CameraSpacePoint point)
    {
        return new Vector3(-point.X, point.Y, point.Z);

    }

    private void OnEnable()
    {
        if(multiFrameSourceReader != null)
            multiFrameSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
    }

    private void OnDisable()
    {

        multiFrameSourceReader.MultiSourceFrameArrived -= Reader_MultiSourceFrameArrived;
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
