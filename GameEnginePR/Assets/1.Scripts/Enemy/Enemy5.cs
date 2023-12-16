using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Complete
{
    public class Enemy5 : MonoBehaviour
    {
        //public GameObject target;
        public GameObject Tbullet;
        public Transform ShootPoint;
        public GameObject Tturret; // 외부(유니티 창)에서 직접 할당


        public bool TturretRotate;

        private NavMeshAgent TnvAgent;
        private Transform Ttarget;
        private int TrotAngle;
        private int TrotSpeed;
        private float TaimtToRot;
        private int Tpower;
        private float Tdistance;
        private Vector3 Tdirection;
        private float TmoveSpeed;
        private float TfTime;
        // Start is called before the first frame update
        public bool TfireOn;

        private void Awake()
        {

        }
        void Start()
        {
            ShootPoint = GameObject.Find("T5muzzle").transform;
            //target = GameObject.FindGameObjectWithTag("Target").GetComponent<Transform>();
            TnvAgent = this.gameObject.GetComponent<NavMeshAgent>();
            Ttarget = GameObject.FindGameObjectWithTag("Tank").GetComponent<Transform>();
            TmoveSpeed = 1.0f;
            Tpower = 80;
            TfTime = 0.0f;
            TrotAngle = 15;
            TrotSpeed = 5;

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
            Tdirection = Ttarget.transform.position - transform.position;
            Tdistance = Vector3.Distance(Ttarget.transform.position, transform.position);
            TfTime += Time.deltaTime;

            if (Tdistance < 30f)
            {
                TnvAgent.SetDestination(Ttarget.transform.position);
                TfireOn = true;
                TturretRotate = true;
                if (TfireOn && TfTime >= 5f)
                {
                    GameObject obj = Instantiate(Tbullet, ShootPoint.position, ShootPoint.rotation);
                    obj.GetComponent<Rigidbody>().AddForce(Tdirection * Tpower);
                    TfTime = 0.0f;
                }
            }
            else
            {
                TturretRotate = false;
                TnvAgent.ResetPath();
            }

            if (TturretRotate)
            {
                Tturret.transform.rotation = Quaternion.Slerp(Tturret.transform.rotation, Quaternion.LookRotation(Tdirection), TrotSpeed * Time.deltaTime);
            }
        }
    }
}
