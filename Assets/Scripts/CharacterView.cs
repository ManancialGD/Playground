using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterView : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform orientation;
    [SerializeField] private InputActionReference lookAction;

    [SerializeField, Range(0.5f, 15f)]
    private float senseX = .25f;
    [SerializeField, Range(0.5f, 15f)]
    private float senseY = .5f;

    [SerializeField, Range(0.15f, 15f)]
    private float aimSense = .25f;

    [SerializeField] private float maxAngleUp = 45;
    [SerializeField] private float maxAngleDown = 15;
    [SerializeField] private bool invertMouseY = false;
    [SerializeField] private CustomCharacterController characterController;

    private Vector2 lookInput;


    private void Awake()
    {
        OnValidate();
    }

    public void UpdateView()
    {
        Vector2 rotation = lookInput;

        rotation.x *= senseX;
        rotation.y *= senseY;

        rotation.y *= invertMouseY ? 1 : -1;

        if (characterController.CharacterState == CharacterStates.Aimming)
            rotation *= aimSense;

        cameraTarget.transform.rotation *= Quaternion.AngleAxis(rotation.x * senseX, Vector3.up);
        cameraTarget.transform.rotation *= Quaternion.AngleAxis(rotation.y * senseY, Vector3.right);

        Vector3 angles = cameraTarget.transform.localRotation.eulerAngles;
        angles.z = 0;

        float angle = cameraTarget.transform.localRotation.eulerAngles.x;

        if (angle > 180 && angle < 360 - maxAngleDown)
        {
            angles.x = 360 - maxAngleDown;
        }
        else if (angle < 180 && angle > maxAngleUp)
        {
            angles.x = maxAngleUp;
        }

        cameraTarget.transform.localRotation = Quaternion.Euler(angles);

        orientation.localEulerAngles = new Vector3(0, cameraTarget.localEulerAngles.y, 0);
    }

    private void OnEnable()
    {
        lookAction.action.performed += OnLookAction;
        lookAction.action.canceled += OnLookAction;
    }

    private void OnDisable()
    {
        lookAction.action.performed -= OnLookAction;
        lookAction.action.canceled -= OnLookAction;
    }

    private void OnLookAction(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnValidate()
    {
        if (characterController == null)
            characterController = GetComponent<CustomCharacterController>();
    }
}