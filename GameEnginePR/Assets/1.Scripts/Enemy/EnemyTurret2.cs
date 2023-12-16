using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTurret2 : MonoBehaviour
{
    //public GameObject target;
    public GameObject bullet;
    public Transform EspPoint;
    public GameObject turret; // 외부(유니티 창)에서 직접 할당


    public bool turretRotate;

    private NavMeshAgent nvAgent;
    private Transform target;
    private int rotAngle;
    private int rotSpeed;
    private float aimtToRot;
    private int power;
    private float distance;
    private Vector3 direction;
    private float moveSpeed;
    private float fTime;
    // Start is called before the first frame update
    public bool fireOn;

    private void Awake()
    {

    }
    void Start()
    {
        EspPoint = GameObject.Find("ShootPoint2").transform;
        //target = GameObject.FindGameObjectWithTag("Target").GetComponent<Transform>();
        nvAgent = this.gameObject.GetComponent<NavMeshAgent>();
        target = GameObject.FindGameObjectWithTag("Tank").GetComponent<Transform>();
        moveSpeed = 1.0f;
        power = 80;
        fTime = 0.0f;
        rotAngle = 15;
        rotSpeed = 5;

        //nvAgent = this.gameObject.GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    /*
    void Update()
    {

        direction = target.transform.position - this.transform.position;
        distance = Vector3.Distance(target.transform.position, this.transform.position);
        fTime += Time.deltaTime;

        if (distance < 30.0f)
        {
            fireOn = true;
            turretRotate = true;
            nvAgent.destination = target.position;

            if (fireOn)
            {
                if (fTime >= 0.5f)
                {
                    GameObject obj = Instantiate(bullet, EspPoint.position, EspPoint.rotation) as GameObject;
                    obj.transform.position = EspPoint.transform.position;
                    obj.GetComponent<Rigidbody>().AddForce(direction * power);
                    //Rigidbody bulletAddforce = obj.GetComponent<Rigidbody>();     
                    //bulletAddforce.AddForce(turret.transform.forward * power);
                    fTime = 0.0f;
                }
            }


            //this.transform.LookAt(target.transform.position);
            //aimtToRot = rotAngle * Time.deltaTime;
            //transform.RotateAround(Vector3.zero, Vector3.up, aimtToRot);
            //this.transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * moveSpeed / 2);
            //직선은 lerp 회전은 slerp 일정한 간격으로 보간을 해주는 것. 정확한 길이를 재기 위해.
            if (distance > 40.0f)
            {
                fireOn = false;
                turretRotate = false;
            }

            if (turretRotate)
            {
                turret.transform.rotation = Quaternion.Slerp(turret.transform.rotation,
                    Quaternion.LookRotation(target.transform.position - turret.transform.position), rotSpeed * Time.deltaTime);
            }

        }
    }
    */

    void Update()
    {
        direction = target.transform.position - transform.position;
        distance = Vector3.Distance(target.transform.position, transform.position);
        fTime += Time.deltaTime;

        if (distance < 40.0f)
        {

            fireOn = true;
            turretRotate = true;
            if (fireOn && fTime >= 5f)
            {
                GameObject obj = Instantiate(bullet, EspPoint.position, EspPoint.rotation);
                obj.GetComponent<Rigidbody>().AddForce(direction * power);
                fTime = 0.0f;
            }
        }
        else
        {
            turretRotate = false;
            nvAgent.ResetPath();
        }

        if (turretRotate)
        {
            turret.transform.rotation = Quaternion.Slerp(turret.transform.rotation, Quaternion.LookRotation(direction), rotSpeed * Time.deltaTime);
        }
    }


}