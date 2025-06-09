using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using DevConsole;

[RequireComponent(typeof(CharacterView)),
RequireComponent(typeof(CharacterSkin), typeof(CharacterShooter))]
public class CustomCharacterController : MonoBehaviour
{
    private CharacterMovement characterMovement;
    private CharacterView characterView;
    private CharacterSkin characterSkin;
    private CharacterShooter characterShooter;
    public CharacterStates CharacterState { get; private set; }

    [SerializeField] private Transform orientation;
    [SerializeField] private float maxWalkSpeed = 8f;
    [SerializeField] private float maxRunSpeed = 12f;
    [SerializeField] private float acceleration = 32;
    [SerializeField] private float deceleration = 32;
    [SerializeField] private float counterStrafingForce;
    [SerializeField] private InputActionReference aimAction;
    [SerializeField] private InputActionReference runAction;
    [SerializeField] private InputActionReference movementAction;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private HealthModule healthModule;

    private DevConsoleController gameConsole;

    private bool runningInput = false;

    private void Awake()
    {
        characterMovement = new CharacterMovement(rb, acceleration, deceleration, counterStrafingForce, orientation, movementAction, maxWalkSpeed, maxRunSpeed);
    }

    public void Start()
    {
        OnValidate();

        CharacterState = CharacterStates.Defaulft;
        aimCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;

        healthModule.Died += OnDied;
    }

    private void Update()
    {
        if (CharacterState == CharacterStates.Dead) return;

        if (CharacterState != CharacterStates.Console)
        {
            characterView.UpdateView();
            characterSkin.UpdateRotation();
            characterSkin.UpdateAnimation();
            characterShooter.UpdateAim();
        }
    }

    private void FixedUpdate()
    {
        if (CharacterState == CharacterStates.Console ||
            CharacterState == CharacterStates.Dead) return;

        if (CharacterState == CharacterStates.Aimming)
            characterMovement.UpdateAimMovement();
        else if (CharacterState == CharacterStates.Defaulft)
            characterMovement.UpdateWalkMovement();
        else if (CharacterState == CharacterStates.Running)
            characterMovement.UpdateRunMovement();
    }

    private void OnValidate()
    {
        if (characterView == null)
            characterView = GetComponent<CharacterView>();

        if (characterSkin == null)
            characterSkin = GetComponent<CharacterSkin>();

        if (characterShooter == null)
            characterShooter = GetComponent<CharacterShooter>();

        if (gameConsole == null)
            gameConsole = FindAnyObjectByType<DevConsoleController>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (healthModule == null)
            healthModule = GetComponentInChildren<HealthModule>();
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
        if (CharacterState == CharacterStates.Console ||
            CharacterState == CharacterStates.Dead) return;
        CharacterState = CharacterStates.Aimming;
        if (aimCamera != null)
            aimCamera.gameObject.SetActive(true);
    }

    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        if (CharacterState == CharacterStates.Console ||
            CharacterState == CharacterStates.Dead) return;
        CharacterState = runningInput ? CharacterStates.Running : CharacterStates.Defaulft;
        if (aimCamera != null)
            aimCamera.gameObject.SetActive(false);
    }
    private void OnRunPerformed(InputAction.CallbackContext context)
    {
        if (CharacterState == CharacterStates.Console ||
            CharacterState == CharacterStates.Dead) return;
        runningInput = true;
        if (CharacterState == CharacterStates.Aimming)
            aimCamera.gameObject.SetActive(false);
        CharacterState = CharacterStates.Running;
    }

    private void OnRunCanceled(InputAction.CallbackContext context)
    {
        if (CharacterState == CharacterStates.Console ||
            CharacterState == CharacterStates.Dead) return;
        runningInput = false;
        CharacterState = CharacterStates.Defaulft;
    }

    private void OnConsoleOpened()
    {
        CharacterState = CharacterStates.Console;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnConsoleClosed()
    {
        if (CharacterState == CharacterStates.Dead)
            return;
        if (CharacterState == CharacterStates.Aimming)
            aimCamera.gameObject.SetActive(false);

        CharacterState = CharacterStates.Defaulft;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDied()
    {
        CharacterState = CharacterStates.Dead;
    }
}
