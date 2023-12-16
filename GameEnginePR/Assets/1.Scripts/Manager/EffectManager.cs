using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : SingletonMonobehaviour<EffectManager>
{

    private Transform effecRoot = null; 

    // Start is called before the first frame update
    void Start()
    {
        if(effecRoot == null)
        {// 이펙트들이 EffectRoot 밑으로 풀링이 된다. -> 공간 생성
            effecRoot = new GameObject("EffectRoot").transform;
            effecRoot.SetParent(transform);
        }
    }

    public GameObject EffectOneShot(int index, Vector3 position)
    {
        EffectClip clip = DataManager.EffectData().GetClip(index);
        GameObject effectInstance = clip.Instantiate(position);
        effectInstance.SetActive(true);
        return effectInstance;
    }
}
