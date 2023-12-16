using FC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 트리거,콜라이더 충돌체를 생성해 무기를 주을 수 있도록 한다.
/// 루팅했으면 콜라이더 충돌체는 제거. -> raycast를 이용.
/// 무기를 다시 버릴 수도 있어야 하며, 충돌체를 다시 붙여준다.
/// 관련해서 UI도 컨트롤할 수 있어야 하고
/// ShootBehaviour에 주은 무기를 넣어주게 된다.
/// </summary>
public class InteractiveWeapon : MonoBehaviour
{
    public string label_weaponName; //무기이름
    public SoundList shotSound, reloadSound, pickSound, dropSound, noBulletSound;
    public Sprite weaponSprite;
    public Vector3 rightHandPosition; // 플레이어 오른손 무기 보정 위치.
    public Vector3 relativeRotation; // 무기마다 다른 offset 값을 갖고 있기 때문에 플레이어에 맞춘 보정을 위한 회전값을 준다.
    public float bulletDamage = 10f;
    public float recoilAngle; //반동
    public enum WeaponType
    {
        NONE,
        SHORT,
        LONG,
    }

    public enum WeaponMode 
    {
        SEMI,
        BURST,
        AUTO,
    }

    public WeaponType weaponType = WeaponType.NONE;
    public WeaponMode weaponMode = WeaponMode.SEMI;
    public int burstSize = 1;

    public int currentMagCapacity, totalBullets; //현재 탄창 양과, 소지하고 있는 전체 총알양
    private int fullMag, maxBullets; // 재장전시 꽉 채우는 탄의 양과 한번에 채울 수 있는 최대 총알량
    private GameObject player, gameController;
    private ShootBehaviour playerInventory; // 인벤토리를 따로 빼서 만드는게 정석이다. 실무에선 이렇게 x 
    private BoxCollider weaponCollider;
    private SphereCollider interactiveRadius;

    private Rigidbody weaponRigidbody;
    private bool pickable; // 총을 주울 수 있는지?

    // UI
    public GameObject screenHUD;
    public WeaponUIManager weaponHUD;
    private Transform pickHUD;
    public Text pickupHUD_Label;

    public Transform muzzleTransform; // 총구.

    private void Awake()
    {
        gameObject.name = this.label_weaponName;
        gameObject.layer = LayerMask.NameToLayer(TagAndLayer.LayerName.IgnoreRayCast);
        foreach(Transform tr in transform)
        {
            tr.gameObject.layer = LayerMask.NameToLayer(TagAndLayer.LayerName.IgnoreRayCast);
        }
        player = GameObject.FindGameObjectWithTag(TagAndLayer.TagName.Player);
        playerInventory = player.GetComponent<ShootBehaviour>();
        gameController = GameObject.FindGameObjectWithTag(TagAndLayer.TagName.GameController);

        if(weaponHUD == null)
        {
            if(screenHUD == null)
            {
                screenHUD = GameObject.Find("ScreenHUD");
            }
            weaponHUD = screenHUD.GetComponent<WeaponUIManager>();
        }
        if(pickHUD == null)
        {
            pickHUD = gameController.transform.Find("PickupHUD");
        }

        //인터랙셩을 위한 충돌체 설정
        weaponCollider = transform.GetChild(0).gameObject.AddComponent<BoxCollider>();
        // 자식에다가 콜라이더를 하는 이유는 상위에는 트리거형 콜라이더를 설정해야되기때문.
        // why? 위에 설명했듯이 무기를 줍기 위해서는 트리거를 이용해서 주워야된다. 이게 상위 개념
        CreateInteractiveRadius(weaponCollider.center); // 이 구모양의 트리거에 들어가면 인터랙션된다.
        weaponRigidbody = gameObject.AddComponent<Rigidbody>();

        if(this.weaponType == WeaponType.NONE)
        {
            this.weaponType = WeaponType.SHORT;
        }
        fullMag = currentMagCapacity;
        maxBullets = totalBullets;
        pickHUD.gameObject.SetActive(false);
        if (muzzleTransform == null)
        {
            muzzleTransform = transform.Find("muzzle");
        }

    }

    //반응형 원형 트리거 생성. 
    private void CreateInteractiveRadius(Vector3 center)
    {
        interactiveRadius = gameObject.AddComponent<SphereCollider>();
        interactiveRadius.center = center;
        interactiveRadius.radius = 1;
        interactiveRadius.isTrigger = true;
    }


