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
public class ThirdTankOrbitCam : MonoBehaviour
{
    public Transform Tank;
    public Vector3 pivotOffsetT = new Vector3(0.0f, 1.0f, 0.0f);
    public Vector3 camOffsetT = new Vector3(0.4f, 0.5f, -2.0f);

    public float smoothT = 10f;  // 카메라 반응속도
    public float horizontalAimingSpeedT = 6.0f; // 수평 회전 속도.
    public float verticalAimingSpeedT = 6.0f; // 수직 회전 속도.
    public float maxVerticalAngleT = 30.0f; // 카메라 수직 최대 각도
    public float minverticalAngleT = -60.0f; // 카메라 수직 최소 각도
    public float recoilAngleBounceT = 5.0f; // 사격 반동 값
    private float angleTH = 0.0f; // 마우스 이동에 따른 카메라 수평이동 수치.
    private float angleTV = 0.0f; // 마우스 이동에 따른 카메라 수직이동 수치.
    private Transform cameraTransformT; // 트랜스폼 캐싱.
    private Camera myCameraT;
    private Vector3 relCameraPosT; // 플레이어로부터 카메라까지의 벡터.
    private float relCameraPosMagT; // 플레이어로부터 카메라사이 거리.
    private Vector3 smoothPivotOffsetT; // 카메라 피봇 보간용 벡터. 
    private Vector3 smoothCamOffsetT; // 카메라 위치 보간용 벡터. 
    private Vector3 targetPivotOffsetT; // 카메라 피봇 보간용 벡터. 
    private Vector3 targetCamOffsetT; // 카메라 위치 보간용 벡터.
    private float defaultFOVT; // 기본 시야값.
    private float targetFOVT; // 타겟 시야값.
    private float targetMaxVerticleAngleT; // 카메라 수직 최대 각도. -> 사격시 반동
    private float recoilAngleT = 0f; // 사격 반동 각도

    /// <summary>
    /// private 프로퍼티 추가 get,set
    /// </summary>
    public float GetH
    {
        //get => angleH;
        get
        {
            return angleTH;
        }
    }

    private void Awake()
    {
        //캐싱
        cameraTransformT = transform;
        myCameraT = cameraTransformT.GetComponent<Camera>();

        // 카메라 기본 포지션 세팅
        cameraTransformT.position = Tank.position + Quaternion.identity * pivotOffsetT + Quaternion.identity * camOffsetT;
        cameraTransformT.rotation = Quaternion.identity;

        // 카메라와 플레이어간의 상대 벡터. -> 충돌체크에 사용함
        relCameraPosT = cameraTransformT.position - Tank.position;
        relCameraPosMagT = relCameraPosT.magnitude - 0.5f; // 플레이어를 뺀 거리.

        // 기본세팅
        smoothPivotOffsetT = pivotOffsetT;
        smoothCamOffsetT = camOffsetT;
        defaultFOVT = myCameraT.fieldOfView;
        angleTH = Tank.eulerAngles.y; // 초기 y값.

        ResetTankOffsets();
        ResetTankFOV();
        ResetMaxVerticalTankAngle();
    }

    //초기화 함수
    public void ResetTankOffsets()
    {
        targetPivotOffsetT = pivotOffsetT;
        targetCamOffsetT = camOffsetT;
    }
    public void ResetTankFOV()
    {
        this.targetFOVT = defaultFOVT;
    }
    public void ResetMaxVerticalTankAngle()
    {
        targetMaxVerticleAngleT = maxVerticalAngleT;
    }

    // 기본 세팅 조절 함수
    public void BounceTankVertical(float degree)
    {
        recoilAngleT = degree;
    }
    public void SetTankOffset(Vector3 newPivotOffset, Vector3 newCamOffset)
    {
        targetPivotOffsetT = newPivotOffset;
        targetCamOffsetT = newCamOffset;
    }
    public void SetTankFOV(float customFOV)
    {
        this.targetFOVT = customFOV;
    }

