using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBase : MonoBehaviour
{
   public class DamageInfo
    {
        public Vector3 location, direction; // 부딪힌 위치
        public float damage;
        public Collider bodyPart; // 맞은 부위에 따라 다르게 표시되는.
        public GameObject origin; // 피격 이펙트

        public DamageInfo(Vector3 location, Vector3 direction, float damage, Collider bodyPart = null, GameObject origin = null)
        {
            this.location = location;
            this.direction = direction;
            this.damage = damage;
            this.bodyPart = bodyPart;
            this.origin = origin;
        }

    }
    [HideInInspector]public bool isDead;
    protected Animator myAnimator;

    public virtual void TakeDamage(Vector3 location, Vector3 direction, float dmamage, Collider bodyPart = null,
        GameObject origin = null)
    {

    }

    public void HitCallBack(DamageInfo damageInfo)
    {
        this.TakeDamage(damageInfo.location, damageInfo.direction, damageInfo.damage, damageInfo.bodyPart, damageInfo.origin);
    }
}