    //무기를 집었을 때 생성되는 UI 함수.
    private void TogglePickHUD(bool toggle)
    {
        pickHUD.gameObject.SetActive(toggle);
        if (toggle)
        {
            pickHUD.position = this.transform.position + Vector3.up * 0.5f;
            Vector3 direction = player.GetComponent<BehaviorController>().playerCamera.forward;
            direction.y = 0;
            pickHUD.rotation = Quaternion.LookRotation(direction);
            pickupHUD_Label.text = "Pick " + this.gameObject.name;
        }
    }

    // 총알량 업데이트 함수.
    private void UpdateHUD()
    {
        weaponHUD.UpdateWeaponHUD(weaponSprite, currentMagCapacity, fullMag, totalBullets);
    }

    //interaction이 일어나면 호출되는 토글 함수.
    public void Toggle(bool active)
    {
        if (active)
        {
            SoundManager.Instance.PlayOneShotEffect((int)pickSound, transform.position, 0.5f);
        }
        weaponHUD.Toggle(active);
        UpdateHUD();
    }

    private void Update()
    {
        if(this.pickable && Input.GetButtonDown(ButtonName.Pick))
        {
            //disable phyisics weapon
            weaponRigidbody.isKinematic = true;
            weaponCollider.enabled = false;
            playerInventory.AddWeapon(this);
            Destroy(interactiveRadius);
            this.Toggle(true);
            this.pickable = false; // 무기를 먹은 상태.

            TogglePickHUD(false);
        }

    }
    private void OnCollisionEnter(Collision collision)
    {

        // 총을 바닥에 떨어뜨렸을 때 나오는 드롭사운드
        if(collision.collider.gameObject != player &&
            Vector3.Distance(transform.position, player.transform.position) <= 5f)
        {
            SoundManager.Instance.PlayOneShotEffect((int)dropSound, transform.position, 0.5f);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        // 반경 범위 외로 벗어나면, 해당 트리거, ui 끄기.
        if(other.gameObject == player)
        {
            pickable = false;
            TogglePickHUD(false);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject == player && playerInventory && playerInventory.isActiveAndEnabled)
        {
            pickable = true;
            TogglePickHUD(true);
        }
    }

    public void Drop()
    {
        gameObject.SetActive(true);
        transform.position += Vector3.up; // 자연스러운 떨어짐을 위해.
        weaponRigidbody.isKinematic = false; // 물리 활성화
        this.transform.parent = null;
        CreateInteractiveRadius(weaponCollider.center);
        this.weaponCollider.enabled = true;
        weaponHUD.Toggle(false);
    }
    /// <summary>
    /// 기본적인 재장전 함수
    /// 재장전을 할 때 총알이 남아 있다면 총 탄창량에서 현재 탄찬량을 뺀 나머지 총알을 장전.
    /// 이런 로직을 도와주는 함수이다.
    /// </summary>
    /// <returns></returns>
    public bool StartReload()
    {
        // 재장전이 불가할 때.
        if(currentMagCapacity == fullMag || totalBullets == 0)
        {
            return false;
        } // 재장전 - 잔탕량만큼.
        else if(totalBullets < fullMag - currentMagCapacity)
        {
            currentMagCapacity += totalBullets;
            totalBullets = 0;
        }
        else
        {
            totalBullets -= fullMag - currentMagCapacity;
            currentMagCapacity = fullMag;
        }
        return true;
    }

    //ui 갱신.
    public void EndReload()
    {
        UpdateHUD();
    }

    /// <summary>
    /// 총알을 발사할 때 쓰는 로직.
    /// </summary>
    /// <param name="firstShot"></param>
    /// <returns></returns>
    public bool Shoot(bool firstShot = true)
    {
        // 잔탕량 ui 생성.
        if(currentMagCapacity > 0)
        {
            currentMagCapacity--;
            UpdateHUD();
            return true;
        }// 총알이 없을 때 사운드 생성.
        if(firstShot && noBulletSound != SoundList.None)
        {
            SoundManager.Instance.PlayOneShotEffect((int)noBulletSound, muzzleTransform.position, 5f);
        }

        return false;
    }
    /// <summary>
    /// 총이 바뀌었거나, 새로운 총을 먹었을 때.
    /// 총알량을 리셋하는 함수.
    /// </summary>
    public void ResetBullet()
    {
        currentMagCapacity = fullMag;
        totalBullets = maxBullets;
    }
}
