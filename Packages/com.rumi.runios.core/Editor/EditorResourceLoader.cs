using Cysharp.Threading.Tasks;
using RuniOS.Resource;
using UnityEditor;

using Progress = Cysharp.Threading.Tasks.Progress;

namespace RuniOS.Editor
{
    /// <summary>
    /// 에디터에서 리소스를 미리 로딩시키는 클래스입니다.<br/>
    /// <see cref="ResourceManager.Reload"/>를 직접 사용해도 문제 없지만, 이 클래스를 통해 리로딩 하면 자동으로 상단 툴바 프로그레스바에 등록됩니다.
    /// </summary>
    public static class EditorResourceLoader
    {
        [InitializeOnLoadMethod]
        static async UniTaskVoid Initialize()
        {
            if (Kernel.isPlaying)
                return;
            
            await UniTask.DelayFrame(10);
            Reload().Forget();
        }

        public const string progressText = "internal.editor_resource_loader.loading";

        /// <summary>
        /// 리소스 레지스트리를 재로드 합니다.
        /// </summary>
        public static async UniTask Reload()
        {
            await ResourceManager.Reload
            (
                Progress.Create<float>
                (
                    x => SetProgress(typeof(EditorResourceLoader).FullName ?? nameof(EditorResourceLoader), x)
                )
            );
        }

        /// <summary>
        /// <see cref="ProgressInToolbar.SetProgress"/> 메소드랑 기능적으론 동일하지만, progressText 매개변수의 값이 <see cref="progressText"/> 상수로 설정됩니다.
        /// </summary>
        /// <param name="id">한 프로그레스 바에서 여러개의 진행도를 구분할 고유 id</param>
        /// <param name="value">0에서 1 사이의 진행도</param>
        public static void SetProgress(string id, float value) => ProgressInToolbar.SetProgress(progressText, id, value);
    }
}