using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// 현재 동작, 기본 동작, 오버라이딩 동작, 잠긴 동작, 마우스 이동값
/// 땅에 서있는지, GenericBehavior를 상속받은 동작들을 업데이트 시켜줌.
/// </summary>
/// 
public class BehaviorController : MonoBehaviour
{
    private List<GenericBehaviour> behaviours; // 동작들
    private List<GenericBehaviour> overrideBehaviours; // 우선시 되는 동작
    private int currentBehaviour; // 현재 동작 해시코드
    private int defalutBehaviour; // 기본 동작 해시코드
    private int behaviorLocked; // 잠긴 동작 해시코드

    // 캐싱.
    public Transform playerCamera;
    private Animator myAnimator;
    private Rigidbody myRigidbody;
    private ThirdPersonOrbitCam camSprint;
    private ThirdTankOrbitCam camSprintT;
    private Transform myTransform;

    // 기본 속성 값들.
    private float h; // horizontal axis
    private float v; // vertical axis
    public float turnSmoothing = 0.06f; // 카메라를 향하도록 움직일 때 회전속도.
    private bool changedFOV; // 달리기 동작이 카메라 시야각이 변경되었을 때 저장됐는지.
    public float sprintFOV = 100; // 달리기 시야각.
    private Vector3 lastDirection; // 마지막 향했던 방향.
    private bool isSprint;
    private int hFloat; // 애니메이터 관련 가로축 값
    private int vFloat; // 애니메이터 관련 세로축 값
    private int groundedBool; // 애니메이터 지상 판별
    private Vector3 colExtents; // 땅과의 출동체크를 위한 충돌체 영역. (확장영역)

    public float GetH { get => h; }
    public float GetV { get => v; }
    public ThirdPersonOrbitCam GetCamScript { get => camSprint; }
    public Rigidbody GetRigidbody { get => myRigidbody; }
    public Animator GetAnimator { get => myAnimator; }
    public int GetDefaultBehaviour { get => defalutBehaviour; }

