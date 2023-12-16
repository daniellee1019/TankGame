using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using Complete;
public class GettingInAndOutOfCars : MonoBehaviour
{
    [Header("Camera")]
    //[SerializeField] AutoCam mCamera = null;
    public GameObject CarCam;
    public GameObject HumanCam;

    [Header("Human")]
    public GameObject human;


    [SerializeField] float closeDistance = 15f;

    [Space, Header("Car Stuff")]
    public GameObject car;

    [Header("Input")]
    [SerializeField] KeyCode enterExitKey = KeyCode.F;

    public CarController carEngine;

    bool inCar = false;

    public GameObject StartMSG;
    public GameObject aimSlider;

    private float ftime = 0.0f;

    // Start is called before the first frame update
    void Start()
    {

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        car.GetComponent<CarController>();
        car.GetComponent<CarUserControl>();
        car.GetComponent<TankFunction>();
        car.GetComponent<TankHealth>();
        car.GetComponent<TankShooting>();

        
        
    }

    // Update is called once per frame
    void Update()
    {
        ftime += Time.deltaTime;
        if(ftime > 3.0f)
        {
            StartMSG.SetActive(false);
        }
        else
        {
            StartMSG.SetActive(true);
        }

        if (Input.GetKeyDown(enterExitKey))
        {
            if (inCar)
            {
                GetOutOfCar();
                

            }
            else if (Vector3.Distance(car.transform.position, human.transform.position) < closeDistance)
            {
                GetInroCar();
            }

        }
    }

    void GetOutOfCar()
    {
        inCar = false;

        human.SetActive(true);
        HumanCam.SetActive(true);

        human.transform.position = car.transform.position + car.transform.TransformDirection(Vector3.left);

        CarCam.SetActive(false);
        car.GetComponent<CarController>().enabled = false;
        car.GetComponent<CarUserControl>().enabled = false;
        car.GetComponent<TankFunction>().enabled = false;
        car.GetComponent<TankHealth>().enabled = false;
        car.GetComponent<TankShooting>().enabled = false;

        aimSlider.SetActive(false);

        carEngine.Move(0, 0, 1, 1);

    }


    void GetInroCar()
    {
        inCar = true;

        CarCam.SetActive(true);

        human.SetActive(false);
        HumanCam.SetActive(false);

        car.GetComponent<CarController>().enabled = true;
        car.GetComponent<CarUserControl>().enabled = true;
        car.GetComponent<TankFunction>().enabled = true;
        car.GetComponent<TankHealth>().enabled = true;
        car.GetComponent<TankShooting>().enabled = true;

        aimSlider.SetActive(true);

    }

}