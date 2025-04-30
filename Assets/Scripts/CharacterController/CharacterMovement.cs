using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovement
{
    private float acceleration;
    private float deceleration;
    private float counterStrafingForce;
    private Transform orientation;
    private float maxWalkSpeed = 8f;
    private float maxRunSpeed = 12f;
    
    private InputActionReference movementAction;

    private Vector2 movementInput;
    private Vector3 moveDir;
    private Rigidbody rb;

    public CharacterMovement(Rigidbody rb,
                            float acceleration, float deceleration, float counterStrafingForce,
                            Transform orientation, InputActionReference movementAction,
                            float maxWalkSpeed = 8f, float maxRunSpeed = 12f)
    {
        this.rb = rb;
        this.acceleration = acceleration;
        this.deceleration = deceleration;
        this.counterStrafingForce = counterStrafingForce;
        this.orientation = orientation;
        this.movementAction = movementAction;
        this.maxWalkSpeed = maxWalkSpeed;
        this.maxRunSpeed = maxRunSpeed;

        movementAction.action.performed += OnMovementAction;
        movementAction.action.canceled += OnMovementAction;
    }

    public void UpdateWalkMovement()
    {
        UpdateMovement(maxWalkSpeed);
    }

    public void UpdateRunMovement()
    {
        UpdateMovement(maxRunSpeed);
    }

    public void UpdateAimMovement()
    {
        UpdateMovement(maxWalkSpeed);
    }

    private void UpdateMovement(float maxSpeed)
    {
        if (Mathf.Abs(movementInput.x) > 1e-5f || Mathf.Abs(movementInput.y) > 1e-5f)
        {
            Vector3 flatForward = new Vector3(orientation.forward.x, 0, orientation.forward.z).normalized;
            Vector3 flatRight = new Vector3(orientation.right.x, 0, orientation.right.z).normalized;

            moveDir = (flatForward * movementInput.y + flatRight * movementInput.x).normalized;

            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 targetVelocity = moveDir * maxSpeed;

            // Calculate desired change in velocity
            Vector3 desiredDeltaV = targetVelocity - horizontalVelocity;

            // Determine if counter-strafing (input opposes current velocity)
            float velocityDot = Vector3.Dot(moveDir, horizontalVelocity.normalized);
            bool isCounterStrafing = velocityDot < -0.5f; // Adjust threshold as needed

            // Use deceleration when counter-strafing
            float appliedAcceleration = isCounterStrafing ? counterStrafingForce : acceleration;
            float maxDeltaV = appliedAcceleration * Time.fixedDeltaTime;

            // Clamp the velocity change to avoid overshooting
            Vector3 clampedDeltaV = Vector3.ClampMagnitude(desiredDeltaV, maxDeltaV);

            // Apply force as an impulse (accounting for mass)
            rb.AddForce(rb.mass * clampedDeltaV, ForceMode.Impulse);
        }
        else
        {
            Vector3 horizontalVelocity = new(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            moveDir = horizontalVelocity;

            float speed = deceleration * Time.fixedDeltaTime;
            speed = Mathf.Clamp(speed, 0, horizontalVelocity.magnitude);

            Vector3 force = -(moveDir.normalized * speed);

            rb.AddForce(force, ForceMode.Impulse);
        }
    }

    private void OnMovementAction(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();

        if (v != null) movementInput = v;
    }
}
