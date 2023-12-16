using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using FC;

/// <summary>
/// 다른 Behaviour 보다 상위에 있다. -> 조준이 먼저.
/// 마우스 오른쪽 버튼으로 조준 및 휠버튼으로 좌우 카메라 변경
/// 벽의 모서리에서 조준할 때 상체를 살짝 기울여주는 기능.
/// </summary>
public class AimBehaviour : GenericBehaviour
{
    public Texture2D crossHair; // 십자선 이미지.
    public float aimTurnSmoothing = 0.15f; // 카메라를 향하도록 조준할 때 회전속도.
    public Vector3 aimPivotOffSet = new Vector3(0.5f, 1.2f, 0.0f);
    public Vector3 aimCamOffSet = new Vector3(0.0f, 0.4f, -0.7f);

    private int aimBool; // 애니메이터 파라미터. 조준.
    private bool aim; // 조준중?
    private int cornerBool; // 애니메이터 관련. 코너. -> 조준
    private bool peekConer; // 플레이어가 코너 모서리에 있는지 여부.
    private Vector3 initialRootRotation; // 루트 본으로부터 로컬까지 회전값
    private Vector3 initialHipRotation;
    private Vector3 initialSpineRotation;
    private Transform myTransform;

    private void Start()
    {
        myTransform = transform;
        //setup
        aimBool = Animator.StringToHash(AnimatorKey.Aim);
        cornerBool = Animator.StringToHash(AnimatorKey.Corner);

        //value
        Transform hips = behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Hips);
        initialRootRotation = (hips.parent == transform) ? Vector3.zero : hips.parent.localEulerAngles;
        initialHipRotation = hips.localEulerAngles;
        initialSpineRotation = behaviourController.GetAnimator.
            GetBoneTransform(HumanBodyBones.Spine).localEulerAngles;

    }
    // 카메라에 따라 플레이어를 올바른 방향으로 회전.
    void Rotating()
    {
        Vector3 forward = behaviourController.playerCamera.TransformDirection(Vector3.forward);
        forward.y = 0.0f;
        forward = forward.normalized;

        Quaternion targetRotation = Quaternion.Euler(0f, behaviourController.GetCamScript.GetH, 0.0f); // 좌우는 회전을 먼저 적용.
        float minSpeed = Quaternion.Angle(myTransform.rotation, targetRotation) * aimTurnSmoothing;

        if (peekConer)
        {
            //조준 중일때 플레이어 상체만 살짝 기울여 주기 위함.
            myTransform.rotation = Quaternion.LookRotation(-behaviourController.GetLastDirection()); // IK(역운동학) 적용. 쉽게 말하면 물체를 잡을 때 그에 맞춰 회전값을 적용할 수 있게 포지션을 역 방향으로 작업하는 것.
            targetRotation *= Quaternion.Euler(initialRootRotation);
            targetRotation *= Quaternion.Euler(initialHipRotation);
            targetRotation *= Quaternion.Euler(initialSpineRotation);
            Transform spine = behaviourController.GetAnimator.GetBoneTransform(HumanBodyBones.Spine);
            spine.rotation = targetRotation;
        }
        else
        {
            behaviourController.SetLastDirection(forward);
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, minSpeed * Time.deltaTime);
        }
    }
    //조준 중일때 관리하는 함수.
    void AimManagement()
    {
        Rotating();
    }
    private IEnumerator ToggleAimOn()
    {
        yield return new WaitForSeconds(0.05f);
        //조준이 불가능한 상태일때에 대한 예외처리.
        if(behaviourController.GetTempLockStatus(this.behaviorCode) || behaviourController.IsOverriding(this))
        {
            yield return false;
        }
        else
        {
            aim = true;
            int signal = 1;
            if (peekConer)
            {
                signal = (int)Mathf.Sign(behaviourController.GetH);
            }
            aimCamOffSet.x = Mathf.Abs(aimCamOffSet.x) * signal; // 기울일 때 값 보정.
            aimPivotOffSet.x = Mathf.Abs(aimPivotOffSet.x) * signal;
            yield return new WaitForSeconds(0.1f);
            behaviourController.GetAnimator.SetFloat(speedFloat, 0.0f); //조준 중에는 뛰기 x
            behaviourController.OverrideWithBehaviour(this);
        }
    }

    private IEnumerator ToggleAimOff()
    {
        aim = false;
        yield return new WaitForSeconds(0.3f);
        behaviourController.GetCamScript.ResetTargetOffsets();
        behaviourController.GetCamScript.ResetMaxVerticalAngle();
        yield return new WaitForSeconds(0.1f);
        behaviourController.RevokeOverridingBehaviour(this);
    }
    public override void LocalFixedUpdate()
    {
        if (aim)
        {
            behaviourController.GetCamScript.SetTargetOffset(aimPivotOffSet, aimCamOffSet); // 조준 전 카메라 위치에서 조준 후 카메라 위치 변경.
        }
    }
    public override void LocalLateUpdate()
    {
        AimManagement();
    }

    private void Update()
    {
        peekConer = behaviourController.GetAnimator.GetBool(cornerBool);

        if(Input.GetAxisRaw(ButtonName.Aim) != 0 && !aim)
        {
            StartCoroutine(ToggleAimOn());
        }else if(aim && Input.GetAxisRaw(ButtonName.Aim) == 0)
        {
            StartCoroutine(ToggleAimOff());
        }
        // 조준 중 달리기 x
        canSprint = !aim;
        if(aim && Input.GetButtonDown(ButtonName.Shoulder) && !peekConer)
        {
            aimCamOffSet.x = aimCamOffSet.x * (-1);
            aimPivotOffSet.x = aimPivotOffSet.x * (-1); // 조준 시 좌우 기울임 바뀜
        }
        behaviourController.GetAnimator.SetBool(aimBool, aim);
    }
    private void OnGUI()
    {
        if(crossHair != null)
        {
            float length = behaviourController.GetCamScript.GetCurrentPivotMagnitude(aimPivotOffSet);
            if(length < 0.05f) // 조준이 완료 됐다면.
            {
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - (crossHair.width * 0.5f), Screen.height * 0.5f - (crossHair.height * 0.5f),
                    crossHair.width, crossHair.height), crossHair); // x, y, w, h, texture
            }
        }
    }
}
