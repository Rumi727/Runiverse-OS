#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Collections.Generic;
using RuniOS.Collections.Handlers.Entrys;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System.Collections;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Collections
{
    [CustomInspectorDrawer(typeof(DictionaryEntry))]
    [CustomInspectorDrawer(typeof(KeyValuePair<,>), true)]
    [CustomInspectorDrawer(typeof(ISerializableKeyValuePair), true)]
    [CustomInspectorDrawer(typeof(ISerializableKeyValuePair<,>), true)]
    public class DictionaryEntryInspectorDrawer : IMGUIInspectorDrawer
    {
        public DictionaryEntryInspectorDrawer(IInspectorVariableElement element) : base(element)
        {
            // 가독성 꼬라지ㅋㅋ
            
            keyElement = element.inspectableObjectElement.FindVariableElement(nameof(DictionaryEntry.Key));
            keyElement = new CustomAccessVariableElement.Builder(keyElement)
                .AddWriteAction((_, valueKey) =>
                {
                    KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(element.value);
                    element.value = EntryHandler.CreateEntry(element.variableType, valueKey, entry.Value);
                })
                .AddSetValuesAction((_, valueKeys) =>
                {
                    var zipedEnumerable = element.GetValues()
                        .Zip(valueKeys, (elementValue, entryKey) => (value: EntryHandler.FindEntry(elementValue).Value, key: entryKey));
                    
                    element.SetValues
                    (
                        zipedEnumerable
                            .Select(x => EntryHandler.CreateEntry(element.variableType, x.key, x.value))
                    );
                })
                .SetIsReadableFunc((_, flags) => element.IsReadable(flags))
                .SetIsWritableFunc((_, flags) => element.IsReadable(flags) && element.IsWritable(flags))
                .Build();
            
            valueElement = element.inspectableObjectElement.FindVariableElement(nameof(DictionaryEntry.Value));
            valueElement = new CustomAccessVariableElement.Builder(valueElement)
                .AddWriteAction((_, valueValue) =>
                {
                    KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(element.value);
                    element.value = EntryHandler.CreateEntry(element.variableType, entry.Key, valueValue);
                })
                .AddSetValuesAction((_, valueValues) =>
                {
                    var zipedEnumerable = element.GetValues()
                        .Zip(valueValues, (elementValue, entryValue) => (key: EntryHandler.FindEntry(elementValue).Key, value: entryValue));
                    
                    element.SetValues
                    (
                        zipedEnumerable
                            .Select(x => EntryHandler.CreateEntry(element.variableType, x.key, x.value))
                    );
                })
                .SetIsReadableFunc((_, flags) => element.IsReadable(flags))
                .SetIsWritableFunc((_, flags) => element.IsReadable(flags) && element.IsWritable(flags))
                .Build();

            keyDrawer = FindDrawer(keyElement);
            valueDrawer = FindDrawer(valueElement);
        }

        public override bool isField => keyDrawer.isField && valueDrawer.isField;

        public IInspectorVariableElement keyElement { get; }
        public IMGUIInspectorDrawer keyDrawer { get; }
        
        public IInspectorVariableElement valueElement { get; }
        public IMGUIInspectorDrawer valueDrawer { get; }
        
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null)
        {
            BeginWideMode(EditorGUIUtility.wideMode && isField);
            
            int controlID = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, controlID, label ?? GUIContent.none, 3); // 2로 하면 크기 절반 줄어듬
            
            string keyLabel = GetTextOrKey("gui.key");
            GUIContent keyLabelContent = new GUIContent(keyLabel);
            
            string valueLabel = GetTextOrKey("gui.value");
            GUIContent valueLabelContent = new GUIContent(valueLabel);
            
            if (isField)
            {
                BeginIndentLevel(0);
                float fieldWidth = (position.width - 4) / 2f;

                {
                    position.width = fieldWidth;
                    position.height = keyDrawer.GetHeight(keyLabelContent, flags, isInArray);

                    BeginLabelWidth(keyLabel);
                    keyDrawer.OnGUI(position, keyLabelContent, flags, isInArray, clipping);
                    EndLabelWidth();

                    position.x += position.width + 4;
                }

                {
                    position.width = fieldWidth.Ceil();
                    position.height = valueDrawer.GetHeight(valueLabelContent, flags, isInArray);

                    BeginLabelWidth(valueLabel);
                    valueDrawer.OnGUI(position, valueLabelContent, flags, isInArray, clipping);
                    EndLabelWidth();
                }

                EndIndentLevel();
            }
            else
            {
                if (EditorGUIUtility.hierarchyMode)
                    BeginLabelWidth(EditorGUIUtility.labelWidth - 15);
                
                position.height = keyDrawer.GetHeight(label, flags, isInArray);
                
                keyDrawer.OnGUI(position, keyLabelContent, flags, isInArray, clipping);

                position.y += position.height + 2;
                position.height = valueDrawer.GetHeight(label, flags, isInArray);
                
                valueDrawer.OnGUI(position, valueLabelContent, flags, isInArray, clipping);
                
                if (EditorGUIUtility.hierarchyMode)
                    EndLabelWidth();
            }
            
            EndWideMode();
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            float height;
            if (isField)
                height = Max(keyDrawer.GetHeight(label, flags, isInArray), valueDrawer.GetHeight(label, flags, isInArray));
            else
                height = keyDrawer.GetHeight(label, flags, isInArray) + 2 + valueDrawer.GetHeight(label, flags, isInArray);
            
            return height + (!LabelHasContent(label) || (EditorGUIUtility.wideMode && isField) ? 0 : EditorGUIUtility.singleLineHeight + 2);
        }
    }
}