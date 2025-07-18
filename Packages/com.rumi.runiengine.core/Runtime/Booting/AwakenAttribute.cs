#nullable enable
using System;

namespace RuniEngine.Booting
{
    /// <summary>
    /// 프로젝트 설정이 로드되기 전에 메소드를 호출 시켜주는 어트리뷰트 입니다
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AwakenAttribute : Attribute { }
}
