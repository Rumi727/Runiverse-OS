#nullable enable
using System;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    public class MethodElement : MemberElement, IInspectorActionElement
    {
        public MethodElement(Type targetType, MethodInfo method) : this(new InspectableObject(targetType), method) { }
        public MethodElement(object instance, MethodInfo method) : this(new InspectableObject(instance), method) { } 
        public MethodElement(InspectableObject inspectable, MethodInfo method) : base(inspectable, method)
        {
            this.method = method;
            if (method.ReturnParameter != null)
                returnNullabilityInfo = new NullabilityInfoContext().Create(method.ReturnParameter);
        }

        public Type returnType => method.ReturnType;
        public RuniNullabilityInfo? returnNullabilityInfo { get; }
        
        public MethodInfo method { get; }

        public override bool isPublic => method.IsPublic;
        
        public override bool isStatic => method.IsStatic;

        public void Execute(object?[] parameters)
        {
            foreach (var instance in inspectable.instances)
            {
                if (instance == null)
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
    }
}