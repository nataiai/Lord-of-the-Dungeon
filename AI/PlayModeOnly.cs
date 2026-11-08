using UnityEngine;

// [PlayModeOnly] on a field: hidden in the Inspector while designing, shown (and editable) while playing, for debugging and trying values. Experimental.showPlayModeOnly turns it off
public class PlayModeOnlyAttribute : PropertyAttribute{}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(PlayModeOnlyAttribute))]
public class PlayModeOnlyDrawer : UnityEditor.PropertyDrawer{
    static bool Show => Application.isPlaying && Experimental.I.showPlayModeOnly; // off in Experimental: always hidden
    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        => Show ? UnityEditor.EditorGUI.GetPropertyHeight(property, label, true) : -UnityEditor.EditorGUIUtility.standardVerticalSpacing; // not playing: takes no space
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label){
        if(Show)UnityEditor.EditorGUI.PropertyField(position, property, label, true);
    }
}
#endif
