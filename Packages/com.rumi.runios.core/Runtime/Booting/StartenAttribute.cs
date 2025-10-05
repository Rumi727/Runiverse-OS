#nullable enable
using System;

namespace RuniOS.Booting
{
    /// <summary>
    /// 리소스 레지스트리가 로드된 후 메소드를 호출 시켜주는 어트리뷰트 입니다<br/>
    /// <see cref="BootLoader"/> 클래스에서 호출됩니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class StartenAttribute : Attribute { }
}
