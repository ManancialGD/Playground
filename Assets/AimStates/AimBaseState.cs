using UnityEngine;
using UnityEngine.Rendering;

public abstract class AimBaseState
{
    public abstract void EnterState(AimStateManager aim);
    public abstract void UpdateState(AimStateManager aim);

}