    // 충돌체크 함수
    bool ViewingTankPosCheck(Vector3 checkPos, float deltaPlayerHeight) // playerPos는 발밑이므로 플레이어의 높이를 체크해서 그 앞으로 충돌체크를 이어나감.
    {
        Vector3 target = Tank.position + (Vector3.up * deltaPlayerHeight);
        // Raycast : 충돌하는 collider에 대한 거리, 위치등 자세한 정보를 RaycastHit로 반환.
        // 또한, LayerMask 필터링을 해서 원하는 layer에 충돌을 하면 true 값 반환.
        // 추가로, OverlapSphere : 중점과 반지름으로 가상의 원을 만들어 추출하려는 반경 이내에 들어와 있는 collider를 반환하는 함수.
        // 함수의 반환 값은 collider 컴포넌트 배열로 넘어온다.
        if (Physics.SphereCast(checkPos, 0.2f, target - checkPos, out RaycastHit hit, relCameraPosMagT))
        {
            if (hit.transform != Tank && !hit.transform.GetComponent<Collider>().isTrigger) // isTrigger을 false로 두는 이유는 이벤트나 연출을 위한 Collider는 제외 시켜야 되기때문.
            {
                return false;
            }
        }
        return true;
    }

    bool ReverseViewingTankPosCheck(Vector3 checkPos, float deltaPlayerHeight, float maxDistance)
    {
        Vector3 origin = Tank.position + (Vector3.up * deltaPlayerHeight);
        if (Physics.SphereCast(origin, 0.2f, checkPos - origin, out RaycastHit hit, maxDistance))
        {
            if (hit.transform != Tank && hit.transform != transform && !hit.transform.GetComponent<Collider>().isTrigger)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 서로 체크를 통해서 true와 false 반환 true -> 충돌X false -> 둘 중에 하나는 충돌을 했다.
    /// </summary>
    bool DoubleViewingTankPosCheck(Vector3 checkPos, float offset)
    {
        float playerFocusHeight = Tank.GetComponent<CapsuleCollider>().height * 0.75f;
        return ViewingTankPosCheck(checkPos, playerFocusHeight) && ReverseViewingTankPosCheck(checkPos, playerFocusHeight, offset);
    }

    private void Update()
    {
        // 마우스 이동 값.
        angleTH += Mathf.Clamp(Input.GetAxis("Mouse X"), -1f, 1f) * horizontalAimingSpeedT;
        angleTV += Mathf.Clamp(Input.GetAxis("Mouse Y"), -1f, 1f) * verticalAimingSpeedT;
        // 수직 이동 제한.
        angleTV = Mathf.Clamp(angleTV, minverticalAngleT, targetMaxVerticleAngleT);
        // 수직 카메라 바운스.
        angleTV = Mathf.LerpAngle(angleTV, angleTV + recoilAngleT, 10f * Time.deltaTime); // 각도 보간.

        // 카메라 회전.
        Quaternion camYRotation = Quaternion.Euler(0.0f, angleTH, 0.0f);
        Quaternion aimRotation = Quaternion.Euler(-angleTV, angleTH, 0.0f);
        cameraTransformT.rotation = aimRotation;

        //set FOV
        myCameraT.fieldOfView = Mathf.Lerp(myCameraT.fieldOfView, targetFOVT, Time.deltaTime);

        Vector3 baseTempPos = Tank.position + camYRotation * targetPivotOffsetT; // 기본 포지션 값.
        Vector3 noCollisionOffset = targetCamOffsetT; // 조준할 때 카메라의 오프셋 값, 조준할 때와 하지 않을 때 값과 다르다.

        for (float zOffset = targetCamOffsetT.z; zOffset <= 0f; zOffset += 0.5f)
        {
            noCollisionOffset.z = zOffset;
            if (DoubleViewingTankPosCheck(baseTempPos + aimRotation * noCollisionOffset, Mathf.Abs(zOffset)) || zOffset == 0f)
            {
                break;
            }
        }
        // Reposition Camera
        smoothPivotOffsetT = Vector3.Lerp(smoothPivotOffsetT, targetPivotOffsetT, smoothT * Time.deltaTime);
        smoothCamOffsetT = Vector3.Lerp(smoothCamOffsetT, noCollisionOffset, smoothT * Time.deltaTime);

        cameraTransformT.position = Tank.position + camYRotation * smoothPivotOffsetT + aimRotation * smoothCamOffsetT;

        // 반동 만들어 주기.
        if (recoilAngleT > 0.0f)
        {
            recoilAngleT -= recoilAngleBounceT * Time.deltaTime;
        }
        else if (recoilAngleT < 0.0f)
        {
            recoilAngleT += recoilAngleBounceT * Time.deltaTime;
        }
    }

    /// <summary>
    /// AimBehavior 관련 조절 함수.
    /// </summary>
    public float GetCurrentPivotMagnitudeT(Vector3 finalPivotOffset)
    {
        return Mathf.Abs((finalPivotOffset - smoothPivotOffsetT).magnitude);
    }
}