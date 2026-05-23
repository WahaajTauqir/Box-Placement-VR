using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Input_Action_Manager : MonoBehaviour
{
    public InputActionReference aButtonAction;

    [SerializeField] private Texture[] textures;
    [SerializeField] private Material targetMaterial; 

    private int currentTextureIndex = 0;

    private void Start()
    {
        if (textures == null || textures.Length == 0)
        {
            Debug.LogError("No textures assigned in the Inspector.");
        }

        if (targetMaterial == null)
        {
            Debug.LogError("Target Material is not assigned in the Inspector.");
        }
    }

    private void OnEnable()
    {
        if (aButtonAction != null)
            aButtonAction.action.performed += OnAButtonPressed;
    }

    private void OnDisable()
    {
        if (aButtonAction != null)
            aButtonAction.action.performed -= OnAButtonPressed;
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        if (textures != null && textures.Length > 0 && targetMaterial != null)
        {
            currentTextureIndex = (currentTextureIndex + 1) % textures.Length;
            targetMaterial.mainTexture = textures[currentTextureIndex];
        }
        else
        {
            Debug.LogWarning("Texture array or target material is not properly set.");
        }
    }
}
