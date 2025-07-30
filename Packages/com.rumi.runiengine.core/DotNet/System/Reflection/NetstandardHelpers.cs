#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Reflection/NullabilityInfoContext.cs
using RuniOS;

#pragma warning disable
// ReSharper disable all
namespace System.Reflection
{
    public static class NetstandardHelpers
    {
        public static MemberInfo GetMemberWithSameMetadataDefinitionAs(this Type type, MemberInfo member)
        {
            ExceptionUtility.ThrowIfArgumentNull(member);
 
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            foreach (MemberInfo myMemberInfo in type.GetMembers(all))
            {
                if (myMemberInfo.HasSameMetadataDefinitionAs(member))
                {
                    return myMemberInfo;
                }
            }
 
            throw new MissingMemberException(type.FullName, member.Name);
        }

        static bool HasSameMetadataDefinitionAs(this MemberInfo info, MemberInfo other)
        {
            if (info.MetadataToken != other.MetadataToken)
                return false;
 
            if (!info.Module.Equals(other.Module))
                return false;
 
            return true;
        }
 
        public static bool IsGenericMethodParameter(this Type type)
            => type.IsGenericParameter && type.DeclaringMethod is not null;
 
        public static ReadOnlySpan<ParameterInfo> GetParametersAsSpan(this MethodBase metaMethod)
            => metaMethod.GetParameters();
    }
}