    private void Awake()
    {
        behaviours = new List<GenericBehaviour>();
        overrideBehaviours = new List<GenericBehaviour>();
        myAnimator = GetComponent<Animator>();
        hFloat = Animator.StringToHash(FC.AnimatorKey.Horizontal);
        vFloat = Animator.StringToHash(FC.AnimatorKey.Vertical);
        camSprint = playerCamera.GetComponent<ThirdPersonOrbitCam>();
        myRigidbody = GetComponent<Rigidbody>();
        myTransform = transform;
        // isGround
        groundedBool = Animator.StringToHash(FC.AnimatorKey.Grounded);
        colExtents = GetComponent<Collider>().bounds.extents;
    }
    public bool IsMoving()
    {
        return Mathf.Abs(h) > Mathf.Epsilon || Mathf.Abs(v) > Mathf.Epsilon; //실수가 갖을 수 있는 가장 최소한의 값을 의미
    }
    public bool IsHorizontalMoving()
    {
        return Mathf.Abs(h) > Mathf.Epsilon || Mathf.Abs(v) > Mathf.Epsilon;
    }
     public bool CanSprint()
    {
        foreach(GenericBehaviour behaviour in behaviours)
        {
            if (!behaviour.AllowSprint)
            {
                return false;
            }
        }
        foreach(GenericBehaviour genericBehaviour in overrideBehaviours)
        {
            if (!genericBehaviour.AllowSprint)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsSprinting()
    {
        return isSprint && IsMoving() && CanSprint();
    }

    public bool IsGrounded()
    {
        Ray ray = new Ray(myTransform.position + Vector3.up * 2 * colExtents.x, Vector3.down);
        return Physics.SphereCast(ray, colExtents.x, colExtents.x + 0.2f);
    }

    private void Update()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        myAnimator.SetFloat(hFloat, h, 0.1f, Time.deltaTime);
        myAnimator.SetFloat(vFloat, v, 0.1f, Time.deltaTime);

        isSprint = Input.GetButton(ButtonName.Sprint);
        if ((IsSprinting()))
        {
            changedFOV = true;
            camSprint.SetFOV(sprintFOV);
        }
        else if (changedFOV)
        {
            camSprint.ResetFOV();
            changedFOV = false;
        }

        myAnimator.SetBool(groundedBool, IsGrounded()); // 땅과 출동 판별 - 땅 위에 있는지 없는지

    }
    /// <summary>
    /// rigidbody의 영향으로 물체와 출동 시 여러가지 상황을 대비하여  position을 재설정 및 보정 해줌
    /// </summary>
    public void Repositioning() 
    {
        if(lastDirection != Vector3.zero)
        {
            lastDirection.y = 0f;
            Quaternion targetRotation = Quaternion.LookRotation(lastDirection);
            Quaternion newRotation = Quaternion.Slerp(myRigidbody.rotation, targetRotation, turnSmoothing);
            myRigidbody.MoveRotation(newRotation);
        }
    }

    private void FixedUpdate()
    {
        Profiler.BeginSample("FixedUpdate_BehaviourController");
        bool isAnyBehaviorActive = false;
        if(behaviorLocked > 0 || overrideBehaviours.Count == 0)
        {
            foreach(GenericBehaviour behaviour in behaviours)
            {
                if(behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviorCode)
                {
                    isAnyBehaviorActive = true;
                    behaviour.LocalFixedUpdate();
                }
            }
        }
        else
        {
            foreach(GenericBehaviour behaviour in overrideBehaviours)
            {
                behaviour.LocalFixedUpdate();
            }
        }
        if(!isAnyBehaviorActive && overrideBehaviours.Count == 0)
        {
            myRigidbody.useGravity = true;
            Repositioning();
        }
        Profiler.EndSample();
    }
    private void LateUpdate() // 카메라이동 업데이트
    {
        if(behaviorLocked > 0 || overrideBehaviours.Count == 0)
        {
            foreach(GenericBehaviour behaviour in behaviours)
            {
                if(behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviorCode)
                {
                    behaviour.LocalLateUpdate();
                }
            }
        }
        else
        {
            foreach(GenericBehaviour behaviour in overrideBehaviours)
            {
                behaviour.LocalLateUpdate();
            }
        }
    }

    public void SubScribeBehavior(GenericBehaviour behaviour)
    {
        behaviours.Add(behaviour);
    }
    public void RegisterDefaultBehaviour(int behaviourCode)
    {
        defalutBehaviour = behaviourCode;
        currentBehaviour = behaviourCode;
    }
    public void RegisterBehaviour(int behaviourCode)
    {
        if(currentBehaviour == defalutBehaviour)
        {
            currentBehaviour = behaviourCode;
        }
    }
    public void UnRegisterBehaviour(int behaviourCode)
    {
        if(currentBehaviour == behaviourCode)
        {
            currentBehaviour = defalutBehaviour;
        }
    }
    public bool OverrideWithBehaviour(GenericBehaviour behaviour)
    {
        if (!overrideBehaviours.Contains(behaviour))
        {
            if(overrideBehaviours.Count == 0)
            {
                foreach(GenericBehaviour behaviour1 in behaviours)
                {
                    if(behaviour1.isActiveAndEnabled && currentBehaviour == behaviour1.GetBehaviorCode)
                    {
                        behaviour1.OnOverride();
                        break;
                    }
                }
            }
            overrideBehaviours.Add(behaviour);
            return true;
        }
        return false;
    }
    public bool RevokeOverridingBehaviour(GenericBehaviour behaviour)
    {
        if (overrideBehaviours.Contains(behaviour))
        {
            overrideBehaviours.Remove(behaviour);
            return true;
        }
        return false;
    }
    public bool IsOverriding(GenericBehaviour behaviour = null)
    {
        if(behaviour == null)
        {
            return overrideBehaviours.Count > 0;
        }
        return overrideBehaviours.Contains(behaviour);
    }
    public bool IsCurrentBehaviour(int behaviourCode)
    {
        return this.currentBehaviour == behaviourCode;
    }
    public bool GetTempLockStatus(int behaviourCode = 0)
    {
        return (behaviorLocked != 0 && behaviorLocked != behaviourCode);
    }
    public void LockTempBehaviour(int behaviourCode)
    {
        if(behaviorLocked == 0)
        {
            behaviorLocked = behaviourCode;
        }
    }
    public void UnLockTempBehaviour(int behaviourCode)
    {
        if(behaviorLocked == behaviourCode)
        {
            behaviorLocked = 0;
        }
    }
    public Vector3 GetLastDirection()
    {
        return lastDirection;
    }
    public void SetLastDirection(Vector3 direction)
    {
        lastDirection = direction;
    }
}

public abstract class GenericBehaviour : MonoBehaviour
{
    protected int speedFloat;
    protected BehaviorController behaviourController;
    protected int behaviorCode;
    protected bool canSprint;

    private void Awake()
    { 
        this.behaviourController = GetComponent<BehaviorController>();
        speedFloat = Animator.StringToHash(FC.AnimatorKey.Speed);
        canSprint = true;

        // 동작 타입을 해시코드로 갖고 있다가, 추후에 구별용으로 사용.
        behaviorCode = this.GetType().GetHashCode();
    }

    public int GetBehaviorCode
    {
        get => behaviorCode;
    }
    public bool AllowSprint
    {
        get => canSprint;
    }
    public virtual void LocalLateUpdate()
    {

    }
    public virtual void LocalFixedUpdate()
    {

    }
    public virtual void OnOverride()
    {

    }

}

