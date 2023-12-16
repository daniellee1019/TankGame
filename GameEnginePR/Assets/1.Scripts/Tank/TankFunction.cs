using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankFunction : MonoBehaviour
{



    public int moveSpeed = 5; // 속도
    public float move;          // 거리
    public float moveVertical;

    // 좌우 회전
    public int rotSpeed = 120; // 속도
    public float rotate;         // 거리
    public float rotHorizon;

    // 터렛 회전
    public float rotTurret;
    public GameObject turret; // 외부(유니티 창)에서 직접 할당

    // 포신 회전
    public float keyGun;
    public GameObject gunBase; // 외부(유니티 창)에서 직접 할당

    // 총알 처리
    public int power; // 총알 발사 속도
    public GameObject bulletPrefab; // 총알 프리펩
    private Transform muzzle; // 나가는 위치

    public float DestroyTime = 5.0f;
    // Start is called before the first frame update
    void Start()
    {
        muzzle = GameObject.Find("muzzle").transform;
    }

    // Update is called once per frame
    void Update()
    {
        rotate = rotSpeed * Time.deltaTime; // 거리 = 속ㄷ * 시간



        // 터렛 회전
        rotTurret = Input.GetAxis("Window Shake X");
        turret.transform.Rotate(Vector3.up * rotTurret * rotate); //(0, 1, 0)

        // 포신 회전
        keyGun = Input.GetAxis("Mouse ScrollWheel");
        gunBase.transform.Rotate(Vector3.right * keyGun * 4); // (1, 0, 0)
        // 포신의 움직이는 범위
        Vector3 ang = gunBase.transform.eulerAngles;
        if (ang.z > 180)
            ang.z -= 360;
        ang.z = Mathf.Clamp(ang.z, -15, 5);
        gunBase.transform.eulerAngles = ang;
    }
}

