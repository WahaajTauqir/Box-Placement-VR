using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Call <see cref="Trigger"/> to invoke a UnityEvent after a delay (default 5 seconds).
/// </summary>
public class DelayedUnityEventInvoker : MonoBehaviour
{
    [SerializeField] float delaySeconds = 5f;
    [SerializeField] UnityEvent onDelayedInvoke;

    Coroutine _delayRoutine;

    /// <summary>
    /// Starts the delay. If already waiting, the previous wait is cancelled and restarted.
    /// </summary>

    public void Start()
    {
        Trigger();
    }

    public void Trigger()
    {
        if (_delayRoutine != null)
            StopCoroutine(_delayRoutine);

        _delayRoutine = StartCoroutine(DelayAndInvoke());
    }

    IEnumerator DelayAndInvoke()
    {
        yield return new WaitForSeconds(delaySeconds);
        onDelayedInvoke?.Invoke();
        _delayRoutine = null;
    }

    void OnDisable()
    {
        if (_delayRoutine != null)
        {
            StopCoroutine(_delayRoutine);
            _delayRoutine = null;
        }
    }
}
