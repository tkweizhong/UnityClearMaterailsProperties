#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EffectSettingEditor
{
    [MenuItem("GameObject/Material/ClearSceneMaterail")]
    public static void ClearSceneMaterail()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        List<Material> mats = new List<Material>();
        for (int i = 0, n = roots.Length; i < n; ++i)
        {
            FindMaterial(ref mats, roots[i]);
        }
        TryClearMaterials(mats.ToArray());
    }

    [MenuItem("GameObject/Material/Cleanup Material")]
    static public void ClearMaterialProperties()
    {
        UnityEngine.Object[] objs = Selection.GetFiltered(typeof(Material), SelectionMode.DeepAssets);
        if (objs == null || objs.Length < 1) return;
        Material[] mats = new Material[objs.Length];
        for (int i = 0, n = mats.Length; i < n; ++i)
        {
            mats[i] = objs[i] as Material;
        }
        TryClearMaterials(mats);
    }


    private static void FindMaterial(ref List<Material> mats, GameObject gameObject)
    {
        if (gameObject == null) return;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            mats.Add(meshRenderer.sharedMaterial);
        }

        SkinnedMeshRenderer skinMeshRender = gameObject.GetComponent<SkinnedMeshRenderer>();
        if (skinMeshRender != null && skinMeshRender.sharedMaterial != null)
        {
            mats.Add(skinMeshRender.sharedMaterial);
        }

        int childCount = gameObject.transform.childCount;
        for (int i = 0; i < childCount; ++i)
        {
            Transform childTransfer = gameObject.transform.GetChild(i);
            GameObject child = childTransfer != null ? childTransfer.gameObject : null;
            if (child != null) FindMaterial(ref mats, child);
        }
    }

    private static void TryClearMaterials(Material[] mats)
    {
        if (mats == null || mats.Length < 1)
        {
            return;
        }

        for (int i = 0, n = mats.Length; i < n; i++)
        {
            EditorUtility.DisplayProgressBar("Cleanup...", mats[i].name, i / mats.Length);
            Material mat = mats[i] as Material;
            if (mat)
            {
                SerializedObject psSource = new SerializedObject(mat);
                SerializedProperty emissionProperty = psSource.FindProperty("m_SavedProperties");
                SerializedProperty texEnvs = emissionProperty.FindPropertyRelative("m_TexEnvs");
                SerializedProperty floats = emissionProperty.FindPropertyRelative("m_Floats");
                SerializedProperty colos = emissionProperty.FindPropertyRelative("m_Colors");

                Debug.Log("Cleanup Material : " + mat.name + "==================begin");
                CleanMaterialSerializedProperty(texEnvs, mat);
                CleanMaterialSerializedProperty(floats, mat);
                CleanMaterialSerializedProperty(colos, mat);
                Debug.Log("Cleanup Material : " + mat.name + "==================end");
                Debug.Log("");

                psSource.ApplyModifiedProperties();
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();

    }

    private static bool CleanMaterialSerializedProperty(SerializedProperty property, Material mat)
    {
        bool res = false;
        for (int j = property.arraySize - 1; j >= 0; --j)
        {
            SerializedProperty serializedProperty = property.GetArrayElementAtIndex(j);
            if (serializedProperty == null) continue;
            string propertyName = serializedProperty.FindPropertyRelative("first").stringValue;
            if (!mat.HasProperty(propertyName))
            {
                if (propertyName == "_MainTex")
                {
                    SerializedProperty secondSerializedProperty = serializedProperty.FindPropertyRelative("second");
                    if (secondSerializedProperty == null) continue;
                    SerializedProperty textureRefrences = secondSerializedProperty.FindPropertyRelative("m_Texture");
                    if (textureRefrences != null && textureRefrences.objectReferenceValue != null)
                    {
                        textureRefrences.objectReferenceValue = null;
                        res = true;
                    }
                }
                else
                {
                    property.DeleteArrayElementAtIndex(j);
                    Debug.LogWarning("Delete legacy property in serialized object:" + propertyName);
                    res = true;
                }
            }
        }
        return res;
    }
}

#endif //UNITY_EDITOR