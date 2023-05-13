using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

using System;
using System.Reflection;
using System.Collections.Generic;

namespace SK.Framework
{
    /// <summary>
    /// 过滤类型
    /// </summary>
    public enum FilterMode
    {
        Name, //根据名字筛选
        Component, //根据组件筛选
        Layer, //根据层级筛选
        Tag, //根据标签筛选
        Active, //根据是否活跃筛选
        Missing, //丢失筛选
    }
    public enum MissingMode
    {
        Material, //材质丢失
        Mesh, //网格丢失
        Script //脚本丢失
    }
    [SerializeField]
    public class FilterCondition
    {
        public FilterMode filterMode;
        public MissingMode missingMode;
        public string stringValue;
        public int intValue;
        public bool boolValue;
        public Type typeValue;

        public FilterCondition(FilterMode filterMode, string stringValue)
        {
            this.filterMode = filterMode;
            this.stringValue = stringValue;
        }
        public FilterCondition(FilterMode filterMode, int intValue)
        {
            this.filterMode = filterMode;
            this.intValue = intValue;
        }
        public FilterCondition(FilterMode filterMode, bool boolValue)
        {
            this.filterMode = filterMode;
            this.boolValue = boolValue;
        }
        public FilterCondition(FilterMode filterMode, Type typeValue)
        {
            this.filterMode = filterMode;
            this.typeValue = typeValue;
        }
        public FilterCondition(FilterMode filterMode, MissingMode missingMode)
        {
            this.filterMode = filterMode;
            this.missingMode = missingMode;
        }
        /// <summary>
        /// 判断物体是否符合条件
        /// </summary>
        /// <param name="target">物体</param>
        /// <returns>符合条件返回true,否则返回false</returns>
        public bool IsMatch(GameObject target)
        {
            switch (filterMode)
            {
                case FilterMode.Name: return target.name.ToLower().Contains(stringValue.ToLower());
                case FilterMode.Component: return target.GetComponent(typeValue) != null;
                case FilterMode.Layer: return target.layer == intValue;
                case FilterMode.Tag: return target.CompareTag(stringValue);
                case FilterMode.Active: return target.activeSelf == boolValue;
                case FilterMode.Missing:
                    switch (missingMode)
                    {
                        case MissingMode.Material:
                            var mr = target.GetComponent<MeshRenderer>();
                            if (mr == null) return false;
                            Material[] materials = mr.sharedMaterials;
                            bool flag = false;
                            for (int i = 0; i < materials.Length; i++)
                            {
                                if (materials[i] == null)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            return flag;
                        case MissingMode.Mesh:
                            var mf = target.GetComponent<MeshFilter>();
                            if (mf == null) return false;
                            return mf.sharedMesh == null;
                        case MissingMode.Script:
                            Component[] components = target.GetComponents<Component>();
                            bool retV = false;
                            for (int i = 0; i < components.Length; i++)
                            {
                                if (components[i] == null)
                                {
                                    retV = true;
                                    break;
                                }
                            }
                            return retV;
                        default:
                            return false;
                    }
                default: return false;
            }
        }
    }

    public sealed class Filter : EditorWindow
    {
        [MenuItem("SKFramework/Tools/Filter")]
        private static void Open()
        {
            var window = GetWindow<Filter>();
            window.titleContent = new GUIContent("Filter");
            window.Show();
        }
        //筛选的目标
        private enum FilterTarget
        {
            All, //在所有物体中筛选
            Specified, //在指定根级物体内筛选
        }
        private FilterTarget filterTarget = FilterTarget.All;
        //存储所有筛选条件
        private readonly List<FilterCondition> filterConditions = new List<FilterCondition>();
        //指定的筛选根级
        private Transform specifiedTarget;
        //存储所有组件类型
        private List<Type> components;
        //存储所有组件名称
        private List<string> componentsNames;
        private readonly List<GameObject> selectedObjects = new List<GameObject>();
        private Vector2 scroll = Vector2.zero;

