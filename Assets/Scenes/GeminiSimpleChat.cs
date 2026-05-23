using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Minimal Gemini text chat: TMP input + output label, Send button calls SendMessage().
/// Copy this file only to another Unity project (needs TextMeshPro).
/// </summary>
public class GeminiSimpleChat : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputField;
    public TextMeshProUGUI outputText;

    [Header("API")]
    [SerializeField] string apiKey = "";
    [SerializeField] string model = "gemini-2.0-flash";

    [Header("Optional")]
    [SerializeField] float minSecondsBetweenRequests = 1f;

    string ApiUrl => $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

    bool isRequesting;
    float lastRequestTime;

    public void SendMessage()
    {
        if (isRequesting)
            return;

        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
            return;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            SetOutput("Error: Set API Key on GeminiSimpleChat in the Inspector.");
            return;
        }

        if (Time.time - lastRequestTime < minSecondsBetweenRequests)
            return;

        string message = inputField.text.Trim();
        inputField.text = "";
        lastRequestTime = Time.time;
        StartCoroutine(SendToGemini(message));
    }

    IEnumerator SendToGemini(string userMessage)
    {
        isRequesting = true;
        SetOutput("Thinking...");

        var body = new GeminiGenerateRequest
        {
            contents = new[]
            {
                new GeminiContent
                {
                    parts = new[] { new GeminiPart { text = userMessage } }
                }
            }
        };

        string json = JsonUtility.ToJson(body);
        string url = $"{ApiUrl}?key={UnityWebRequest.EscapeURL(apiKey)}";

        using (var www = new UnityWebRequest(url, "POST"))
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(payload);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                string detail = www.downloadHandler != null ? www.downloadHandler.text : "";
                SetOutput($"Error {(long)www.responseCode}: {www.error}\n{detail}");
                isRequesting = false;
                yield break;
            }

            var response = JsonUtility.FromJson<GeminiGenerateResponse>(www.downloadHandler.text);
            if (response?.candidates != null && response.candidates.Length > 0 &&
                response.candidates[0].content?.parts != null &&
                response.candidates[0].content.parts.Length > 0)
            {
                SetOutput(response.candidates[0].content.parts[0].text);
            }
            else
            {
                SetOutput("No text in response.");
            }
        }

        isRequesting = false;
    }

    void SetOutput(string text)
    {
        if (outputText != null)
            outputText.text = text;
    }

    [System.Serializable]
    class GeminiPart { public string text; }

    [System.Serializable]
    class GeminiContent { public GeminiPart[] parts; }

    [System.Serializable]
    class GeminiGenerateRequest { public GeminiContent[] contents; }

    [System.Serializable]
    class GeminiGenerateResponse { public GeminiCandidate[] candidates; }

    [System.Serializable]
    class GeminiCandidate { public GeminiContent content; }
}
