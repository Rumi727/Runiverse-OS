#nullable enable
using RuniOS.Editor.IMGUI;
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Editor.Unity.Inspectors;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.Sounds
{
    public abstract class RuniAudioSourceEditor<TTarget> : CustomInspectorBase<TTarget> where TTarget : RuniAudioSource
    {
        public PlayableController playableController { get; } = new PlayableController(new GenericTimeUnit());

        protected override bool repaintInEditor
        {
            get
            {
                for (int i = 0; i < playableController.targets.Count; i++)
                {
                    if (playableController.targets[i].isPlaying)
                        return true;
                }

                return false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            playableController.targets.Clear();
            foreach (TTarget? item in targets)
            {
                if (item != null)
                    playableController.targets.Add(item);
            }

            playableController.loopRangeSetter = SetLoopRange;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            if (DrawSourceLayout())
                Space();

            DrawPlaybackLayout();
            Space();

            DrawLoopingLayout();
            Space();

            DrawSpatialLayout();
            Space();

            DrawTransportLayout();
            Space();

            DrawInformationLayout();
        }

        protected virtual bool DrawSourceLayout() => false;

        protected virtual void DrawPlaybackLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.playback.header"), EditorStyles.boldLabel);

            DrawSliderPropertyLayout("_volume", TrTempContent("runios-editor:inspector.runi_audio_source.playback.volume"), static x => x.volume, static (x, value) => x.volume = value, 0, 1);
            DrawSliderPropertyLayout("_tempo", TrTempContent("runios-editor:inspector.runi_audio_source.playback.tempo"), static x => x.tempo, static (x, value) => x.tempo = value, -3, 3);
            DrawSliderPropertyLayout("_pitch", TrTempContent("runios-editor:inspector.runi_audio_source.playback.pitch"), static x => x.pitch, static (x, value) => x.pitch = value, 0, 3);
        }

        protected virtual void DrawLoopingLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.looping.header"), EditorStyles.boldLabel);

            DrawPropertyLayoutAndSync("_loop", TrTempContent("runios-editor:inspector.runi_audio_source.looping.loop"), static x => x.loop, static (x, value) => x.loop = value);
            DrawPropertyLayoutAndSync("_loopStart", TrTempContent("runios-editor:inspector.runi_audio_source.looping.start"), static x => x.loopStart, static (x, value) => x.loopStart = value);
            DrawNullableDoublePropertyLayoutAndSync
            (
                "_loopEnd",
                TrTempContent("runios-editor:inspector.runi_audio_source.looping.end"),
                static x => x.loopEnd,
                static (x, value) => x.loopEnd = value
            );
        }

        protected virtual void DrawSpatialLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.spatial.header"), EditorStyles.boldLabel);

            DrawSliderPropertyLayout("_panStereo", TrTempContent("runios-editor:inspector.runi_audio_source.spatial.pan_stereo"), static x => x.panStereo, static (x, value) => x.panStereo = value, -1, 1);
            DrawSliderPropertyLayout("_spatialBlend", TrTempContent("runios-editor:inspector.runi_audio_source.spatial.blend"), static x => x.spatialBlend, static (x, value) => x.spatialBlend = value, 0, 1);
            DrawSliderPropertyLayout("_dopplerLevel", TrTempContent("runios-editor:inspector.runi_audio_source.spatial.doppler_level"), static x => x.dopplerLevel, static (x, value) => x.dopplerLevel = value, 0, 5);
            DrawSliderPropertyLayout("_spread", TrTempContent("runios-editor:inspector.runi_audio_source.spatial.spread"), static x => x.spread, static (x, value) => x.spread = value, 0, 360);

            DrawPropertyLayoutAndSync("_minDistance", TrTempContent("runios-editor:inspector.runi_audio_source.spatial.min_distance"), static x => x.minDistance, static (x, value) => x.minDistance = value);
            DrawPropertyLayoutAndSync("_maxDistance", TrTempContent("runios-editor:inspector.runi_audio_source.spatial.max_distance"), static x => x.maxDistance, static (x, value) => x.maxDistance = value);

            DrawAdditionalSpatialLayout();
        }

        protected virtual void DrawAdditionalSpatialLayout() { }

        protected virtual void DrawTransportLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.transport.header"), EditorStyles.boldLabel);
            playableController.DrawLayout();
        }

        protected virtual void DrawInformationLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.information.header"), EditorStyles.boldLabel);
            DrawReadOnlyValue
            (
                TrTempContent("runios-editor:inspector.runi_audio_source.information.length"),
                static x => x.length,
                static (label, value) => { EditorGUILayout.DoubleField(label, value); }
            );

            DrawAdditionalInformationLayout();
        }

        protected virtual void DrawAdditionalInformationLayout() { }

        protected void DrawPropertyLayoutAndSync<TValue>
        (
            string propertyName,
            GUIContent label,
            Func<TTarget, TValue> readFunc,
            Action<TTarget, TValue> writeFunc
        )
        {
            EditorGUI.BeginChangeCheck();
            SerializedProperty? property = DrawPropertyLayout(propertyName, label);
            if (EditorGUI.EndChangeCheck() && property != null)
                SyncSerializedValue(readFunc, writeFunc);
        }

        protected void DrawNullableDoublePropertyLayoutAndSync
        (
            string propertyName,
            GUIContent label,
            Func<TTarget, double> readFunc,
            Action<TTarget, double> writeFunc
        )
        {
            SerializedProperty? property = GetProperty(propertyName);
            if (property == null)
            {
                GUILayout.Label(GetTextOrKey("runios-editor:inspector.property_none").Replace("{name}", propertyName));
                return;
            }

            Rect position = EditorGUILayout.GetControlRect(true, RuniFields.GetMultiColumnsFieldHeight(label));
            bool oldMixedValue = EditorGUI.showMixedValue;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            double? value = property.doubleValue.Approximately(double.MaxValue) ? null : property.doubleValue;
            value = RuniFields.NullablePrimitiveField
            (
                position,
                label,
                value,
                GetTextOrKey("runios-editor:gui.none")
            );

            if (EditorGUI.EndChangeCheck())
            {
                property.doubleValue = value ?? double.MaxValue;
                serializedObject.ApplyModifiedProperties();
                SyncSerializedValue(readFunc, writeFunc);
            }

            EditorGUI.EndProperty();
            EditorGUI.showMixedValue = oldMixedValue;
        }

        protected void DrawSliderPropertyLayout
        (
            string propertyName,
            GUIContent label,
            Func<TTarget, float> readFunc,
            Action<TTarget, float> writeFunc,
            float sliderMin,
            float sliderMax
        )
        {
            SerializedProperty? property = GetProperty(propertyName);
            if (property == null)
            {
                GUILayout.Label(GetTextOrKey("runios-editor:inspector.property_none").Replace("{name}", propertyName));
                return;
            }

            Rect position = EditorGUILayout.GetControlRect();
            bool oldMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            float value = DrawUnclampedSlider(position, label, property.floatValue, sliderMin, sliderMax);
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedProperties();

                SyncSerializedValue(readFunc, writeFunc);
            }

            EditorGUI.EndProperty();
            EditorGUI.showMixedValue = oldMixedValue;
        }

        protected void DrawReadOnlyValue<TValue>
        (
            GUIContent label,
            Func<TTarget, TValue> readFunc,
            Action<GUIContent, TValue> drawAction
        )
        {
            TTarget? firstTarget = null;
            foreach (TTarget? item in targets)
            {
                if (item != null)
                {
                    firstTarget = item;
                    break;
                }
            }

            if (firstTarget == null)
                return;

            bool oldMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = !HasSameValue(readFunc);

            EditorGUI.BeginDisabledGroup(true);
            drawAction.Invoke(label, readFunc.Invoke(firstTarget));
            EditorGUI.EndDisabledGroup();

            EditorGUI.showMixedValue = oldMixedValue;
        }

        void SyncSerializedValue<TValue>(Func<TTarget, TValue> readFunc, Action<TTarget, TValue> writeFunc)
        {
            foreach (TTarget? item in targets)
            {
                if (item != null)
                    writeFunc.Invoke(item, readFunc.Invoke(item));
            }
        }

        static float DrawUnclampedSlider(Rect position, GUIContent label, float value, float sliderMin, float sliderMax)
        {
            Rect contentPosition = GetPrefixLabelRect(position, label, out Rect? labelPosition);

            if (labelPosition.HasValue)
                GUI.Label(labelPosition.Value, label);

            const float spacing = 4;
            if (contentPosition.width <= spacing)
                return EditorGUI.FloatField(contentPosition, value);

            float fieldWidth = Mathf.Min(Mathf.Max(EditorGUIUtility.fieldWidth, 50), contentPosition.width * 0.45f);

            Rect sliderPosition = contentPosition;
            sliderPosition.width -= fieldWidth + spacing;

            Rect fieldPosition = contentPosition;
            fieldPosition.x = sliderPosition.xMax + spacing;
            fieldPosition.width = fieldWidth;

            float result = value;

            EditorGUI.BeginChangeCheck();
            float sliderDisplayValue = float.IsNaN(value) ? sliderMin : value.Clamp(sliderMin, sliderMax);
            float sliderValue = GUI.HorizontalSlider(sliderPosition, sliderDisplayValue, sliderMin, sliderMax);
            if (EditorGUI.EndChangeCheck())
                result = sliderValue;

            EditorGUI.BeginChangeCheck();
            float fieldValue = EditorGUI.FloatField(fieldPosition, result);
            if (EditorGUI.EndChangeCheck())
                result = fieldValue;

            return result;
        }

        void SetLoopRange(ILoopablePlayer loopablePlayer, double loopStart, double loopEnd)
        {
            if (loopablePlayer is not TTarget item)
            {
                loopablePlayer.loopStart = loopStart;
                loopablePlayer.loopEnd = loopEnd;
                return;
            }

            SerializedProperty? loopStartProperty = GetProperty("_loopStart");
            SerializedProperty? loopEndProperty = GetProperty("_loopEnd");
            if (loopStartProperty == null || loopEndProperty == null)
                return;

            loopStartProperty.doubleValue = loopStart;
            loopEndProperty.doubleValue = loopEnd;
            serializedObject.ApplyModifiedProperties();

            double serializedLoopStart = item.loopStart;
            double serializedLoopEnd = item.loopEnd;
            item.loopStart = serializedLoopStart;
            item.loopEnd = serializedLoopEnd;
        }

    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(RuniAudioSource), true)]
    class RuniAudioSourceDefaultEditor : RuniAudioSourceEditor<RuniAudioSource>;
}
