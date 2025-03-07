using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterMovement), typeof(CharacterView)),
RequireComponent(typeof(CharacterSkin), typeof(CharacterShooter))]
public class CustomCharacterController : MonoBehaviour
{
    private CharacterMovement characterMovement;
    private CharacterView characterView;
    private CharacterSkin characterSkin;
    private CharacterShooter characterShooter;

    public CharacterStates CharacterState { get; private set; }

    [SerializeField] private InputActionReference aimAction;
    [SerializeField] private InputActionReference runAction;
    [SerializeField] private CinemachineCamera aimCamera;

    private bool runningInput = false;

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
        characterShooter.UpdateAim();
    }

    private void FixedUpdate()
    {
        if (CharacterState == CharacterStates.Aimming)
            characterMovement.UpdateAimMovement();
        else if (CharacterState == CharacterStates.Defaulft)
            characterMovement.UpdateWalkMovement();
        else if (CharacterState == CharacterStates.Running)
            characterMovement.UpdateRunMovement();
    }

    private void OnValidate()
    {
        if (characterMovement == null)
            characterMovement = GetComponent<CharacterMovement>();

        if (characterView == null)
            characterView = GetComponent<CharacterView>();

        if (characterSkin == null)
            characterSkin = GetComponent<CharacterSkin>();

        if (characterShooter == null)
            characterShooter = GetComponent<CharacterShooter>();
    }

    private void OnEnable()
    {
        if (aimAction != null)
        {
            aimAction.action.performed += OnAimPerformed;
            aimAction.action.canceled += OnAimCanceled;
        }
        if (runAction != null)
        {
            runAction.action.performed += OnRunPerformed;
            runAction.action.canceled += OnRunCanceled;
        }
    }

    private void OnDisable()
    {
        if (aimAction != null)
        {
            aimAction.action.performed -= OnAimPerformed;
            aimAction.action.canceled -= OnAimCanceled;
        }
        if (runAction != null)
        {
            runAction.action.performed -= OnRunPerformed;
            runAction.action.canceled -= OnRunCanceled;
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
        CharacterState = runningInput ? CharacterStates.Running : CharacterStates.Defaulft;
        if (aimCamera != null)
            aimCamera.gameObject.SetActive(false);
    }
    private void OnRunPerformed(InputAction.CallbackContext context)
    {
        runningInput = true;
        if (CharacterState == CharacterStates.Aimming)
            aimCamera.gameObject.SetActive(false);
        CharacterState = CharacterStates.Running;
    }

    private void OnRunCanceled(InputAction.CallbackContext context)
    {
        runningInput = false;
        CharacterState = CharacterStates.Defaulft;
    }
}
