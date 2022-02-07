using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(LoopText), true)]
[CanEditMultipleObjects]
public class LoopTextEditor : UnityEditor.UI.TextEditor
{
    private LoopText _target;
    private SerializedProperty enableLoop;
    private SerializedProperty offsetValue;
    private SerializedProperty startDelayTime;
    private SerializedProperty movePerSecond;
    private SerializedProperty stopDelayTime;
    private SerializedProperty offsetMin;
    private SerializedProperty offsetMax;

    private SerializedProperty m_ShowMaskGraphic;

    protected override void OnEnable()
    {
        base.OnEnable();
        _target = target as LoopText;
        enableLoop = serializedObject.FindProperty("enableLoop");
        offsetValue = serializedObject.FindProperty("offsetValue");
        startDelayTime = serializedObject.FindProperty("startDelayTime");
        movePerSecond = serializedObject.FindProperty("movePerSecond");
        stopDelayTime = serializedObject.FindProperty("stopDelayTime");
        offsetMin = serializedObject.FindProperty("offsetMin");
        offsetMax = serializedObject.FindProperty("offsetMax");
        m_ShowMaskGraphic = serializedObject.FindProperty("m_ShowMaskGraphic");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Loop Text", EditorStyles.boldLabel);
        ++EditorGUI.indentLevel;

        serializedObject.Update();
        EditorGUILayout.PropertyField(enableLoop);
        if (enableLoop.boolValue)
        {
            EditorGUILayout.PropertyField(startDelayTime);
            EditorGUILayout.PropertyField(movePerSecond);
            EditorGUILayout.PropertyField(stopDelayTime);

            GUI.enabled = false;
            EditorGUILayout.PropertyField(offsetValue);
            EditorGUILayout.PropertyField(offsetMin);
            EditorGUILayout.PropertyField(offsetMax);
            GUI.enabled = true;
        }
        --EditorGUI.indentLevel;

        //var graphic = _target.GetComponent<Graphic>();
        //if (graphic && !graphic.IsActive())
        //    EditorGUILayout.HelpBox("Masking disabled due to Graphic component being disabled.", MessageType.Warning);
        //EditorGUILayout.PropertyField(m_ShowMaskGraphic);

        serializedObject.ApplyModifiedProperties();
    }
}
