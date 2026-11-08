using UnityEngine;

// [PlayModeOnly] on a field: hidden in the Inspector while designing, shown (and editable) while playing, for debugging and trying values
public class PlayModeOnlyAttribute : PropertyAttribute{}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(PlayModeOnlyAttribute))]
public class PlayModeOnlyDrawer : UnityEditor.PropertyDrawer{
    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        => Application.isPlaying ? UnityEditor.EditorGUI.GetPropertyHeight(property, label, true) : -UnityEditor.EditorGUIUtility.standardVerticalSpacing; // not playing: takes no space
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label){
        if(Application.isPlaying)UnityEditor.EditorGUI.PropertyField(position, property, label, true);
    }
}
#endif
