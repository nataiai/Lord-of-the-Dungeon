#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Weapon Inspector: section headers, tooltips, and hides code-made settings while a prefab is set
[CustomEditor(typeof(Weapon))]
public class WeaponEditor : Editor
{
    static readonly Dictionary<string, string> headers = new(){
        {"type", "Weapon"},
        {"hitBoxPrefab", "Hitbox"},
        {"effectPrefab", "Effect"},
    };
    static readonly Dictionary<string, string> hideIfSet = new(){ // field -> hidden while this field is assigned
        {"hitBoxLength", "hitBoxPrefab"},
        {"hitBoxWidth", "hitBoxPrefab"},
        {"hitBoxSprite", "hitBoxPrefab"},
        {"hitBoxLayer", "hitBoxPrefab"},
        {"effectController", "effectPrefab"},
        {"effectOrder", "effectPrefab"},
    };
    static readonly Dictionary<string, string> tips = new(){
        {"hitBoxPrefab", "Optional. Empty = made by code from the settings below"},
        {"hitBoxLength", "Box length, from the player along the facing direction. 0 = no box"},
        {"hitBoxWidth", "Box width, across the facing direction. 0 = no box"},
        {"hitBoxSprite", "Optional hitbox sprite (e.g. arrow)"},
        {"hitBoxLayer", "Physics layer of the hitbox (pick one)"},
        {"hitBoxDistance", "Push the hitbox forward from the player"},
        {"velocity", "Hitbox flying speed. 0 = stays where it spawns"},
        {"destroyTimer", "Seconds before the hitbox disappears. 0 = disappears on first touch"},
        {"maxHits", "Enemies it can hit before disappearing. 0 = unlimited"},
        {"up", Side}, {"right", Side}, {"down", Side}, {"left", Side},
        {"effectPrefab", "Optional. Empty = made by code. Needs an Animator; plays the clip below if set, else lives for one cooldown"},
        {"effectController", "Optional Animator Controller. Each clip plays by state name (name the state like the clip). With no clips, its default state plays once. Empty = clips play without a controller"},
        {"animations", "Effect clip per facing: up, right, down, left"},
        {"effectOrder", "Effect draw order relative to the player. 1 = in front, -1 = behind"},
    };
    const string Side = "This facing side (x = right, y = up). offset: extra shift. shape: optional 3+ points that replace the box or prefab collider";

    public override void OnInspectorGUI(){
        serializedObject.Update();
        SerializedProperty p = serializedObject.GetIterator();
        p.NextVisible(true); // script field
        using(new EditorGUI.DisabledScope(true))EditorGUILayout.PropertyField(p);
        while(p.NextVisible(false)){
            if(headers.TryGetValue(p.name, out string header)){EditorGUILayout.Space(); EditorGUILayout.LabelField(header, EditorStyles.boldLabel);}
            if(hideIfSet.TryGetValue(p.name, out string other) && serializedObject.FindProperty(other).objectReferenceValue!=null)continue;
            tips.TryGetValue(p.name, out string tip);
            EditorGUILayout.PropertyField(p, new GUIContent(p.displayName, tip), true);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
