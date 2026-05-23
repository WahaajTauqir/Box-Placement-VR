using UnityEngine;

/// <summary>
/// Exposes application quit behavior for UI buttons and UnityEvents.
/// </summary>
public class QuitGame : MonoBehaviour
{
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
