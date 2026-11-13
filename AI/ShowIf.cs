using UnityEngine;

// [ShowIf("skill", WeaponSkill.Spin, WeaponSkill.Volley)] on a field: the Inspector shows it only while the field next to it
// named "skill" (an enum or a bool) is one of those values. Hidden fields keep their values
public class ShowIfAttribute : PropertyAttribute{
    public string field;
    public int[] values;
    public ShowIfAttribute(string field, params object[] values){
        this.field = field;
        this.values = System.Array.ConvertAll(values, v => System.Convert.ToInt32(v));
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : UnityEditor.PropertyDrawer{
    bool Show(UnityEditor.SerializedProperty property){
        var a = (ShowIfAttribute)attribute;
        string path = property.propertyPath;
        int dot = path.LastIndexOf('.');
        var other = property.serializedObject.FindProperty(dot<0 ? a.field : path.Substring(0,dot+1)+a.field);
        if(other==null)return true; // typo in the name: just show it
        int value = other.propertyType==UnityEditor.SerializedPropertyType.Boolean ? (other.boolValue?1:0) : other.intValue;
        return System.Array.IndexOf(a.values, value)>=0;
    }
    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        => Show(property) ? UnityEditor.EditorGUI.GetPropertyHeight(property, label, true) : -UnityEditor.EditorGUIUtility.standardVerticalSpacing;
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label){
        if(Show(property))UnityEditor.EditorGUI.PropertyField(position, property, label, true);
    }
}
#endif
