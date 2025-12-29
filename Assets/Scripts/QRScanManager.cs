using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// 負責處理 WebCamTexture 與 QR Code 解析
/// [已修正: 修復編譯錯誤，改回 BarcodeReaderGeneric 並手動轉換格式，支援持續掃描]
/// </summary>
public class QRScanManager : MonoBehaviour
{
    [Header("UI 元件")]
    [Tooltip("用來顯示相機畫面的 RawImage")]
    public RawImage cameraDisplay;

    [Tooltip("維持相機比例")]
    public AspectRatioFitter aspectRatioFitter;

    [Header("掃描參數")]
    [Tooltip("每次嘗試解碼間隔（秒）")]
    public float scanInterval = 0.5f;

    private WebCamTexture camTexture;
    private BarcodeReaderGeneric reader; // 修正: 明確使用 BarcodeReaderGeneric
    private bool isScanning = false;
    private Action<string> onScanResult;
    private float lastScanTime;
    
    // 緩存 Buffer 減少 GC
    private byte[] buffer;
    private Color32[] pixelBuffer;

    void Awake()
    {
        // 初始化 ZXing Reader (Generic 版本)
        reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };
    }

    public void StartScan(Action<string> callback)
    {
        if (isScanning) return;
        onScanResult = callback;
        StartCoroutine(StartCameraRoutine());
    }

    public void StopScan()
    {
        isScanning = false;
        
        if (camTexture != null)
        {
            if (camTexture.isPlaying) camTexture.Stop();
            Destroy(camTexture);
            camTexture = null;
        }

        if (cameraDisplay != null)
        {
            cameraDisplay.texture = null;
            cameraDisplay.color = Color.black; 
        }
        
        // 清空 Buffer
        buffer = null;
        pixelBuffer = null;
        
        Debug.Log("[QRScanManager] 資源已釋放");
    }

    private IEnumerator StartCameraRoutine()
    {
        // 1. 權限檢查
        yield return RequestCameraPermission();
        if (!HasCameraPermission())
        {
            Debug.LogError("[QRScanManager] 無相機權限");
            yield break;
        }

        // 2. 初始化相機
        string cameraName = GetBackCameraName();
        camTexture = new WebCamTexture(cameraName, 640, 480, 30);
        
        if (cameraDisplay != null)
        {
             cameraDisplay.texture = camTexture;
             cameraDisplay.color = Color.white; 
        }
        camTexture.Play();

        // 3. 等待啟動
        float timeout = 0f;
        while (camTexture.width <= 16 && timeout < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            timeout += 0.1f;
        }

        if (camTexture.width <= 16)
        {
            Debug.LogError("[QRScanManager] 相機啟動失敗");
            StopScan();
            yield break;
        }

        isScanning = true;
        Debug.Log($"[QRScanManager] 啟動成功: {camTexture.width}x{camTexture.height}");

        // 4. 掃描迴圈
        while (isScanning && camTexture != null && camTexture.isPlaying)
        {
            if (Time.time - lastScanTime < scanInterval) 
            {
                yield return null;
                continue;
            }

            if (camTexture.width > 100)
            {
                UpdateAspectRatio();
                TryDecode();
                lastScanTime = Time.time;
            }
            yield return null;
        }
    }

    private void TryDecode()
    {
        try
        {
            int w = camTexture.width;
            int h = camTexture.height;
            
            // 重新配置 Buffer (若解析度改變)
            if (pixelBuffer == null || pixelBuffer.Length != w * h)
            {
                pixelBuffer = new Color32[w * h];
                buffer = new byte[w * h * 4];
            }

            // 取得像素
            camTexture.GetPixels32(pixelBuffer);
            
            if (pixelBuffer == null || pixelBuffer.Length == 0) return;

            // 手動轉換 Color32 (RGBA) -> byte[] (RGBA)
            // 這樣可以傳給 ZXing 的 RGBA32 格式
            for (int i = 0; i < pixelBuffer.Length; i++)
            {
                buffer[i * 4 + 0] = pixelBuffer[i].r;
                buffer[i * 4 + 1] = pixelBuffer[i].g;
                buffer[i * 4 + 2] = pixelBuffer[i].b;
                buffer[i * 4 + 3] = pixelBuffer[i].a;
            }

            // 呼叫 Decode (指定格式為 RGBA32)
            var result = reader.Decode(buffer, w, h, RGBLuminanceSource.BitmapFormat.RGBA32);

            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                Debug.Log($"[QRScanManager] 掃描成功: {result.Text}");
                // 持續回報，不自動停止
                onScanResult?.Invoke(result.Text);
            }
        }
        catch (Exception ex)
        {
            // 靜默失敗
            // Debug.LogWarning($"[QRScanManager] Decode Error: {ex.Message}");
        }
    }

    #region Helpers
    
    private void UpdateAspectRatio()
    {
        if (aspectRatioFitter == null || camTexture == null) return;
        float ratio = (float)camTexture.width / camTexture.height;
        aspectRatioFitter.aspectRatio = ratio;
        int rotation = camTexture.videoRotationAngle;
        if (cameraDisplay != null)
             cameraDisplay.rectTransform.localEulerAngles = new Vector3(0, 0, -rotation);
    }

    private IEnumerator RequestCameraPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            yield return new WaitForSeconds(0.5f);
        }
#elif !UNITY_EDITOR && (UNITY_IOS || UNITY_STANDALONE_OSX)
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }
#else
        yield return null;
#endif
    }

    private bool HasCameraPermission()
    {
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Camera);
#elif !UNITY_EDITOR && (UNITY_IOS || UNITY_STANDALONE_OSX)
        return Application.HasUserAuthorization(UserAuthorization.WebCam);
#else
        return true;
#endif
    }

    private string GetBackCameraName()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0) return "";
        foreach (var d in devices) if (!d.isFrontFacing) return d.name;
        return devices[0].name;
    }

    void OnDisable() { StopScan(); }
    void OnDestroy() { StopScan(); }

    #endregion
}
