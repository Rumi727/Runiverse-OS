#nullable enable
namespace RuniOS.Inspectors;

[Flags]
public enum InspectorFlags
{
    /// <summary>
    /// 아무것도 표시하지 않습니다.
    /// </summary>
    None = 0,
        
    /// <summary>
    /// 공개 멤버를 엑세스 가능하게 합니다.
    /// </summary>
    Public = 1 << 0,
        
    /// <summary>
    /// 비공개 멤버를 엑세스 가능하게 합니다.
    /// </summary>
    NonPublic = 1 << 1,
        
    /// <summary>
    /// 정적 멤버를 엑세스 가능하게 합니다.
    /// </summary>
    Static = 1 << 2,
        
    /// <summary>
    /// 비정적 멤버를 엑세스 가능하게 합니다.
    /// </summary>
    Instance = 1 << 3,
        
    /// <summary>
    /// 읽기 전용 멤버를 엑세스 가능하게 합니다.
    /// </summary>
    ReadOnly = 1 << 4,
        
    /// <summary>
    /// 쓰기 전용 멤버를 엑세스 가능하게 합니다.
    /// </summary>
    WriteOnly = 1 << 5,
        
    /// <summary>
    /// 공개적으로 엑세스할 수 있는 모든 멤버를 엑세스 가능하게 합니다.
    /// </summary>
    PublicAccess = Public | Static | Instance | ReadOnly | WriteOnly,
        
    /// <summary>
    /// 공개되지 않는 멤버를 포함한 엑세스할 수 있는 모든 멤버를 엑세스 가능하게 합니다.
    /// </summary>
    Access = PublicAccess | NonPublic,
        
    /// <summary>
    /// 엑세스 가능한 프로퍼티를 표시합니다.
    /// </summary>
    Property = 1 << 10,
        
    /// <summary>
    /// 엑세스 가능한 이벤트를 표시합니다. 
    /// </summary>
    Event = 1 << 11,
        
    /// <summary>
    /// 엑세스 가능한 필드를 표시합니다.
    /// </summary>
    Field = 1 << 12,
        
    /// <summary>
    /// 엑세스 가능한 메서드를 표시합니다.
    /// </summary>
    Method = 1 << 13,
        
    /// <summary>
    /// 엑세스 가능한 변수를 (값이 변하는) 모든 멤버를 표시합니다.
    /// </summary>
    Variable = Property | Event | Field,
        
    /// <summary>
    /// 엑세스 가능한 메소드를 포함한 모든 멤버를 표시합니다.
    /// </summary>
    Member = Variable | Method,
        
    /// <summary>
    /// 엑세스 가능한 리스트를 표시합니다.
    /// </summary>
    List = 1 << 20,
        
    /// <summary>
    /// 엑세스 가능한 숨겨진 멤버를 표시합니다. (C#에선 컴파일러가 생성한 멤버)
    /// </summary>
    Hidden = 1 << 30,
        
    /// <summary>
    /// 인스펙터를 디버깅 모드로 표시합니다.
    /// </summary>
    Debug = 1 << 31,
        
    /// <summary>
    /// 모든 메소드를 포함한 멤버를 표시합니다.
    /// </summary>
    All = -1 & ~Debug
}