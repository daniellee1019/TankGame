using NPOI.SS.Formula.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 카메라 속성중 중요 속성 하나는 카메라로부터 위치 오프셋 벡터, 피봇 오프셋 벡터
// 위치 오프셋 벡터는 충돌 처리용으로 사용하고 피봇 오프셋 벡터는 시선이동에 사용하도록
// 충돌체크 : 이중 충돌 체크 가능 ( 캐릭터로부터 카메라, 카메라로부터 캐릭터사이 )
// 사격 반동을 위한 기능
// FOV - 시야각 변경 기능

[RequireComponent(typeof(Camera))] // 카메라 컴포넌트가 누락되는 것을 방지하기 위한 코드. -> 스크립트를 추가하면 자동으로 Camera 컴포넌트 반영.
public class ThirdPersonOrbitCam : MonoBehaviour
{
    public Transform player;
    public Vector3 pivotOffset = new Vector3(0.0f, 1.0f, 0.0f);
    public Vector3 camOffset = new Vector3(0.4f, 0.5f, -2.0f);

    public float smooth = 10f;  // 카메라 반응속도
    public float horizontalAimingSpeed = 6.0f; // 수평 회전 속도.
    public float verticalAimingSpeed = 6.0f; // 수직 회전 속도.
    public float maxVerticalAngle = 30.0f; // 카메라 수직 최대 각도
    public float minverticalAngle = -60.0f; // 카메라 수직 최소 각도
    public float recoilAngleBounce = 5.0f; // 사격 반동 값
    private float angleH = 0.0f; // 마우스 이동에 따른 카메라 수평이동 수치.
    private float angleV = 0.0f; // 마우스 이동에 따른 카메라 수직이동 수치.
    private Transform cameraTransform; // 트랜스폼 캐싱.
    private Camera myCamera;
    private Vector3 relCameraPos; // 플레이어로부터 카메라까지의 벡터.
    private float relCameraPosMag; // 플레이어로부터 카메라사이 거리.
    private Vector3 smoothPivotOffset; // 카메라 피봇 보간용 벡터. 
    private Vector3 smoothCamOffset; // 카메라 위치 보간용 벡터. 
    private Vector3 targetPivotOffset; // 카메라 피봇 보간용 벡터. 
    private Vector3 targetCamOffset; // 카메라 위치 보간용 벡터.
    private float defaultFOV; // 기본 시야값.
    private float targetFOV; // 타겟 시야값.
    private float targetMaxVerticleAngle; // 카메라 수직 최대 각도. -> 사격시 반동
    private float recoilAngle = 0f; // 사격 반동 각도

    /// <summary>
    /// private 프로퍼티 추가 get,set
    /// </summary>
    public float GetH
    {
        //get => angleH;
        get
        {
            return angleH;
        }
    }

    private void Awake()
    {
        //캐싱
        cameraTransform = transform;
        myCamera = cameraTransform.GetComponent<Camera>();

        // 카메라 기본 포지션 세팅
        cameraTransform.position = player.position + Quaternion.identity * pivotOffset + Quaternion.identity * camOffset;
        cameraTransform.rotation = Quaternion.identity;

        // 카메라와 플레이어간의 상대 벡터. -> 충돌체크에 사용함
        relCameraPos = cameraTransform.position - player.position;
        relCameraPosMag = relCameraPos.magnitude - 0.5f; // 플레이어를 뺀 거리.

        // 기본세팅
        smoothPivotOffset = pivotOffset;
        smoothCamOffset = camOffset;
        defaultFOV = myCamera.fieldOfView;
        angleH = player.eulerAngles.y; // 초기 y값.

        ResetTargetOffsets();
        ResetFOV();
        ResetMaxVerticalAngle();


    }
    //초기화 함수
    public void ResetTargetOffsets()
    {
        targetPivotOffset = pivotOffset;
        targetCamOffset = camOffset;
    }
    public void ResetFOV()
    {
        this.targetFOV = defaultFOV;
    }
    public void ResetMaxVerticalAngle()
    {
        targetMaxVerticleAngle = maxVerticalAngle;
    }

    // 기본 세팅 조절 함수
    public void BounceVertical(float degree)
    {
        recoilAngle = degree;
    }
    public void SetTargetOffset(Vector3 newPivotOffset, Vector3 newCamOffset)
    {
        targetPivotOffset = newPivotOffset;
        targetCamOffset = newCamOffset;
    }
    public void SetFOV(float customFOV)
    {
        this.targetFOV = customFOV;  
    }

