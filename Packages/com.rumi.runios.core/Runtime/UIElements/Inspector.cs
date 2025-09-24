#nullable enable
using RuniOS.APIBridge.UnityEngine.UIElements;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.Inspectors.Drawers;
using RuniOS.Inspectors.Drawers.UIElements;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <remarks>
    /// 생성 후 Rebuild 메소드를 호출하여 빌드해야합니다
    /// </remarks>
    [UxmlElement]
    public partial class Inspector : BindableElement, IInspector
    {
        public const string ussClassName = "runios-inspector";
        public const string elementUssClassName = "runios-inspector-element";

        public static readonly BindingId inspectorFlagsProperty = nameof(inspectorFlags);
        public static readonly BindingId alignFieldsProperty = nameof(alignFields);
        public static readonly BindingId updateDelayMSProperty = nameof(updateDelayMS);

        /// <summary>
        /// 루트 인스펙터를 가져옵니다.
        /// </summary>
        public Inspector rootInspector { get; }

        /// <remarks>
        /// 변경 사항을 적용하려면 Rebuild 메소드를 호출하여 빌드해야합니다
        /// </remarks>
        [DisallowNull]
        public IInspectable? targetInspectable
        {
            get => _targetInspectable;
            set
            {
                if (targetInspectable == value)
                    return;

                _targetInspectable = value;
                _targetElement = null;
            }
        }
        IInspectable? _targetInspectable;
        
        /// <remarks>
        /// 변경 사항을 적용하려면 Rebuild 메소드를 호출하여 빌드해야합니다
        /// </remarks>
        [DisallowNull]
        public IInspectorElement? targetElement
        {
            get => _targetElement;
            set
            {
                if (targetElement == value)
                    return;

                _targetElement = value;
                _targetInspectable = null;
            }
        }
        IInspectorElement? _targetElement;

        public ImmutableArray<IInspectorElement> elements { get; private set; } = ImmutableArray<IInspectorElement>.Empty;
        public ImmutableArray<UIElementInspectorDrawer?> drawers { get; private set; } = ImmutableArray<UIElementInspectorDrawer?>.Empty;
        IEnumerable<InspectorDrawer?> IInspector.drawers => drawers.OfType<InspectorDrawer>();
        
        public ImmutableArray<VisualElement?> visualElements { get; private set; } = ImmutableArray<VisualElement?>.Empty;

        /// <remarks>
        /// 변경 사항을 적용하려면 Rebuild 메소드를 호출하여 빌드해야합니다
        /// </remarks>
        [UxmlAttribute]
        [CreateProperty]
        public InspectorFlags inspectorFlags
        {
            get => _inspectorFlags;
            set
            {
                if (_inspectorFlags == value)
                    return;

                _inspectorFlags = value;
                NotifyPropertyChanged(in inspectorFlagsProperty);
            }
        }
        InspectorFlags _inspectorFlags = InspectorFlags.All;

        /// <remarks>
        /// 변경 사항을 적용하려면 Rebuild 메소드를 호출하여 빌드해야합니다
        /// </remarks>
        [UxmlAttribute]
        [CreateProperty]
        public bool alignFields
        {
            get => _alignFields;
            set
            {
                if (_alignFields == value)
                    return;

                _alignFields = value;
                NotifyPropertyChanged(in alignFieldsProperty);
            }
        }
        bool _alignFields = false;

        [UxmlAttribute]
        [CreateProperty]
        public int updateDelayMS
        {
            get => _updateDelayMS;
            set
            {
                if (_updateDelayMS == value)
                    return;

                _updateDelayMS = value;
                NotifyPropertyChanged(in updateDelayMSProperty);

                updateSchedule.Every(updateDelayMS);
            }
        }
        int _updateDelayMS = 100;
        
        public IVisualElementScheduledItem updateSchedule { get; }

        public Inspector()
        {
            AddToClassList(ussClassName);
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);

            rootInspector = this;
            updateSchedule = schedule.Execute(Update).Every(updateDelayMS);
        }
        
        public Inspector(Inspector? rootInspector) : this()
        {
            if (rootInspector != null)
                this.rootInspector = rootInspector;
        }

        public Inspector(object instance) : this(new InspectableObject(instance)) { }
        public Inspector(Type type) : this(new InspectableObject(type)) { }
        public Inspector(Type type, params object[] instances) : this(new InspectableObject(type, instances)) { }
        public Inspector(Type type, IEnumerable<object> instances) : this(new InspectableObject(type, instances)) { }

        public Inspector(IInspectable inspectable) : this() => targetInspectable = inspectable;
        public Inspector(IInspectorElement inspectorElement) : this() => targetElement = inspectorElement;

        Action? readAction;
        readonly ConditionalWeakTable<VisualElement, AlginLabelInfo> alginLabelInfos = new();

        public void Rebuild()
        {
            hierarchy.Clear();
            readAction = null;

            if (targetElement != null)
            {
                VisualElement? visualElement = ElementToVisualElement(targetElement, out UIElementInspectorDrawer? drawer);
                hierarchy.Add(visualElement);
                
                elements = ImmutableArray.Create(targetElement);
                drawers = ImmutableArray.Create(drawer);
                visualElements = ImmutableArray.Create(visualElement);
                
                return;
            }
            
            if (targetInspectable == null)
                return;
            
            if (targetInspectable is IInspectableList inspectableList)
            {
                VisualElement visualElement;
                
                try
                {
                    ListInspectorDrawer drawer = new ListInspectorDrawer(inspectableList, rootInspector);
                    visualElement = drawer.Build();

                    DrawerBind(null, visualElement, drawer);
                    
                    hierarchy.Add(visualElement);
                
                    elements = ImmutableArray<IInspectorElement>.Empty;
                    drawers = ImmutableArray.Create<UIElementInspectorDrawer?>(drawer);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    hierarchy.Add(new LabelField(targetInspectable.inspectionDisplayName, e.ToString()));

                    return;
                }
                
                visualElements = ImmutableArray.Create<VisualElement?>(visualElement);
            }
            else
            {
                try
                {
                    elements = targetInspectable.GetElements(inspectorFlags);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    hierarchy.Add(new LabelField(targetInspectable.inspectionDisplayName, e.ToString()));

                    return;
                }

                UIElementInspectorDrawer?[] drawers = new UIElementInspectorDrawer[elements.Length];
                VisualElement?[] visualElements = new VisualElement[elements.Length];
                for (int i = 0; i < elements.Length; i++)
                {
                    IInspectorElement element = elements[i];
                    
                    VisualElement? visualElement = ElementToVisualElement(element, out UIElementInspectorDrawer? drawer);
                    hierarchy.Add(visualElement);
                    
                    drawers[i] = drawer;
                    visualElements[i] = visualElement;
                }
                
                this.drawers = drawers.ToImmutableArray();
                this.visualElements = visualElements.ToImmutableArray();
            }
            
            return;
            
            VisualElement? ElementToVisualElement(IInspectorElement element, out UIElementInspectorDrawer? drawer)
            {
                drawer = null;
                if (element is not IInspectorVariableElement variableElement)
                    return null;

                try
                {
                    drawer = UIElementInspectorDrawer.FindDrawer(variableElement);
                    if (drawer == null)
                        return null;

                    VisualElement visualElement = drawer.Build();
                    if (visualElement is Foldout foldout)
                        foldout.text = element.displayName;
                    else if (visualElement is BaseListView listView)
                        listView.headerTitle = element.displayName;
                    else if (IPrefixLabelBridge.__targetType.IsInstanceOfType(visualElement))
                        IPrefixLabelBridge.__GetInstanceFrom(visualElement).SetLabel(element.displayName);
                    
                    DrawerBind(element, visualElement, drawer);
                    
                    if (alignFields)
                    {
                        visualElement.Query<VisualElement>(null, BaseField<int>.ussClassName)
                            .ForEach(static x => x.AddToClassList(BaseField<int>.alignedFieldUssClassName));

                        visualElement.RegisterCallback<GeometryChangedEvent>(OnInspectorFieldGeometryChanged);
                        visualElement.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
                    }

                    return visualElement;
                }
                catch (Exception e)
                {
                    return new LabelField(element.name, e.ToString());
                }
            }
            
            void DrawerBind(IInspectorElement? element, VisualElement visualElement, UIElementInspectorDrawer drawer)
            {
#if UNITY_EDITOR
                if (drawer is not ObjectInspectorDrawer && element is IInspectorSerializedPropertyElement serializedPropertyElement && visualElement is IBindable bindable)
                {
                    bindable.bindingPath = serializedPropertyElement.property.propertyPath;
                    UnityEditor.UIElements.BindingExtensions.BindProperty(bindable, serializedPropertyElement.property);
                        
                    visualElement.Q<VisualElement>(BaseField<int>.inputUssClassName)?.AddToClassList(UnityEditor.UIElements.PropertyField.inputUssClassName);
                }
                else
#endif
                {
                    drawer.Bind(visualElement, out Action? readAction);
                    this.readAction += readAction;
                }
            }
        }
        
        void OnInspectorFieldGeometryChanged(GeometryChangedEvent e)
        {
            if (e.target is VisualElement element)
                AlignLabel(element);
        }

        public void Update() => readAction?.Invoke();
        
        void AlignLabel(VisualElement element)
        {
            if (!IPrefixLabelBridge.__targetType.IsInstanceOfType(element))
                return;
            
            IPrefixLabelBridge prefixLabel = IPrefixLabelBridge.__GetInstanceFrom(element);
            AlginLabelInfo alginLabelInfo = alginLabelInfos.GetOrCreateValue(element);
            
            float labelExtraPadding = alginLabelInfo.labelExtraPadding;
            float labelBaseMinWidth = alginLabelInfo.labelBaseMinWidth;
            float labelWidthRatio = alginLabelInfo.labelWidthRatio;
            
            float num1 = (element.worldBound.x - rootInspector.worldBound.x) - rootInspector.resolvedStyle.paddingLeft;
            
            float minWidth = labelBaseMinWidth - num1 - resolvedStyle.paddingLeft;
            prefixLabel.labelElement.style.minWidth = minWidth.Clamp(0);
            
            float num2 = labelExtraPadding + num1 + resolvedStyle.paddingLeft;
            float width = (rootInspector.resolvedStyle.width * labelWidthRatio).Ceil() - num2;
            
            prefixLabel.labelElement.style.width = width.Clamp(0);
        }
        
        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.target is not VisualElement element)
                return;

            AlginLabelInfo alginLabelInfo = alginLabelInfos.GetOrCreateValue(element);
            
            {
                if (evt.customStyle.TryGetValue(new CustomStyleProperty<float>("--unity-property-field-label-width-ratio"), out float value))
                    alginLabelInfo.labelWidthRatio = value;
            }

            {
                if (evt.customStyle.TryGetValue(new CustomStyleProperty<float>("--unity-property-field-label-extra-padding"), out float value))
                    alginLabelInfo.labelExtraPadding = value;
            }

            {
                if (evt.customStyle.TryGetValue(new CustomStyleProperty<float>("--unity-property-field-label-base-min-width"), out float value))
                    alginLabelInfo.labelBaseMinWidth = value;
            }

            AlignLabel(element);
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        class AlginLabelInfo
        {
            public float labelWidthRatio = 0.45f;
            public float labelExtraPadding = 37;
            public float labelBaseMinWidth = 123;
        }
    }
}