using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무기를 획득하면 획득한 무기를 UI를 통해 보여주고
/// 현재 잔탄량과 전체 소지할 수 잇는 총알량을 출력
/// </summary>
public class TankUIManager : MonoBehaviour
{
    public Color bulletColor = Color.white;
    public Color emptyBulletColor = Color.black;

    private Color noBulletColor; // 투명하게 색깔 표시.
    // 1. UI 링크를 걸어서 바로 찾는다. -> 유기적인 코딩이 불가능. why? UI를 조금만 변경해도 링크 수정을 해야되기 떄문.
    // 2. Tranform에서 이름으로 찾는다. -> 링크가 깨져도 Find로 찾기 때문에 오류가 일어날 가능성이 떨어진다.

    [SerializeField] private Image weaponHUD; // weapon image
    [SerializeField] private GameObject bulletMag; // 탄창
    [SerializeField] private Text totalBulletsHUD; // 잔탄수

    void Start()
    {
        noBulletColor = new Color(0f, 0f, 0f, 0f);
        // UI에 변동이 생겼을 경우를 대비하여.
        if (weaponHUD == null)
        {
            weaponHUD = transform.Find("WeaponHUD/Weapon").GetComponent<Image>();
        }
        if (bulletMag == null)
        {
            bulletMag = transform.Find("WeaponHUD/Data/Mag").gameObject;
        }
        if (totalBulletsHUD == null)
        {
            totalBulletsHUD = transform.Find("WeaponHUD/Data/bulletAmount").GetComponent<Text>();
        }
        Toggle(false);
    }
    /// <summary>
    /// 무기 장착 시 UI 생성.
    /// </summary>
    /// <param name="active"></param>
    public void Toggle(bool active)
    {
        weaponHUD.transform.parent.gameObject.SetActive(active);
    }

    public void UpdateWeaponHUD(Sprite weaponSprite, int bulletLeft, int fullMag, int ExtraBullets) //데이터 업데이트.
    {
        if (weaponSprite != null && weaponHUD.sprite != weaponSprite)
        {
            weaponHUD.sprite = weaponSprite;
            weaponHUD.type = Image.Type.Filled;
            weaponHUD.fillMethod = Image.FillMethod.Horizontal;
        }
        int bulletCount = 0;
        foreach (Transform bullet in bulletMag.transform)
        {
            //남은 탄 량.
            if (bulletCount < bulletLeft)
            {
                bullet.GetComponent<Image>().color = bulletColor;
            }
            else if (bulletCount >= fullMag)
            { // 넘치는 탄
                bullet.GetComponent<Image>().color = noBulletColor;
            }
            else
            { // 사용한 탄
                bullet.GetComponent<Image>().color = emptyBulletColor;
            }
            bulletCount++;
        }
        totalBulletsHUD.text = bulletLeft + "/" + ExtraBullets;
    }
}
