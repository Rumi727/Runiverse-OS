#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor
{
    /// <summary>
    /// RuniOS 설정을 위한 추상화된 ScriptableObject 클래스입니다.<br/>
    /// 이 클래스를 상속받아 설정을 정의하고 관리할 수 있습니다.<br/>
    /// 싱글톤 패턴으로 인스턴스를 관리하며, 에디터에서 쉽게 접근하고 수정할 수 있도록 설계되었습니다.
    /// </summary>
    /// <typeparam name="T">이 클래스를 상속받는 실제 파생 클래스의 타입입니다. 재귀적 제네릭 제약 조건을 사용합니다.</typeparam>
    public abstract class RuniOSConfigObject<T> : ScriptableObject where T : RuniOSConfigObject<T>
    {
        /// <summary>
        /// 싱글톤 인스턴스를 가져옵니다.<br/>
        /// 에셋 폴더 내 "Assets/Runiverse OS/{T}.asset" 경로에 에셋이 없다면 새로 생성합니다.
        /// </summary>
        public static T instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                RuniPath assetFolderPath = "Assets/Runiverse OS";
                string assetPath = (assetFolderPath + typeof(T).Name).AddExtension(".asset");

                T? scriptableObject = AssetDatabase.LoadAssetAtPath<T>(assetPath) ? AssetDatabase.LoadAssetAtPath<T>(assetPath) : CreateInstance<T>();

                if (!AssetDatabase.AssetPathExists(assetFolderPath))
                    AssetDatabase.CreateFolder("Assets", "Runiverse OS");

                if (!AssetDatabase.AssetPathExists(assetPath))
                    AssetDatabase.CreateAsset(scriptableObject, assetPath);

                return _instance = scriptableObject;
            }
        }
        static T? _instance;

        /// <summary>
        /// 이 객체가 변경되었음을 에디터에 알립니다.
        /// </summary>
        public new void SetDirty() => EditorUtility.SetDirty(this);
    }
}