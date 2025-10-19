#nullable enable
using System;

namespace RuniOS.Inspectors.Drawers
{
    [AttributeUsage(AttributeTargets.All)]
    public class CustomInspectorDrawerAttribute : Attribute
    {
        public Type targetType { get; }
        
        /// <summary>
        /// Overrides the priority of this drawer.<br/>
        /// If the value is 0, the priority according to the inheritance hierarchy is used.
        /// <br/><br/>
        /// 이 서랍의 우선순위를 재정의 합니다.<br/>
        /// 값이 0이면 상속 계층 구조에 따른 우선순위가 사용됩니다.
        /// </summary>
        public int priority { get; set; } = 0;
        
        public CustomInspectorDrawerAttribute(Type targetType) => this.targetType = targetType;
    }
}