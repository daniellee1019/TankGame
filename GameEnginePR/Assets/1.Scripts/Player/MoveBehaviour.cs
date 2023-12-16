using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 이동과 점프 동작을 담당하는 컴포넌트
/// 충돌처리에 대한 기능도 포함
/// 기본 동작으로써 작동.
/// </summary>
public class MoveBehaviour : GenericBehaviour
{
    public float walkSpeed = 0.15f;
    public float runSpeed = 1.0f;
    public float sprintSpeed = 2.0f;
    public float speedDampTime = 0.1f;

    public float jumpHeight = 1.5f;
    public float jumpInertialForce = 10f; //점프 관성
    public float speed, speedSeeker;

    private int jumpBool;
    private int groundedBool;
    private bool jump;
    private bool isColliding;
    private CapsuleCollider capsuleCollider;
    private Transform myTransform;

    private void Start()
    {
        myTransform = transform;
        capsuleCollider = GetComponent<CapsuleCollider>();
        jumpBool = Animator.StringToHash(FC.AnimatorKey.Jump);
        groundedBool = Animator.StringToHash(FC.AnimatorKey.Grounded);
        behaviourController.GetAnimator.SetBool(groundedBool, true);

        //
        behaviourController.SubScribeBehavior(this);
        behaviourController.RegisterDefaultBehaviour(this.behaviorCode);
        speedSeeker = runSpeed;
    }
    Vector3 Rotating(float horizontal, float vertical)
    {
        Vector3 forward = behaviourController.playerCamera.TransformDirection(Vector3.forward);

        forward.y = 0.0f;
        forward = forward.normalized;

        Vector3 right = new Vector3(forward.z, 0.0f, -forward.x);
        Vector3 targetDirection = Vector3.zero;
        targetDirection = forward * vertical + right * horizontal;

        //보간을 통해 움직임을 자연스럽게 해줌
        if(behaviourController.IsMoving() && targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            Quaternion newRotation = Quaternion.Slerp(behaviourController.GetRigidbody.rotation, targetRotation,
                behaviourController.turnSmoothing);
            behaviourController.GetRigidbody.MoveRotation(newRotation);
            behaviourController.SetLastDirection(targetDirection);

        }
        //repositioning -> why? 하늘을 바라볼 수 있기 때문에.
        if(!(Mathf.Abs(horizontal) > 0.9f || Mathf.Abs(vertical) > 0.9f))
        {
            behaviourController.Repositioning();
        }
        return targetDirection;
    }
    /*
     y에 대한 속도를 없애 물체를 안정화 시킨다
     */
    private void RemoveVerticalVelocity()
    {
        Vector3 horizentalVelocity = behaviourController.GetRigidbody.velocity;
        horizentalVelocity.y = 0.0f;
        behaviourController.GetRigidbody.velocity = horizentalVelocity;
    }

    void MovementManagement(float horizontal, float vertical)
    {
        if (behaviourController.IsGrounded())
        {
            behaviourController.GetRigidbody.useGravity = true;
        }else if(!behaviourController.GetAnimator.GetBool(jumpBool) && behaviourController.GetRigidbody.velocity.y > 0)
        {
            RemoveVerticalVelocity();
        } // 점프 중이 아님에도 불구하고 y값이 0보다 크다면 어떤 물체에 껴있는 경우.
        Rotating(horizontal, vertical);
        Vector2 dir = new Vector2(horizontal, vertical);
        speed = Vector2.ClampMagnitude(dir, 1f).magnitude;
        speedSeeker += Input.GetAxis("Mouse ScrollWheel");
        speedSeeker = Mathf.Clamp(speedSeeker, walkSpeed, runSpeed);
        speed *= speedSeeker;
        if (behaviourController.IsSprinting())
        {
            speed = sprintSpeed;
        }
        // 애니메이션 speed 파라미터 조정. 캐릭터의 각 모션 속도 값. ex) 수그리기, 앉기, 뛰기 등
        behaviourController.GetAnimator.SetFloat(speedFloat, speed, speedDampTime, Time.deltaTime);
    }

    private void OnCollisionStay(Collision collision) // 충돌중
    {
        isColliding = true; // 웅크린상태
        if(behaviourController.IsCurrentBehaviour(GetBehaviorCode) && collision.GetContact(0).normal.y <= 0.1f)
        {
            float vel = behaviourController.GetAnimator.velocity.magnitude;
            Vector3 targentMove = Vector3.ProjectOnPlane(myTransform.forward, collision.GetContact(0).normal).normalized * vel;
            behaviourController.GetRigidbody.AddForce(targentMove, ForceMode.VelocityChange); // 부딪히면 강제로 미끄러지게 함.
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        isColliding = false;
    }
    /*
     * 점프 3가지 요소
     * 1. 점프 시작
     * 2. 점프 중 이동
     * 3. 착지
     */
    void JumpManagement()
    {
        if(jump && !behaviourController.GetAnimator.GetBool(jumpBool) && behaviourController.IsGrounded())
        {
            behaviourController.LockTempBehaviour(behaviorCode); // 점프중에는 이동할 수 없게 잠굼.
            behaviourController.GetAnimator.SetBool(jumpBool, true);
            if(behaviourController.GetAnimator.GetFloat(speedFloat) > 0.1f)
            {
                capsuleCollider.material.dynamicFriction = 0f; // 원활히 장애물을 넘기 위해 마찰력을 줄여준다.
                capsuleCollider.material.staticFriction = 0f;
                RemoveVerticalVelocity();
                float velocity = 2f * Mathf.Abs(Physics.gravity.y) * jumpHeight;
                velocity = Mathf.Sqrt(velocity);
                behaviourController.GetRigidbody.AddForce(Vector3.up * velocity, ForceMode.VelocityChange);
            }

        }else if (behaviourController.GetAnimator.GetBool(jumpBool))
        {
            if(!behaviourController.IsGrounded() && !isColliding && behaviourController.GetTempLockStatus())
            {
                behaviourController.GetRigidbody.AddForce(myTransform.forward * jumpInertialForce * Physics.gravity.magnitude *
                    sprintSpeed, ForceMode.Acceleration);
            }
            if(behaviourController.GetRigidbody.velocity.y < 0f && behaviourController.IsGrounded())
            {
                behaviourController.GetAnimator.SetBool(groundedBool, true);
                capsuleCollider.material.dynamicFriction = 0.6f;
                capsuleCollider.material.staticFriction = 0.6f;
                jump = false;
                behaviourController.GetAnimator.SetBool(jumpBool, false);
                behaviourController.UnLockTempBehaviour(this.behaviorCode);
            }
        }
    }


    //점프키를 오버라이딩 받았는지 update 해준다.
    private void Update()
    {
        if (!jump && Input.GetButtonDown(ButtonName.Jump) && behaviourController.IsCurrentBehaviour(this.behaviorCode) &&
            !behaviourController.IsOverriding())
        {
            jump = true;
        }
    }
    public override void LocalFixedUpdate()
    {
        MovementManagement(behaviourController.GetH, behaviourController.GetV);
        JumpManagement();
    }
}
