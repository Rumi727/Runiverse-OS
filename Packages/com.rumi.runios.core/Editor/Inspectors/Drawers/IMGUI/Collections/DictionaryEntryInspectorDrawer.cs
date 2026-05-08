#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Collections.Generic;
using RuniOS.Collections.Handlers.Entrys;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;
using System.Collections;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Collections
{
    [CustomInspectorDrawer(typeof(DictionaryEntry))]
    [CustomInspectorDrawer(typeof(KeyValuePair<,>), true)]
    [CustomInspectorDrawer(typeof(ISerializableKeyValuePair), true)]
    [CustomInspectorDrawer(typeof(ISerializableKeyValuePair<,>), true)]
    public class DictionaryEntryInspectorDrawer : IMGUIInspectorDrawer
    {
        public DictionaryEntryInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder)
        {
            // 가독성 꼬라지ㅋㅋ
            
            keyElement = element.inspectableObjectElement.GetVariableElement(nameof(DictionaryEntry.Key));
            keyElement.accessor.writeAction = valueKey =>
            {
                KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(element.value);
                element.value = EntryHandler.CreateEntry(element.variableType, valueKey, entry.Value);
            };
            keyElement.accessor.setValuesAction = valueKeys =>
            {
                var zipedEnumerable = element.GetValues()
                    .Zip(valueKeys, (elementValue, entryKey) => (value: EntryHandler.FindEntry(elementValue).Value, key: entryKey));
                
                element.SetValues
                (
                    zipedEnumerable
                        .Select(x => EntryHandler.CreateEntry(element.variableType, x.key, x.value))
                );
            };
            keyElement.accessor.isReadableFunc = (flags, _) => element.IsReadable(flags, true);
            keyElement.accessor.isWritableFunc = (flags, _) => element.IsReadable(flags, true) && element.IsWritable(flags, true);
            
            valueElement = element.inspectableObjectElement.GetVariableElement(nameof(DictionaryEntry.Value));
            valueElement.accessor.writeAction = valueValue =>
            {
                KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(element.value);
                element.value = EntryHandler.CreateEntry(element.variableType, entry.Key, valueValue);
            };
            valueElement.accessor.setValuesAction = valueValues =>
            {
                var zipedEnumerable = element.GetValues()
                    .Zip(valueValues, (elementValue, entryValue) => (key: EntryHandler.FindEntry(elementValue).Key, value: entryValue));
                
                element.SetValues
                (
                    zipedEnumerable
                        .Select(x => EntryHandler.CreateEntry(element.variableType, x.key, x.value))
                );
            };
            valueElement.accessor.isReadableFunc = (flags, _) => element.IsReadable(flags, true);
            valueElement.accessor.isWritableFunc = (flags, _) => element.IsReadable(flags, true) && element.IsWritable(flags, true);

            keyDrawer = FindDrawer(keyElement, inheritedAttributes, undoRecorder);
            valueDrawer = FindDrawer(valueElement, inheritedAttributes, undoRecorder);
        }

        public override bool isField => keyDrawer.isField && valueDrawer.isField;

        public IInspectorVariableElement keyElement { get; }
        public IMGUIInspectorDrawer keyDrawer { get; }
        
        public IInspectorVariableElement valueElement { get; }
        public IMGUIInspectorDrawer valueDrawer { get; }

        protected override void OnGUI(Rect position, GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            label ??= new GUIContent(element?.displayName ?? inspectable.inspectionDisplayName);
            
            BeginWideMode(EditorGUIUtility.wideMode && isField);
            
            int controlID = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, controlID, label, 3); // 2로 하면 크기 절반 줄어듬
            
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
                    position.height = keyDrawer.GetHeight(keyLabelContent, flags, context);

                    BeginLabelWidth(keyLabel);
                    keyDrawer.Draw(position, keyLabelContent, flags, context);
                    EndLabelWidth();

                    position.x += position.width + 4;
                }

                {
                    position.width = fieldWidth.Ceil();
                    position.height = valueDrawer.GetHeight(valueLabelContent, flags, context);

                    BeginLabelWidth(valueLabel);
                    valueDrawer.Draw(position, valueLabelContent, flags, context);
                    EndLabelWidth();
                }

                EndIndentLevel();
            }
            else
            {
                if (EditorGUIUtility.hierarchyMode)
                    BeginLabelWidth(EditorGUIUtility.labelWidth - 15);
                
                position.height = keyDrawer.GetHeight(label, flags, context);
                
                keyDrawer.Draw(position, keyLabelContent, flags, context);

                position.y += position.height + 2;
                position.height = valueDrawer.GetHeight(label, flags, context);
                
                valueDrawer.Draw(position, valueLabelContent, flags, context);
                
                if (EditorGUIUtility.hierarchyMode)
                    EndLabelWidth();
            }
            
            EndWideMode();
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            float height;
            if (isField)
                height = Max(keyDrawer.GetHeight(label, flags, context), valueDrawer.GetHeight(label, flags, context));
            else
                height = keyDrawer.GetHeight(label, flags, context) + 2 + valueDrawer.GetHeight(label, flags, context);
            
            return height + (!LabelHasContent(label) || (EditorGUIUtility.wideMode && isField) ? 0 : EditorGUIUtility.singleLineHeight + 2);
        }
    }
}