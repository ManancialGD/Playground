using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSkin : MonoBehaviour
{
    [SerializeField] private Transform skinTransform;
    [SerializeField] private Transform orientation;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator anim;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField, Range(0.1f, 30)] private float slerpTime;

    public void UpdateRotation()
    {
        skinTransform.rotation = Quaternion.Slerp(skinTransform.rotation, orientation.rotation, Time.deltaTime * slerpTime);
    }

    public void UpdateAnimation()
    {
        float forwardValue = Vector3.Dot(rb.linearVelocity, orientation.forward);
        float horizontalValue = Vector3.Dot(rb.linearVelocity, orientation.right);

        anim.SetFloat("xInput", horizontalValue);
        anim.SetFloat("yInput", forwardValue);
    }
    private void OnValidate()
    {
        if(rb == null) rb = GetComponent<Rigidbody>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }
}
