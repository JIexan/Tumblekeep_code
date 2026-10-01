using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DynamicSettings))]
public class DynamicSettingsDrawer : PropertyDrawer 
{
    private SecondOrderDynamicsFloat dynamics;

    private Vector3[] points = new Vector3[50];

    private Vector3[] step = new Vector3[4];

    private float graphBorder = 10;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        // use the default property height, which takes into account the expanded state
        return property.isExpanded ? 4 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + EditorGUIUtility.singleLineHeight * 8 : EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);
        //position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        position.yMax = position.yMin + EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

        if(property.isExpanded) {

            position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position.yMax = position.yMin + EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("F"), new GUIContent("Frequency"));

            position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position.yMax = position.yMin + EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("Z"), new GUIContent("Damping"));

            position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position.yMax = position.yMin + EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("R"), new GUIContent("Responsiveness"));

            //if(GUI.changed) {
                position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                position.yMax = position.yMin + EditorGUIUtility.singleLineHeight * 8;
                fillGraph(position, property.FindPropertyRelative("F").floatValue, property.FindPropertyRelative("Z").floatValue, property.FindPropertyRelative("R").floatValue);
                DrawPreview(position);
			//}

        }

        EditorGUI.EndProperty();
    }


    private void DrawPreview(Rect window) {
        GUI.BeginClip(window);
        Handles.color = Color.yellow;
        Handles.DrawPolyLine(step);
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(2f, points);
        GUI.EndClip();
    }

    private void fillGraph(Rect window, float f, float z, float r) {

        float rectWidth = window.width;
        float rectHeight = window.height;

        dynamics = new SecondOrderDynamicsFloat(f,z,r,0);

        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < points.Length; i++) {
            points[i] = new Vector3(graphBorder + i * (rectWidth / points.Length), dynamics.Update(1f / points.Length, 1f));

            if(points[i].y <minY) {
                minY = points[i].y;
            }

            if (points[i].y > maxY) {
                maxY = points[i].y;
            }
        }

        float scale = (rectHeight - graphBorder * 2) / (maxY - minY);

        for (int i = 0; i < points.Length; i++) {
            points[i].y = (rectHeight - graphBorder) - scale * (points[i].y - minY);
        }

        float zero = rectHeight - graphBorder - scale * (0f - minY);
        float one = rectHeight - graphBorder - scale * (1f - minY);

        EditorGUI.LabelField(new Rect(window.xMin, window.yMin + zero, 10f,10f), "0");
        EditorGUI.LabelField(new Rect(window.xMin, window.yMin + one, 10f, 10f), "1");

        step[0] = new Vector3(0, zero);
        step[1] = new Vector3(graphBorder, zero);
        step[2] = new Vector3(graphBorder, one);
        step[3] = new Vector3(rectWidth, one);

    }
}
