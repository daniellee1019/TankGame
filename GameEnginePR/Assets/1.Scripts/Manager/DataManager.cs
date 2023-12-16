using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 이펙트, 사운드 등 여러 Data들을 읽고 불러오는, (get, load) 데이터 홀더 같은 개념  
/// </summary>
public class DataManager : MonoBehaviour
{
    private static SoundData soundData = null;
    private static EffectData effectData = null;
    // Start is called before the first frame update
    void Start()
    {
        if(effectData == null)
        {
            effectData = ScriptableObject.CreateInstance<EffectData>();
            effectData.LoadData();
        }
        if(soundData == null)
        {
            soundData = ScriptableObject.CreateInstance<SoundData>();
            soundData.LoadData();
        }
    }

    public static EffectData EffectData()
    {
        if (effectData == null)
        {
            effectData = ScriptableObject.CreateInstance<EffectData>();
            effectData.LoadData();
        }
        return effectData;
    }
    public static SoundData SoundData()
    {
        if(soundData == null)
        {
            soundData = ScriptableObject.CreateInstance<SoundData>();
            soundData.LoadData();
        }
        return soundData;
    }

}
