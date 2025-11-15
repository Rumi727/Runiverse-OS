#nullable enable
namespace RuniOS
{
    public static partial class Kernel
    {
        public static float fps { get; private set; } = 60;

        public static float deltaTime { get; private set; } = fps60Second;

        public static double deltaTimeDouble { get; private set; } = fps60Second;

        public static float smoothDeltaTime { get; private set; } = fps60Second;

        public static float unscaledDeltaTime { get; private set; } = fps60Second;

        public static double unscaledDeltaTimeDouble { get; private set; } = fps60Second;

        public static float unscaledSmoothDeltaTime { get; private set; } = fps60Second;

        public static float fixedDeltaTime
        {
            get => Time.fixedDeltaTime;
            set => Time.fixedDeltaTime = value;
        }

        public const float fps60Second = 1f / 60f;

        static void TimeUpdate()
        {
            double realDeltaTime = deltaTimeStopwatch.Elapsed.TotalSeconds;
            deltaTimeStopwatch.Restart();

            float timeScale = Time.timeScale;

            //유니티의 내장 변수들은 테스트 결과, 약간의 성능을 더 먹는것으로 확인되었기 때문에
            //이렇게 관리 스크립트가 변수를 할당하고 다른 스크립트가 그 변수를 가져오는것이 성능에 더 도움 되는것을 확인하였습니다
            deltaTime = (float)realDeltaTime * timeScale;
            deltaTimeDouble = realDeltaTime * timeScale;
            unscaledDeltaTime = (float)realDeltaTime;
            unscaledDeltaTimeDouble = realDeltaTime;

            fps = 1f / unscaledDeltaTime;

            //Smooth Delta Time
            //테스트 결과 유니티 내부 구현이랑 100% 일치하진 않지만 오차를 무시할 수 있을 만큼 일치합니다
            {
                unscaledSmoothDeltaTime = (0.2f * unscaledDeltaTime) + ((1 - 0.2f) * unscaledSmoothDeltaTime);
                smoothDeltaTime = unscaledDeltaTime * timeScale;
            }
        }
    }
}