using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovement
{
    private AnimationCurve walkCurve;
    private AnimationCurve runCurve;
    private AnimationCurve decelerationCurve;
    private Transform orientation;
    private float maxWalkSpeed = 8f;
    private float maxRunSpeed = 12f;
    private InputActionReference movementAction;

    private Vector2 movementInput;
    private Vector3 moveDir;
    private Rigidbody rb;

    public CharacterMovement(Rigidbody rb,
                            AnimationCurve walkCurve, AnimationCurve runCurve, AnimationCurve decelerationCurve,
                            Transform orientation, InputActionReference movementAction,
                            float maxWalkSpeed = 8f, float maxRunSpeed = 12f)
    {
        this.rb = rb;
        this.walkCurve = walkCurve;
        this.runCurve = runCurve;
        this.decelerationCurve = decelerationCurve;
        this.orientation = orientation;
        this.movementAction = movementAction;
        this.maxWalkSpeed = maxWalkSpeed;
        this.maxRunSpeed = maxRunSpeed;

        movementAction.action.performed += OnMovementAction;
        movementAction.action.canceled += OnMovementAction;
    }

    public void UpdateWalkMovement()
    {
        UpdateMovement(walkCurve, maxWalkSpeed);
    }

    public void UpdateRunMovement()
    {
        UpdateMovement(runCurve, maxRunSpeed);
    }
    public void UpdateAimMovement()
    {
        UpdateMovement(walkCurve, maxWalkSpeed, 0.75f);
    }

    private void UpdateMovement(AnimationCurve movementCurve, float maxSpeed, float velMultiplier = 1)
    {
        if (Mathf.Abs(movementInput.x) > 1e-5f || Mathf.Abs(movementInput.y) > 1e-5f)
        {
            Vector3 flatForward = new Vector3(orientation.forward.x, 0, orientation.forward.z).normalized;
            Vector3 flatRight = new Vector3(orientation.right.x, 0, orientation.right.z).normalized;

            moveDir = flatForward * movementInput.y + flatRight * movementInput.x;

            Vector3 horizontalVelocity = new(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float speed = movementCurve.Evaluate(horizontalVelocity.magnitude) * velMultiplier;
            speed = Mathf.Clamp(speed, 0, maxSpeed - rb.linearVelocity.magnitude);
            Vector3 force = moveDir.normalized * speed;

            rb.AddForce(force - horizontalVelocity, ForceMode.Impulse);
        }
        else
        {
            Vector3 horizontalVelocity = new(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            moveDir = horizontalVelocity;

            float speed = decelerationCurve.Evaluate(horizontalVelocity.magnitude);
            speed = Mathf.Clamp(speed, 0, rb.linearVelocity.magnitude);

            Vector3 force = -(moveDir.normalized * speed);

            rb.AddForce(force - horizontalVelocity, ForceMode.Impulse);
        }
        Debug.Log(rb.linearVelocity.magnitude);
    }

    private void OnMovementAction(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();

        if (v != null) movementInput = v;
    }
}
