using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterMovement), typeof(CharacterView), typeof(CharacterSkin))]
public class CustomCharacterController : MonoBehaviour
{
    private CharacterMovement characterMovement;
    private CharacterView characterView;
    private CharacterSkin characterSkin;

    public CharacterStates CharacterState { get; private set; }

    [SerializeField] private InputActionReference aimAction;
    [SerializeField] private CinemachineCamera aimCamera;

    public void Start()
    {
        OnValidate();

        CharacterState = CharacterStates.Defaulft;
        aimCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        characterView.UpdateView();
        characterSkin.UpdateRotation();
        characterSkin.UpdateAnimation();
    }

    private void FixedUpdate()
    {
        characterMovement.UpdateMovement();
    }

    private void OnValidate()
    {
        if (characterMovement == null)
            characterMovement = GetComponent<CharacterMovement>();

        if (characterView == null)
            characterView = GetComponent<CharacterView>();
        
        if (characterSkin == null)
            characterSkin = GetComponent<CharacterSkin>();
    }

    private void OnEnable()
    {
        if (aimAction != null)
        {
            aimAction.action.performed += OnAimPerformed;
            aimAction.action.canceled += OnAimCanceled;
        }
    }

    private void OnDisable()
    {
        if (aimAction != null)
        {
            aimAction.action.performed -= OnAimPerformed;
            aimAction.action.canceled -= OnAimCanceled;
        }
    }

    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        CharacterState = CharacterStates.Aimming;
        if (aimCamera != null)
            aimCamera.gameObject.SetActive(true);
    }

    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        CharacterState = CharacterStates.Defaulft;
        if (aimCamera != null)
            aimCamera.gameObject.SetActive(false);
    }
}
