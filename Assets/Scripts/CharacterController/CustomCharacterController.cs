using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using GameConsole;

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
    private GameConsoleController gameConsole;

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
        if (CharacterState != CharacterStates.Console)
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

        if (gameConsole == null)
            gameConsole = FindAnyObjectByType<GameConsoleController>();
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

        if (gameConsole != null)
        {
            gameConsole.ConsoleOpened.AddListener(OnConsoleOpened);
            gameConsole.ConsoleClosed.AddListener(OnConsoleClosed);
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
        if (gameConsole != null)
        {
            gameConsole.ConsoleOpened.RemoveListener(OnConsoleOpened);
            gameConsole.ConsoleClosed.RemoveListener(OnConsoleClosed);
        }
    }

    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        if (CharacterState == CharacterStates.Console) return;
        CharacterState = CharacterStates.Aimming;
        if (aimCamera != null)
            aimCamera.gameObject.SetActive(true);
    }

    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        if (CharacterState == CharacterStates.Console) return;
        CharacterState = runningInput ? CharacterStates.Running : CharacterStates.Defaulft;
        if (aimCamera != null)
            aimCamera.gameObject.SetActive(false);
    }
    private void OnRunPerformed(InputAction.CallbackContext context)
    {
        if (CharacterState == CharacterStates.Console) return;
        runningInput = true;
        if (CharacterState == CharacterStates.Aimming)
            aimCamera.gameObject.SetActive(false);
        CharacterState = CharacterStates.Running;
    }

    private void OnRunCanceled(InputAction.CallbackContext context)
    {
        if (CharacterState == CharacterStates.Console) return;
        runningInput = false;
        CharacterState = CharacterStates.Defaulft;
    }

    private void OnConsoleOpened()
    {
        CharacterState = CharacterStates.Console;
    }

    private void OnConsoleClosed()
    {
        if (CharacterState == CharacterStates.Aimming)
            aimCamera.gameObject.SetActive(false);

        CharacterState = CharacterStates.Defaulft;
    }
}
