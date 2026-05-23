using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Follow_Manager : MonoBehaviour
{
    [SerializeField] GameObject menu;
    [SerializeField] Transform head;
    [SerializeField] float spawnDistance;

    private void Update()
    {
        menu.transform.position = head.position + new Vector3(head.forward.x, 0, head.forward.z).normalized * spawnDistance;

        menu.transform.LookAt(new Vector3(head.position.x, menu.transform.position.y, head.position.z));
        menu.transform.forward *= -1;
    }
}
