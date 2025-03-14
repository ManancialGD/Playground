using System.IO;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterView : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform orientation;
    [SerializeField] private InputActionReference lookAction;

    [SerializeField, Range(0.5f, 15f)]
    private float senseX = .5f;
    [SerializeField, Range(0.5f, 15f)]
    private float senseY = .5f;
    [SerializeField] private float maxAngleUp = 15;
    [SerializeField] private float maxAngleDown = 45;
    [SerializeField] private CustomCharacterController characterController;
    [SerializeField] private CharacterShooter characterShooter;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField, Range(0, .15f)] private float impulseForce = .05f;
    [SerializeField] private PlayerSettings playerSettings;
    private Vector2 lookInput;

    private void Awake()
    {
        OnValidate();
    }

    public void UpdateView()
    {
        Vector2 rotation = lookInput;

        //? The sensitivity axes for player controls are swapped to align with the player's expected behavior. 
        rotation.x *= senseX * playerSettings.SensitivityY;
        rotation.y *= senseY * playerSettings.SensitivityX;

        rotation.y *= playerSettings.InvertMouseY ? 1 : -1;

        if (characterController.CharacterState == CharacterStates.Aimming)
            rotation *= playerSettings.AimMultiplier;

        cameraTarget.transform.rotation *= Quaternion.AngleAxis(rotation.x * senseX, Vector3.up);
        cameraTarget.transform.rotation *= Quaternion.AngleAxis(rotation.y * senseY, Vector3.right);

        Vector3 angles = cameraTarget.transform.localRotation.eulerAngles;
        angles.z = 0;

        float angle = cameraTarget.transform.localRotation.eulerAngles.x;

        if (angle > 180 && angle < 360 - maxAngleUp)
        {
            angles.x = 360 - maxAngleUp;
        }
        else if (angle < 180 && angle > maxAngleDown)
        {
            angles.x = maxAngleDown;
        }

        cameraTarget.transform.localRotation = Quaternion.Euler(angles);

        orientation.localEulerAngles = new Vector3(0, cameraTarget.localEulerAngles.y, 0);
    }

    private void OnEnable()
    {
        lookAction.action.performed += OnLookAction;
        lookAction.action.canceled += OnLookAction;
        characterShooter.ShootEvent += OnShootEvent;
    }

    private void OnDisable()
    {
        lookAction.action.performed -= OnLookAction;
        lookAction.action.canceled -= OnLookAction;
        characterShooter.ShootEvent -= OnShootEvent;
    }

    private void OnLookAction(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnShootEvent()
    {
        impulseSource.DefaultVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 0));
        impulseSource.GenerateImpulse(impulseForce);
    }

    private void OnValidate()
    {
        if (characterController == null)
            characterController = GetComponent<CustomCharacterController>();
        if (characterShooter == null)
            characterShooter = GetComponent<CharacterShooter>();
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
        
        // Resources Loading is expensive, we hope to do this once max.
        if (playerSettings == null)
            playerSettings = Resources.Load<PlayerSettings>(Path.Combine("Settings", "PlayerSettings"));
    }
}