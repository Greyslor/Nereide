using UnityEngine;

// SwimBehaviour inherits from GenericBehaviour.
public class SwimBehaviour : GenericBehaviour
{
    public KeyCode swimKey = KeyCode.Q;
    public float swimSpeed = 3.0f;
    public float sprintFactor = 1.5f;
    public float waterDrag = 3.0f;        // Resistencia del agua
    public float buoyancyForce = 1.5f;    // Flotabilidad
    public float swimMaxVerticalAngle = 60f;

    private int swimBool;
    private bool swim = false;
    private CapsuleCollider col;

    void Start()
    {
        swimBool = Animator.StringToHash("Swim");
        col = this.GetComponent<CapsuleCollider>();
        behaviourManager.SubscribeBehaviour(this);
    }

    void Update()
    {
        if (Input.GetKeyDown(swimKey) &&
                    !behaviourManager.IsOverriding() &&
            !behaviourManager.GetTempLockStatus(behaviourManager.GetDefaultBehaviour))
        {
            swim = !swim;

            behaviourManager.UnlockTempBehaviour(behaviourManager.GetDefaultBehaviour);

            behaviourManager.GetRigidBody.useGravity = !swim;

            if (swim)
            {
                // Simular resistencia del agua
                behaviourManager.GetRigidBody.linearDamping = waterDrag;
                behaviourManager.RegisterBehaviour(this.behaviourCode);
            }
            else
            {
                behaviourManager.GetRigidBody.linearDamping = 0;
                col.direction = 1;
                behaviourManager.GetCamScript.ResetTargetOffsets();
                behaviourManager.UnregisterBehaviour(this.behaviourCode);
            }
        }

        swim = swim && behaviourManager.IsCurrentBehaviour(this.behaviourCode);
        behaviourManager.GetAnim.SetBool(swimBool, swim);
    }

    public override void OnOverride()
    {
        col.direction = 1;
    }

    public override void LocalFixedUpdate()
    {
        behaviourManager.GetCamScript.SetMaxVerticalAngle(swimMaxVerticalAngle);
        SwimManagement(behaviourManager.GetH, behaviourManager.GetV);
        ApplyBuoyancy();
    }

    void SwimManagement(float horizontal, float vertical)
    {
        Vector3 direction = Rotating(horizontal, vertical);

        float finalSpeed = swimSpeed *
            (behaviourManager.IsSprinting() ? sprintFactor : 1);

        behaviourManager.GetRigidBody.AddForce(
            direction * finalSpeed * 100,
            ForceMode.Acceleration
        );
    }

    void ApplyBuoyancy()
    {
        // jugador flota suavemente
        behaviourManager.GetRigidBody.AddForce(
            Vector3.up * buoyancyForce,
            ForceMode.Acceleration
        );
    }

    Vector3 Rotating(float horizontal, float vertical)
    {
        Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward).normalized;
        Vector3 right = new Vector3(forward.z, 0, -forward.x);

        Vector3 targetDirection = forward * vertical + right * horizontal;

        if (behaviourManager.IsMoving() && targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            Quaternion newRotation = Quaternion.Slerp(
                behaviourManager.GetRigidBody.rotation,
                targetRotation,
                behaviourManager.turnSmoothing
            );

            behaviourManager.GetRigidBody.MoveRotation(newRotation);
            behaviourManager.SetLastDirection(targetDirection);
        }

        if (!(Mathf.Abs(horizontal) > 0.2 || Mathf.Abs(vertical) > 0.2))
        {
            behaviourManager.Repositioning();
            col.direction = 1;
        }
        else
        {
            col.direction = 2;
        }

        return targetDirection;
    }
}
