using UnityEngine;
using TMPro;
using Whisper;
using System.Threading.Tasks;
using UnityEngine.Events; // Needed for VR system mapping

public class VRVoiceController : MonoBehaviour
{
    [Header("Whisper Configuration")]
    public WhisperManager whisper;
    public TextMeshProUGUI resultText;

    [Header("VR Command Actions")]
    [Tooltip("Triggered when the word 'jump' or 'fly' is detected.")]
    public UnityEvent onVRJump;
    
    [Tooltip("Triggered when the word 'open', 'menu', or 'inventory' is detected.")]
    public UnityEvent onVROpenMenu;

    private string _microphone;
    private AudioClip _clip;
    private const int SampleRate = 16000;
    private bool _isRecording = false;
void Start()
{
    #if UNITY_ANDROID
    if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
    {
        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
    }
    #endif

    if (Microphone.devices.Length > 0)
    {
        _microphone = Microphone.devices[0];
        
        // Semantic check: Loop through found mics to explicitly target VR hardware if available
        foreach (var device in Microphone.devices)
        {
            if (device.ToLower().Contains("oculus") || device.ToLower().Contains("virtual") || device.ToLower().Contains("xr"))
            {
                _microphone = device;
                break;
            }
        }
        Debug.Log("VR Mic Selected: " + _microphone);
    }
    else
    {
        if (resultText != null) resultText.text = "No Mic Found!";
    }
}
    // Button 1 Hook
    public void StartVoiceRecording()
    {
        if (_isRecording) return;

        _isRecording = true;
        if (resultText != null) resultText.text = "<color=red>Listening...</color>";
        
        _clip = Microphone.Start(_microphone, false, 10, SampleRate);
    }

    // Button 2 Hook
    public async void StopAndExecuteCommand()
    {
        if (!_isRecording) return;

        _isRecording = false;
        Microphone.End(_microphone);
        if (resultText != null) resultText.text = "Parsing command...";

        var task = await whisper.GetTextAsync(_clip);
        string result = task.Result.ToLower().Trim();
        
        if (resultText != null) resultText.text = "Heard: " + result;

        ExecuteVRCommand(result);
    }

    private void ExecuteVRCommand(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Contains("blank_audio")) return;

        // Map words to the Unity Events
        if (text.Contains("jump") || text.Contains("fly"))
        {
            onVRJump?.Invoke();
        }
        else if (text.Contains("open") || text.Contains("menu") || text.Contains("inventory"))
        {
            onVROpenMenu?.Invoke();
        }
    }
}