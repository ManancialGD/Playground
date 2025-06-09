using System;
using NaughtyAttributes.Test;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class HealthModule : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Animator anim;
    private int currentHealth = 100;
    public int CurrentHealth => currentHealth;  

    public bool IsDead { get; private set; } = false;
    public event Action Died;

    private RagDollLimb[] ragDollLimbs;

    private void Start()
    {
        currentHealth = maxHealth;

        ragDollLimbs = GetComponentsInChildren<RagDollLimb>();

        DeactivateRagdoll();
        foreach (var limb in ragDollLimbs)
        {
            if (limb != null)
            {
                limb.Hit += TakeDamage;
            }
        }
    }

    public void TakeDamage(RagDollLimb limb, Vector3 hitPos, Vector3 direction)
    {
        if (IsDead) return;
        int damage = 0;

        switch (limb.ThisLimbType)
        {
            case LimbType.Bottom:
                damage = 25;
                break;
            case LimbType.Upper:
                damage = 50;
                break;
            case LimbType.Head:
                damage = 100;
                break;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die(limb, direction);
            return;
        }

        // if (GetComponentInParent<CustomCharacterController>() != null)
        // {
        //     var volume = FindAnyObjectByType<Volume>();
        //     if (volume != null && volume.profile.TryGet<Vignette>(out var vignette))
        //     {
        //         vignette.color.value = new Color(currentHealth / 100, 0, 0);
        //     }
        // }
    }

    private void Die(RagDollLimb limb, Vector3 direction)
    {
        if (IsDead) return;
        IsDead = true;
        Died?.Invoke();
        ActivateRagdoll();

        Rigidbody rb = limb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direction * 5f, ForceMode.Impulse);
        }
    }

    public void DieByLaser()
    {
        if (IsDead) return;
        IsDead = true;
        Died?.Invoke();
        ActivateRagdoll();
    }

    private void DeactivateRagdoll()
    {
        anim.enabled = true;

        foreach (var limb in ragDollLimbs)
        {
            limb.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    private void ActivateRagdoll()
    {
        anim.enabled = false;

        foreach (var limb in ragDollLimbs)
        {
            limb.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
