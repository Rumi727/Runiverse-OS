#nullable enable
using RuniOS.Reflection;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class MethodElement(InspectableObject inspectable, MethodInfo method) : MemberElement(inspectable, method), IInspectorActionElement
    {
        public MethodElement(Type targetType, MethodInfo method) : this(new InspectableObject(targetType), method) { }
        public MethodElement(object instance, MethodInfo method) : this(new InspectableObject(instance), method) { }

        public Type returnType => method.ReturnType;
        public Nullability? returnNullabilityInfo { get; } = Nullability.Create(method.ReturnParameter);

        public MethodInfo method { get; } = method;

        public override bool isPublic => method.IsPublic;
        
        public override bool isStatic => method.IsStatic;

        public void Execute(object?[] parameters)
        {
            var instances = inspectable.instances;
            for (int i = 0; i < instances.Count; i++)
            {
                object? instance = instances[i];
                if (instance.IsNull())
                    return;

                try
                {
                    method.Invoke(instance, parameters);
                }
                catch (Exception e)
                {
                    string memberName = method.Name;
                    string instanceType = instance.GetType().FullName ?? string.Empty;

                    throw new InspectorException($"An error occurred while trying to invoke method '{memberName}' on an instance of '{instanceType}'.", e);
                }
            }
        }
        
        public override bool HasFlags(InspectorFlags flags)
        {
            if (!base.HasFlags(flags))
                return false;

            if (!flags.HasFlagFast(InspectorFlags.Method))
                return false;
            
            if ((method.IsSpecialName || name.Contains('.')) && !flags.HasFlagFast(InspectorFlags.Hidden))
                return false;

            return true;
        }
        
        /// <inheritdoc cref="IInspectorActionElement.Clone"/>
        public override MemberElement Clone() => new MethodElement(inspectable.Clone(), method);
        IInspectorActionElement IInspectorActionElement.Clone() => new MethodElement(inspectable.Clone(), method);
    }
}