﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.EditorGUIUtility;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace AV.Hierarchy
{
    public abstract class PopupElement : VisualElement
    {
        private static StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("d7461833a510d124191fbed727ac19f0"));
        private static Texture2D arrowIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("70cf301939ec64147b3b646ec72c2cf2"));

        private static PopupElement active;
        private static Dictionary<Type, PopupElement> activePopups = new Dictionary<Type, PopupElement>();

        public string title
        {
            get => titleText.text;
            set => titleText.text = value;
        }
        public VisualElement titleContainer { get; }
        public override VisualElement contentContainer { get; }

        public override bool canGrabFocus => true;

        private TextElement titleText;
        private VisualElement contextArrow;

        private Vector2 position;
        private VisualElement root;

        private Color backgroundColor => isProSkin ? new Color32(35, 35, 35, 230) : new Color32(165, 165, 165, 230);
        
        
        protected PopupElement()
        {
            styleSheets.Add(styleSheet);
            style.backgroundColor = backgroundColor;
            
            AddToClassList("popup-window");
            
            contentContainer = new VisualElement();
            
            contentContainer.AddToClassList("content-container");
            
            titleContainer = new VisualElement();
            titleContainer.AddToClassList("title-container");
            
            titleText = new TextElement { name = "Title" };
            titleContainer.Add(titleText);
            
            hierarchy.Add(titleContainer);
            
            if (!string.IsNullOrEmpty(title))
                hierarchy.Add(CreateSeparator());
            
            hierarchy.Add(contentContainer);

            var type = GetType();
            
            if (activePopups.TryGetValue(GetType(), out var activePopup))
                activePopup.Close();

            activePopups.Add(type, this);
        }
        
        public static T GetPopup<T>() where T : PopupElement
        {
            activePopups.TryGetValue(typeof(T), out var popup);
            return (T)popup;
        }

        public VisualElement CreateSeparator()
        {
            var separator = new VisualElement();
            separator.AddToClassList("separator");
            return separator;
        }

        public void Close()
        {
            RemoveFromHierarchy();
            activePopups.Remove(GetType());
        }
        
        public void ShowInsideWindow(Vector2 position, VisualElement root)
        {
            active = this;
            
            position.x -= 7;
            position.y -= 5;
            
            this.root = root;
            this.position = position;
            
            root.Add(this);
            
            GUI.SetNextControlName("PopupElement");
            GUI.FocusControl("PopupElement");
            Focus();
            
            // BUG: All children must be NOT focusable, or they will steal the focus and blur event will not be sent.
            RegisterCallback<BlurEvent>(evt => Close());
            
            root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChange);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);

            style.top = position.y;
            
            contextArrow = new VisualElement { style = { backgroundImage = arrowIcon, position = Position.Absolute } };
            contextArrow.style.unityBackgroundImageTintColor = backgroundColor;
            contextArrow.AddToClassList("context-arrow");
            
            hierarchy.Insert(0, contextArrow);
        }

        private void OnRootGeometryChange(GeometryChangedEvent evt)
        {
            FitToRootWidth();
        }

        private void OnGeometryChange(GeometryChangedEvent evt)
        {
            FitToRootWidth();
            
            contextArrow.style.left = position.x - layout.x + resolvedStyle.paddingLeft;
        }

        private void FitToRootWidth()
        {
            var xMax = position.x + layout.width;
            var rootWidth = root.layout.width;

            if (xMax > rootWidth)
                style.left = layout.x + (rootWidth - layout.xMax);
            else
                style.left = position.x;
        }
    }
}