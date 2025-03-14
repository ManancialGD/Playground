using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Scripting.APIUpdating;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private AnimationCurve walkCurve;
    [SerializeField] private AnimationCurve runCurve;
    [SerializeField] private AnimationCurve decelerationCurve;

    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField]
    private InputActionReference movementAction;

    [Header("Debugging")]
    [SerializeField, Tooltip("Enable this to display debug messages from this script in the Console.")]
    private bool showDebugMessages = false;
    [SerializeField, Tooltip("If enabled, debug messages will include the object's name as an identifier."), ShowIf(nameof(showDebugMessages))]
    private bool identifyObject = true;

    private Vector2 movementInput;
    private Vector3 moveDir;
    private Rigidbody rb;

    private void Awake()
    {
        OnValidate();
    }

    public void UpdateWalkMovement()
    {
        UpdateMovement(walkCurve);
    }

    public void UpdateRunMovement()
    {
        UpdateMovement(runCurve);
    }
    public void UpdateAimMovement()
    {
        UpdateMovement(walkCurve, 0.75f);
    }

    private void UpdateMovement(AnimationCurve movementCurve, float velMultiplier = 1)
    {
        if (Mathf.Abs(movementInput.x) > 1e-5f || Mathf.Abs(movementInput.y) > 1e-5f)
        {
            Vector3 flatForward = new Vector3(orientation.forward.x, 0, orientation.forward.z).normalized;
            Vector3 flatRight = new Vector3(orientation.right.x, 0, orientation.right.z).normalized;

            moveDir = flatForward * movementInput.y + flatRight * movementInput.x;

            Vector3 horizontalVelocity = new(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 force = moveDir.normalized *
                                movementCurve.Evaluate(horizontalVelocity.magnitude) * velMultiplier;

            rb.AddForce(force - horizontalVelocity, ForceMode.Force);
        }
        else
        {
            Vector3 horizontalVelocity = new(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            moveDir = horizontalVelocity;

            Vector3 force = -(moveDir.normalized * decelerationCurve.Evaluate(horizontalVelocity.magnitude));

            rb.AddForce(force - horizontalVelocity, ForceMode.Force);
        }
        Log(rb.linearVelocity.magnitude.ToString());
    }

    private void OnEnable()
    {
        movementAction.action.performed += OnMovementAction;
        movementAction.action.canceled += OnMovementAction;
    }
    private void OnDisable()
    {
        movementAction.action.performed -= OnMovementAction;
        movementAction.action.canceled -= OnMovementAction;
    }

    private void OnValidate()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    private void OnMovementAction(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();

        if (v != null) movementInput = v;
    }

    /// <summary>
    /// Logs a debug message to the Console if debugging is enabled.
    /// Includes the object's name as an identifier if 'identifyObject' is true.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    private void Log(string message)
    {
        if (showDebugMessages)
        {
            if (identifyObject)
                Debug.Log(message, this); // Includes object name in the log message.
            else
                Debug.Log(message); // Logs without object name.
        }
    }
}