        private void OnEnable()
        {
            components = new List<Type>();
            componentsNames = new List<string>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var types = assemblies[i].GetTypes();
                for (int j = 0; j < types.Length; j++)
                {
                    if (types[j].IsSubclassOf(typeof(Component)))
                    {
                        components.Add(types[j]);
                        componentsNames.Add(types[j].Name);
                    }
                }
            }
        }
        private void OnGUI()
        {
            OnTargetGUI();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            OnConditionGUI();
            OnIsMatchedGameObjectsGUI();
            EditorGUILayout.EndScrollView();
            OnFilterGUI();
        }
        private void OnTargetGUI()
        {
            filterTarget = (FilterTarget)EditorGUILayout.EnumPopup("Target", filterTarget);
            switch (filterTarget)
            {
                case FilterTarget.Specified:
                    specifiedTarget = EditorGUILayout.ObjectField("Root", specifiedTarget, typeof(Transform), true) as Transform;
                    break;
            }
            EditorGUILayout.Space();
        }
        private void OnConditionGUI()
        {
            if (GUILayout.Button("Create New Condition", "DropDownButton"))
            {
                GenericMenu gm = new GenericMenu();
                gm.AddItem(new GUIContent("Name"), false, () => filterConditions.Add(new FilterCondition(FilterMode.Name, "GameObject")));
                gm.AddItem(new GUIContent("Component"), false, () => filterConditions.Add(new FilterCondition(FilterMode.Component, typeof(Transform))));
                gm.AddItem(new GUIContent("Layer"), false, () => filterConditions.Add(new FilterCondition(FilterMode.Layer, 0)));
                gm.AddItem(new GUIContent("Tag"), false, () => filterConditions.Add(new FilterCondition(FilterMode.Tag, "Untagged")));
                gm.AddItem(new GUIContent("Active"), false, () => filterConditions.Add(new FilterCondition(FilterMode.Active, true)));
                gm.AddItem(new GUIContent("Missing / Material"), false, () => filterConditions.Add(new FilterCondition(FilterMode.Missing, MissingMode.Material)));
                gm.AddItem(new GUIContent("Missing / Mesh"), false, () => filterConditions.Add(new FilterCondition(FilterMode.Missing, MissingMode.Mesh)));
                gm.AddItem(new GUIContent("Missing / Script"), false, () => filterConditions.Add(new FilterCondition(FilterMode.Missing, MissingMode.Script)));
                gm.ShowAsContext();
            }
            EditorGUILayout.Space();
            if (filterConditions.Count > 0)
            {
                GUILayout.BeginVertical("Badge");
                for (int i = 0; i < filterConditions.Count; i++)
                {
                    var condition = filterConditions[i];
                    GUILayout.BeginHorizontal();
                    if (filterConditions.Count > 1)
                        GUILayout.Label($"{i + 1}.", GUILayout.Width(30f));
                    switch (condition.filterMode)
                    {
                        case FilterMode.Name:
                            GUILayout.Label("Name", GUILayout.Width(80f));
                            condition.stringValue = EditorGUILayout.TextField(condition.stringValue);
                            break;
                        case FilterMode.Component:
                            var index = componentsNames.FindIndex(m => m == condition.typeValue.Name);
                            GUILayout.Label("Component", GUILayout.Width(80f));
                            var newIndex = EditorGUILayout.Popup(index, componentsNames.ToArray());
                            if (index != newIndex) condition.typeValue = components[newIndex];
                            break;
                        case FilterMode.Layer:
                            GUILayout.Label("Layer", GUILayout.Width(80f));
                            condition.intValue = EditorGUILayout.LayerField(condition.intValue);
                            break;
                        case FilterMode.Tag:
                            GUILayout.Label("Tag", GUILayout.Width(80f));
                            condition.stringValue = EditorGUILayout.TagField(condition.stringValue);
                            break;
                        case FilterMode.Active:
                            GUILayout.Label("Active", GUILayout.Width(80f));
                            condition.boolValue = EditorGUILayout.Toggle(condition.boolValue);
                            break;
                        case FilterMode.Missing:
                            GUILayout.Label("Missing", GUILayout.Width(80f));
                            condition.missingMode = (MissingMode)EditorGUILayout.EnumPopup(condition.missingMode);
                            break;
                        default:
                            break;
                    }
                    if (GUILayout.Button("×", "MiniButton", GUILayout.Width(20f)))
                    {
                        filterConditions.RemoveAt(i);
                        return;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            EditorGUILayout.Space();
        }
        private void OnIsMatchedGameObjectsGUI()
        {
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                GameObject obj = selectedObjects[i];
                if (obj == null)
                {
                    selectedObjects.RemoveAt(i);
                    i--;
                    continue;
                }
                GUILayout.BeginHorizontal("IN Title");
                GUILayout.Label(obj.name);
                GUILayout.EndHorizontal();
                if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.PingObject(obj);
                }
            }
            GUILayout.FlexibleSpace();
        }
        private void OnFilterGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Filter", "ButtonLeft"))
            {
                selectedObjects.Clear();
                List<GameObject> targetGameObjects = new List<GameObject>();
                switch (filterTarget)
                {
                    case FilterTarget.All:
                        GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                        for (int i = 0; i < rootGameObjects.Length; i++)
                        {
                            var root = rootGameObjects[i];
                            var allChildren = root.GetComponentsInChildren<Transform>(true);
                            for (int j = 0; j < allChildren.Length; j++)
                            {
                                EditorUtility.DisplayProgressBar("Filter", allChildren[j].name, (float)i / rootGameObjects.Length);
                                targetGameObjects.Add(allChildren[j].gameObject);
                            }
                        }
                        EditorUtility.ClearProgressBar();
                        break;
                    case FilterTarget.Specified:
                        Transform[] children = specifiedTarget.GetComponentsInChildren<Transform>(true);
                        for (int i = 0; i < children.Length; i++)
                        {
                            EditorUtility.DisplayProgressBar("Filter", children[i].name, (float)i / children.Length);
                            targetGameObjects.Add(children[i].gameObject);
                        }
                        EditorUtility.ClearProgressBar();
                        break;
                    default:
                        break;
                }

                for (int i = 0; i < targetGameObjects.Count; i++)
                {
                    GameObject target = targetGameObjects[i];
                    bool isMatch = true;
                    for (int j = 0; j < filterConditions.Count; j++)
                    {
                        if (!filterConditions[j].IsMatch(target))
                        {
                            isMatch = false;
                            break;
                        }
                    }
                    EditorUtility.DisplayProgressBar("Filter", $"{target.name} -> Is Matched : {isMatch}", (float)i / targetGameObjects.Count);
                    if (isMatch)
                    {
                        selectedObjects.Add(target);
                    }
                }
                EditorUtility.ClearProgressBar();
            }
            if (GUILayout.Button("Select", "ButtonMid"))
            {
                Selection.objects = selectedObjects.ToArray();
            }
            if (GUILayout.Button("Clear", "ButtonRight"))
            {
                selectedObjects.Clear();
            }
            GUILayout.EndHorizontal();
        }
    }
}