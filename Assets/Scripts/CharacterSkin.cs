using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSkin : MonoBehaviour
{
    [SerializeField] private Transform skinTransform;
    [SerializeField] private Transform orientation;
    [SerializeField] private Animator anim;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField, Range(0.1f, 30)] private float slerpTime;

    private Vector2 moveInput;

    public void UpdateRotation()
    {
        skinTransform.rotation = Quaternion.Slerp(skinTransform.rotation, orientation.rotation, Time.deltaTime * slerpTime);
    }

    public void UpdateAnimation()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        float forwardValue = Vector3.Dot(rb.linearVelocity, orientation.forward);
        float horizontalValue = Vector3.Dot(rb.linearVelocity, orientation.right);

        anim.SetFloat("xInput", horizontalValue);
        anim.SetFloat("yInput", forwardValue);
    }

    private void OnEnable()
    {
        moveAction.action.performed += OnMovementAction;
        moveAction.action.canceled += OnMovementAction;
    }
    private void OnDisable()
    {
        moveAction.action.performed -= OnMovementAction;
        moveAction.action.canceled -= OnMovementAction;
    }

    private void OnMovementAction(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();

        if (v != null) moveInput = v;
    }

    public void OnValidate()
    {
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }
}
