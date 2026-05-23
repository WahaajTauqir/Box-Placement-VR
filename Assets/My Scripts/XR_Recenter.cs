using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class XR_Recenter : MonoBehaviour
{
    [SerializeField] Transform target;

    private void Start()
    {
        Recenter();
    }

    void Recenter()
    {
        XROrigin xROrigin = GetComponent<XROrigin>();
        xROrigin.MoveCameraToWorldLocation(target.position);
        xROrigin.MatchOriginUpCameraForward(target.up, target.forward);
    }
}
