﻿using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AV.Hierarchy
{
    internal class HierarchySettingsProvider : SettingsProvider
    {
        private const string PreferencePath = "Preferences/Workflow/Smart Hierarchy";

        private static UIResources UIResource => UIResources.Index;
        
        private static HierarchySettingsProvider provider;
        private static HierarchyPreferences preferences;
        
        public static HierarchyPreferences Preferences 
        {
            get
            {
                if (!preferences)
                    LoadFromJson();
                return preferences;
            }
        }

        public static event Action onChange;
        
        private SerializedObject serializedObject;
        private TypesPriorityGUI typesPriorityGui;


        private HierarchySettingsProvider(string path, SettingsScope scope)
            : base(path, scope){}

        public override void OnActivate(string searchContext, VisualElement root)
        {
            if (!preferences)
                LoadFromJson();
                
            serializedObject = new SerializedObject(preferences);
            keywords = GetSearchKeywordsFromSerializedObject(serializedObject);
            
            var visualTree = UIResource.preferencesUxml;
            visualTree.CloneTree(root);
            
            var scrollView = root.Query<ScrollView>().First();
            var container = scrollView.contentContainer;
            
            ApplyStyling(root);
            root.Bind(serializedObject);

            var componentsFoldout = root.Query("Components").First();

            provider.CreateTypesPriorityGUI("Types Priority", componentsFoldout, "componentsPriority");
            
            // this is stupid
            container.RegisterCallback<ChangeEvent<bool>>(evt => SaveToJson());
            container.RegisterCallback<ChangeEvent<Enum>>(evt => SaveToJson());
        }

        public override void OnDeactivate()
        {
            SaveToJson();
        }

        private void CreateTypesPriorityGUI(string header, VisualElement parent, string propertyName)
        {
            typesPriorityGui = new TypesPriorityGUI(header, serializedObject.FindProperty(propertyName));
            typesPriorityGui.onChange += SaveToJson;
            
            var container = new IMGUIContainer(() => typesPriorityGui.List.DoLayoutList());

            parent.Add(container);
        }

        private static void ApplyStyling(VisualElement root)
        {
            root.styleSheets.Add(UIResource.preferencesStyle);
            root.styleSheets.Add(UIResource.foldoutHeaderStyle);

            if (EditorGUIUtility.isProSkin)
                root.styleSheets.Add(UIResource.foldoutHeaderDarkStyle);
        }
        
        private static void LoadFromJson()
        {
            if (!preferences)
                preferences = ScriptableObject.CreateInstance<HierarchyPreferences>();

            var json = EditorPrefs.GetString(PreferencePath);
            JsonUtility.FromJsonOverwrite(json, preferences);
        }

        private static void SaveToJson()
        {
            if (!preferences)
                return;
        
            var json = JsonUtility.ToJson(preferences, true);
            EditorPrefs.SetString(PreferencePath, json);
            
            onChange?.Invoke();
        }

        [SettingsProvider]
        private static SettingsProvider GetSettingsProvider()
        {
            return provider ?? (provider = new HierarchySettingsProvider(PreferencePath, SettingsScope.User));
        }
    }
}