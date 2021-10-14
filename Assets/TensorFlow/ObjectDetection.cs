using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ObjectDetection : MonoBehaviour
{
    [SerializeField]
    TextAsset model;
    
    [SerializeField]
    TextAsset labels;

    private Detector detector;
    // Start is called before the first frame update
    void Start()
    {
        detector = new Detector(model, labels);
    }

    
    // Update is called once per frame
    void Update()
    {
        OnCameraFrameReceived();
        var outputs = detector.Detect(m_Texture, angle: 90, threshold: 0.6f);
        for (int i = 0; i < outputs.Count; i++)
        {
            var output = outputs[i] as Dictionary<string, object>;
            Debug.Log(output["detectedClass"]);
            if (output["detectedClass"].Equals("apple"))
            {
                
                DrawApple(output["rect"] as Dictionary<string, float>);
                break;
            }
        }
    }

    [SerializeField]private GameObject apple;
    private void DrawApple(Dictionary<string, float> rect)
    {
        var xMin = rect["x"];
        var yMin = 1 - rect["y"];
        var xMax = rect["x"] + rect["w"];
        var yMax = 1 - rect["y"] - rect["h"];

        var pos = GetPosition((xMin + xMax) / 2 * Screen.width, (yMin + yMax) / 2 * Screen.height);
        
        apple.SetActive(true);
        apple.transform.position = pos;
    }
    public ARRaycastManager  arOrigin;
    private Vector3 GetPosition(float x, float y)
    {
        var hits = new List<ARRaycastHit>();

        arOrigin.Raycast(new Vector3(x, y, 0), hits, TrackableType.Planes);

        if (hits.Count > 0)
        {
            var pose = hits[0].pose;
            return pose.position;
        }

        return new Vector3();
    }
    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events.")]
    ARCameraManager m_CameraManager;
    public ARCameraManager cameraManager
    {
        get => m_CameraManager;
        set => m_CameraManager = value;
    }
    [SerializeField]
    Text m_ImageInfo;
    unsafe  void OnCameraFrameReceived()
    {
        // Attempt to get the latest camera image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        Debug.Log("why");


        // Display some information about the camera image
        m_ImageInfo.text = string.Format(
            "Image info:\n\twidth: {0}\n\theight: {1}\n\tplaneCount: {2}\n\ttimestamp: {3}\n\tformat: {4}",
            image.width, image.height, image.planeCount, image.timestamp, image.format);

        // Choose an RGBA format.
        // See CameraImage.FormatSupported for a complete list of supported formats.
        var format = TextureFormat.RGBA32;

        if (m_Texture == null || m_Texture.width != image.width || m_Texture.height != image.height)
            m_Texture = new Texture2D(image.width, image.height, format, false);

        // Convert the image to format, flipping the image across the Y axis.
        // We can also get a sub rectangle, but we'll get the full image here.
        XRCpuImage.Transformation m_Transformation = XRCpuImage.Transformation.MirrorY;
        var conversionParams = new XRCpuImage.ConversionParams (image, format, m_Transformation);

        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureData = m_Texture.GetRawTextureData<byte>();
        try
        {

            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);

        }
        finally
        {
            // We must dispose of the CameraImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        m_Texture.Apply();
    }
    public Texture2D m_Texture;
}
