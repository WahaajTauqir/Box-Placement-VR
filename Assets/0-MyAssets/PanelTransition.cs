using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Utility that fades CanvasGroups in/out with optional completion callbacks.
/// Pass the CanvasGroups as parameters when calling the methods.
/// </summary>
public class PanelTransition: MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] float duration = 0.5f;
    [Tooltip("Optional. If set, time is evaluated through this curve (0-1) for closing.")]
    [SerializeField] AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    Coroutine _fadeRoutine;

    /// <summary>
    /// Call this to start fading the closing CanvasGroup's alpha to 0.
    /// When that finishes, the close callback is invoked, the closing object is turned off,
    /// and if an opening CanvasGroup is assigned, its object is turned on and its alpha is lerped to 1.
    /// </summary>
    /// 
    public void CloseAndOpenPanel(CanvasGroup closingCanvasGroup, CanvasGroup openingCanvasGroup, Action onCloseComplete = null, Action onOpenComplete = null)
    {
        if (closingCanvasGroup == null)
        {
            Debug.LogWarning("PanelTransition: No closing CanvasGroup assigned.", this);
            onCloseComplete?.Invoke();

            if (openingCanvasGroup != null)
            {
                StartOpenRoutine(openingCanvasGroup, onOpenComplete);
            }
            else
            {
                onOpenComplete?.Invoke();
            }
            return;
        }

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(CloseAndOpenRoutine(closingCanvasGroup, openingCanvasGroup, onCloseComplete, onOpenComplete));
    }

    public void ClosePanel(CanvasGroup canvasGroup, Action onCloseComplete = null)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("PanelTransition: No CanvasGroup assigned for close.", this);
            onCloseComplete?.Invoke();
            return;
        }

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(CloseRoutine(canvasGroup, onCloseComplete, true));
    }

    public void OpenPanel(CanvasGroup canvasGroup, Action onOpenComplete = null)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("PanelTransition: No CanvasGroup assigned for open.", this);
            onOpenComplete?.Invoke();
            return;
        }

        if (!canvasGroup.gameObject.activeSelf)
            canvasGroup.gameObject.SetActive(true);

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(OpenRoutine(canvasGroup, onOpenComplete, true));
    }

    IEnumerator CloseAndOpenRoutine(CanvasGroup closingCanvasGroup, CanvasGroup openingCanvasGroup, Action onCloseComplete, Action onOpenComplete)
    {
        yield return CloseRoutine(closingCanvasGroup, onCloseComplete, false);

        if (openingCanvasGroup != null)
        {
            yield return OpenRoutine(openingCanvasGroup, onOpenComplete, false);
        }
        else
        {
            onOpenComplete?.Invoke();
        }

        _fadeRoutine = null;
    }

    IEnumerator CloseRoutine(CanvasGroup canvasGroup, Action onCloseComplete, bool clearRoutineAtEnd)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curve.Evaluate(t));
            
            yield return null;
            
        }

        canvasGroup.alpha = 0f;
        
        onCloseComplete?.Invoke();
        
        canvasGroup.gameObject.SetActive(false);

        if (clearRoutineAtEnd)
            _fadeRoutine = null;

    }

    IEnumerator OpenRoutine(CanvasGroup canvasGroup, Action onOpenComplete, bool clearRoutineAtEnd)
    {
        if (!canvasGroup.gameObject.activeSelf)
            canvasGroup.gameObject.SetActive(true);

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, curve.Evaluate(t));
            yield return null;
        }

        canvasGroup.alpha = 1f;
        onOpenComplete?.Invoke();

        if (clearRoutineAtEnd)
            _fadeRoutine = null;
    }

    void StartOpenRoutine(CanvasGroup canvasGroup, Action onOpenComplete)
    {
        if (!canvasGroup.gameObject.activeSelf)
            canvasGroup.gameObject.SetActive(true);

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(OpenRoutine(canvasGroup, onOpenComplete, true));
    }
}
