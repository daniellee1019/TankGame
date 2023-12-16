using UnityEngine;
using UnityEditor; // Editor라는 폴더 아래에 스크립트 파일이 구문을 쓸 수 있음.
using System.Text;
using UnityObject = UnityEngine.Object;

/// <summary>
/// EffectClip 프로퍼티를 수정하기 위한 클래스이다. -> tool 
/// </summary>
public class EffectTool : EditorWindow  // Tool 안에 window를 띄울 수 있음.
{
    //UI 그리는데 필요한 변수들.
    public int uiWidthLarge = 300;
    public int uiWidthMiddle = 200;
    private int selection = 0;
    private Vector2 SP1 = Vector2.zero; //SP = ScrollPosition
    private Vector2 SP2 = Vector2.zero;

    //이펙트 클립
    private GameObject effectSource = null;
    //이펙트 데이터
    private static EffectData effectData;

    //EditorWindow를 띄우기 위한 코드
    [MenuItem("Tools/Effect Tool")]
    static void Init()
    {
        effectData = ScriptableObject.CreateInstance<EffectData>();
        effectData.LoadData();

        EffectTool window = GetWindow<EffectTool>(false, "Effect Tool");
        window.Show();
    }


    private void OnGUI()
    {
        if(effectData == null)
        {
            return;
        }
        EditorGUILayout.BeginVertical();
        {

            //상단. add, remove, copy
            UnityObject source = effectSource;
            EditorHelper.EditorToolTopLayer(effectData, ref selection, ref source, this.uiWidthMiddle); 
            // source에 Sound, Audio 등 어떤 오브젝트가 들어갈지 모르게 때문에 45번 줄에 source를 캐스팅 해줌.
            effectSource = (GameObject)source; // 캐스팅 -> 명시적변환 

            EditorGUILayout.BeginHorizontal();
            {   // 중간. 데이터 목록
                EditorHelper.EditorToolListLayer(ref SP1, effectData, ref selection, ref source, this.uiWidthLarge);
                effectSource = (GameObject)source;

                //설정부분
                EditorGUILayout.BeginVertical();
                {
                    SP2 = EditorGUILayout.BeginScrollView(this.SP2);
                    {
                        if(effectData.GetDataCount() > 0)
                        {
                            EditorGUILayout.BeginVertical();
                            {
                                EditorGUILayout.Separator(); // 구분자
                                EditorGUILayout.LabelField("ID", selection.ToString(), GUILayout.Width(uiWidthLarge));
                                effectData.names[selection] = EditorGUILayout.TextField("이름.", effectData.names[selection], GUILayout.Width(uiWidthLarge * 1.5f));
                                effectData.effectClips[selection].effectType = (EffectType)EditorGUILayout.EnumPopup("이펙트 타입.", effectData.effectClips[selection].effectType, GUILayout.Width(uiWidthLarge));
                                EditorGUILayout.Separator();
                                if(effectSource == null && effectData.effectClips[selection].effectName != string.Empty)
                                {
                                    effectData.effectClips[selection].PreLoad();
                                    effectSource = Resources.Load(effectData.effectClips[selection].effectPath + effectData.effectClips[selection].effectName) as GameObject;
                                }
                                effectSource = (GameObject)EditorGUILayout.ObjectField("이펙트", this.effectSource, typeof(GameObject), false, GUILayout.Width(uiWidthLarge * 1.5f));
                                if (effectSource != null) // 넣은 effectSource를 자동으로 경로와 이름을 찾아서 데이터 추가.
                                {
                                    effectData.effectClips[selection].effectPath = EditorHelper.GetPath(this.effectSource);
                                    effectData.effectClips[selection].effectName = effectSource.name;
                                }
                                else 
                                {
                                    effectData.effectClips[selection].effectPath = string.Empty;
                                    effectData.effectClips[selection].effectName = string.Empty;
                                    effectSource = null;
                                }
                                EditorGUILayout.Separator();
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();
        // 하단. reload & save
        EditorGUILayout.BeginHorizontal();
        {
            if(GUILayout.Button("Reload Settings"))
            {
                effectData = CreateInstance<EffectData>();
                effectData.LoadData();
                selection = 0;
                this.effectSource = null;  
            }
            if (GUILayout.Button("Save"))
            {
                EffectTool.effectData.SaveData();
                CreteEnumStructure();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // 파일이 추가됐으므로 에디터를 최적화 하는 기능.

            }
        }
        EditorGUILayout.EndHorizontal();

    }

    /// <summary>
    /// Tools에서 enum 형식으로 데이터 구조 생성     
    /// </summary>
    public void CreteEnumStructure()
    {
        string enumName = "EffectList";
        StringBuilder builder = new StringBuilder();
        builder.AppendLine();
        for (int i = 0; i < effectData.names.Length; i++)
        {
            if(effectData.names[i] != string.Empty)
            {
                builder.AppendLine("    " + effectData.names[i] + "= " + i + ",");
            }
        }
        EditorHelper.CreateEnumStructure(enumName, builder);
    }
}
