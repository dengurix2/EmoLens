﻿//https://torikasyu.com/?p=921

using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using HoloToolkit.Unity.InputModule;
using System.Collections.Generic;

public class CognitiveCamera : MonoBehaviour, IInputClickHandler
{
    public string response = "";

    // for inspector(uGUI Objects)
    public RawImage ImageViewer;
    public RawImage ImageCaptured;
    public Text uiText;

    [SerializeField]
    string VISIONKEY = "YOURVISIONKEY"; // replace with your Computer Vision API Key
    [SerializeField]
    string filePath = "";

    int camWidth_px = 1280;
    int camHeight_px = 720;
    WebCamTexture webcamTexture;

    string emotionURL = "https://westus.api.cognitive.microsoft.com/emotion/v1.0/recognize";

    void Start()
    {
        // Any place AirTap
        InputManager.Instance.PushFallbackInputHandler(gameObject);

		WebCamDevice[] devices = WebCamTexture.devices;
#if UNITY_EDITOR
        string devicename = devices[1].name;
#else
        //Hololensの実機がここを通ると想定
        string devicename = devices[0].name;
#endif

        if (devices.Length > 0)
        {
            var euler = transform.localRotation.eulerAngles;
#if UNITY_EDITOR
            webcamTexture = new WebCamTexture(devicename, camWidth_px, camHeight_px);
#else
            webcamTexture = new WebCamTexture(camWidth_px, camHeight_px);
#endif
            //iPhone,Androidの場合はカメラの向きを縦にする
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
            {
                transform.localRotation = Quaternion.Euler(euler.x, euler.y, euler.z - 90);
            }

            ImageViewer.texture = webcamTexture;
            webcamTexture.Play();
        }
        else
        {
            Debug.Log("Webカメラが検出できませんでした");
            return;
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(1))
        {
            _OnClicked();
        }
#endif
    }

    public void SavePhoto()
    {
        string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
        ///filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        filePath = Application.persistentDataPath + "/" + filename;

        Texture2D snap = new Texture2D(camWidth_px, camHeight_px);
        snap.SetPixels(webcamTexture.GetPixels());
        snap.Apply();

        //var bytes = snap.EncodeToPNG();
        var bytes = snap.EncodeToJPG();
        File.WriteAllBytes(filePath, bytes);
        Debug.Log(filePath);

        ImageCaptured.texture = snap;
        webcamTexture.Stop();

        StartCoroutine(GetVisionDataFromImages());
    }

    //Sceneを変更する場合にカメラを止める
    public void EndCam()
    {
        webcamTexture.Stop();
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("AirTapped");
        _OnClicked();
    }

    private void _OnClicked()
    {
        if (webcamTexture.isPlaying)
        {
            SavePhoto();
        }
        else
        {
            webcamTexture.Play();
        }
    }

    IEnumerator GetVisionDataFromImages()
    {
        byte[] bytes = UnityEngine.Windows.File.ReadAllBytes(filePath);
        Debug.Log(filePath + ": " + bytes.Length.ToString());

        var headers = new Dictionary<string, string>() {
            { "Ocp-Apim-Subscription-Key", VISIONKEY },
            { "Content-Type", "application/octet-stream" }
        };

        WWW www = new WWW(emotionURL, bytes, headers);

        yield return www;
        string responseData = www.text; // Save the response as JSON string

        Debug.Log(responseData);
        //GetComponent<ParseComputerVisionResponse>().ParseJSONData(responseData);
        response = responseData;

        uiText.text = response;
    }

}