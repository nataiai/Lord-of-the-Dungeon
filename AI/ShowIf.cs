using UnityEngine;

// [ShowIf("skill", WeaponSkill.Spin, WeaponSkill.Volley)] on a field: the Inspector shows it only while the field next to it
// named "skill" (an enum or a bool) is one of those values. Hidden fields keep their values
// A [Flags] enum: shown when any of those flags is set. Several [ShowIf] on one field: all must match. [Range] still shows its slider. Experimental.useShowIf off: all show
[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
public class ShowIfAttribute : PropertyAttribute{
    public string field;
    public int[] values;
    public bool flags;
    public ShowIfAttribute(string field, params object[] values){
        this.field = field;
        this.values = System.Array.ConvertAll(values, v => System.Convert.ToInt32(v));
        flags = values.Length>0 && values[0].GetType().IsDefined(typeof(System.FlagsAttribute), false);
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : UnityEditor.PropertyDrawer{
    bool Show(UnityEditor.SerializedProperty property){
        var e = Object.FindFirstObjectByType<Experimental>(); // not Experimental.I: that makes one in the scene
        if(e!=null && !e.useShowIf)return true;
        foreach(ShowIfAttribute a in fieldInfo.GetCustomAttributes(typeof(ShowIfAttribute), true))if(!Show(property, a))return false;
        return true;
    }
    static bool Show(UnityEditor.SerializedProperty property, ShowIfAttribute a){
        string path = property.propertyPath;
        int dot = path.LastIndexOf('.');
        var other = property.serializedObject.FindProperty(dot<0 ? a.field : path.Substring(0,dot+1)+a.field);
        if(other==null)return true; // typo in the name: just show it
        int value = other.propertyType==UnityEditor.SerializedPropertyType.Boolean ? (other.boolValue?1:0) : other.intValue;
        if(a.flags)return System.Array.Exists(a.values, v => (value & v)!=0);
        return System.Array.IndexOf(a.values, value)>=0;
    }
    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        => Show(property) ? UnityEditor.EditorGUI.GetPropertyHeight(property, label, true) : -UnityEditor.EditorGUIUtility.standardVerticalSpacing;
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label){
        if(!Show(property))return;
        var range = (RangeAttribute)System.Attribute.GetCustomAttribute(fieldInfo, typeof(RangeAttribute));
        if(range!=null && property.propertyType==UnityEditor.SerializedPropertyType.Float)UnityEditor.EditorGUI.Slider(position, property, range.min, range.max, label);
        else UnityEditor.EditorGUI.PropertyField(position, property, label, true);
    }
}
#endif
