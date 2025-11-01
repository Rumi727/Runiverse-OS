#nullable enable
using Newtonsoft.Json;
using RuniOS.Booting;
using RuniOS.Json.Converters;
using RuniOS.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace RuniOS.Json
{
    /// <summary>
    /// Json.NET 직렬화 및 역직렬화를 관리하는 정적 클래스입니다.
    /// <br/>
    /// 애플리케이션 전반에 걸쳐 사용되는 기본 <see cref="JsonSerializerSettings"/>를 설정하고,
    /// <see cref="RuniOS"/>에서 정의된 커스텀 <see cref="JsonConverter"/>들을 관리합니다.
    /// </summary>
    public static class JsonManager
    {
        static readonly JsonConverter[] _runiConverts = new JsonConverter[] { new SerializableNullableConverter() };
        
        /// <summary>
        /// <see cref="RuniOS"/>에서 기본적으로 사용되는 읽기 전용 <see cref="JsonConverter"/> 목록입니다.
        /// <br/>
        /// 이 컨버터들은 <see cref="AwakenAttribute"/> 단계에서 전역 <see cref="JsonSerializerSettings"/>에 추가됩니다.
        /// </summary>
        public static IReadOnlyList<JsonConverter> runiConverts { get; } = _runiConverts.AsReadOnly();
        
        [Awaken]
        [Preserve]
        static void Awaken()
        {
            // 현재 기본 설정이 있으면 가져오고, 없으면 새로운 설정을 생성합니다.
            JsonSerializerSettings settings = JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();
            
            // 기존 컨버터 목록과 runiConverts 목록을 합쳐서 새로운 컨버터 목록으로 설정합니다.
            // .Union() 메서드를 사용하여 중복 컨버터 추가를 방지합니다.
            settings.Converters = settings.Converters.Union(runiConverts).ToList();
            
            // 업데이트된 설정을 Json.NET의 기본 설정으로 지정합니다.
            JsonConvert.DefaultSettings = () => settings;
        }
    }
}