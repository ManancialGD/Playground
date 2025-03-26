using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovement
{
    private AnimationCurve walkCurve;
    private AnimationCurve runCurve;
    private AnimationCurve decelerationCurve;
    private Transform orientation;
    private InputActionReference movementAction;

    private Vector2 movementInput;
    private Vector3 moveDir;
    private Rigidbody rb;

    public CharacterMovement(Rigidbody rb,
                            AnimationCurve walkCurve, AnimationCurve runCurve, AnimationCurve decelerationCurve,
                            Transform orientation, InputActionReference movementAction)
    {
        this.rb = rb;
        this.walkCurve = walkCurve;
        this.runCurve = runCurve;
        this.decelerationCurve = decelerationCurve;
        this.orientation = orientation;
        this.movementAction = movementAction;
        
        movementAction.action.performed += OnMovementAction;
        movementAction.action.canceled += OnMovementAction;
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
    }

    private bool Validate()
    {
        return rb != null;
    }

    private void OnMovementAction(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();

        if (v != null) movementInput = v;
    }
}