    // 충돌체크 함수
    bool ViewingPosCheck(Vector3 checkPos, float deltaPlayerHeight) // playerPos는 발밑이므로 플레이어의 높이를 체크해서 그 앞으로 충돌체크를 이어나감.
    {
        Vector3 target = player.position + (Vector3.up * deltaPlayerHeight);
        // Raycast : 충돌하는 collider에 대한 거리, 위치등 자세한 정보를 RaycastHit로 반환.
        // 또한, LayerMask 필터링을 해서 원하는 layer에 충돌을 하면 true 값 반환.
        // 추가로, OverlapSphere : 중점과 반지름으로 가상의 원을 만들어 추출하려는 반경 이내에 들어와 있는 collider를 반환하는 함수.
        // 함수의 반환 값은 collider 컴포넌트 배열로 넘어온다.
        if (Physics.SphereCast(checkPos, 0.2f, target - checkPos, out RaycastHit hit, relCameraPosMag))
        {
            if(hit.transform != player && !hit.transform.GetComponent<Collider>().isTrigger) // isTrigger을 false로 두는 이유는 이벤트나 연출을 위한 Collider는 제외 시켜야 되기때문.
            {
                return false;
            }
        }
        return true;
    }
    bool ReverseViewingPosCheck(Vector3 checkPos, float deltaPlayerHeight, float maxDistance)
    {
        Vector3 origin = player.position + (Vector3.up * deltaPlayerHeight);
        if(Physics.SphereCast(origin, 0.2f, checkPos - origin, out RaycastHit hit, maxDistance))
        {
            if(hit.transform != player && hit.transform != transform && !hit.transform.GetComponent<Collider>().isTrigger)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 서로 체크를 통해서 true와 false 반환 true -> 충돌X false -> 둘 중에 하나는 충돌을 했다.
    /// </summary>
    bool DoubleViewingPosCheck(Vector3 checkPos, float offset)
    {
        float playerFocusHeight = player.GetComponent<CapsuleCollider>().height * 0.75f;
        return ViewingPosCheck(checkPos, playerFocusHeight) && ReverseViewingPosCheck(checkPos, playerFocusHeight, offset);
    }

    private void Update()
    {
        // 마우스 이동 값.
        angleH += Mathf.Clamp(Input.GetAxis("Mouse X"), -1f, 1f) * horizontalAimingSpeed;
        angleV += Mathf.Clamp(Input.GetAxis("Mouse Y"), -1f, 1f) * verticalAimingSpeed;
        // 수직 이동 제한.
        angleV = Mathf.Clamp(angleV, minverticalAngle, targetMaxVerticleAngle);
        // 수직 카메라 바운스.
        angleV = Mathf.LerpAngle(angleV, angleV + recoilAngle, 10f * Time.deltaTime); // 각도 보간.

        // 카메라 회전.
        Quaternion camYRotation = Quaternion.Euler(0.0f, angleH, 0.0f);
        Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0.0f);
        cameraTransform.rotation = aimRotation;

        //set FOV
        myCamera.fieldOfView = Mathf.Lerp(myCamera.fieldOfView, targetFOV, Time.deltaTime);

        Vector3 baseTempPos = player.position + camYRotation * targetPivotOffset; // 기본 포지션 값.
        Vector3 noCollisionOffset = targetCamOffset; // 조준할 때 카메라의 오프셋 값, 조준할 때와 하지 않을 때 값과 다르다.

        for(float zOffset = targetCamOffset.z; zOffset <= 0f; zOffset += 0.5f)
        {
            noCollisionOffset.z = zOffset;
            if(DoubleViewingPosCheck(baseTempPos + aimRotation * noCollisionOffset, Mathf.Abs(zOffset)) || zOffset == 0f)
            {
                break;
            }
        }
        // Reposition Camera
        smoothPivotOffset = Vector3.Lerp(smoothPivotOffset, targetPivotOffset, smooth * Time.deltaTime);
        smoothCamOffset = Vector3.Lerp(smoothCamOffset, noCollisionOffset, smooth * Time.deltaTime);

        cameraTransform.position = player.position + camYRotation * smoothPivotOffset + aimRotation * smoothCamOffset;

        // 반동 만들어 주기.
        if(recoilAngle > 0.0f)
        {
            recoilAngle -= recoilAngleBounce * Time.deltaTime;
        }
       else if(recoilAngle < 0.0f)
        {
            recoilAngle += recoilAngleBounce * Time.deltaTime;
        }
    }

    /// <summary>
    /// AimBehavior 관련 조절 함수.
    /// </summary>
    public float GetCurrentPivotMagnitude(Vector3 finalPivotOffset)
    {
        return Mathf.Abs((finalPivotOffset - smoothPivotOffset).magnitude);
    }

}
