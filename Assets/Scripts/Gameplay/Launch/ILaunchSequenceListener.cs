namespace Car3C.Gameplay.Launch
{
    /// <summary>
    /// RocketLaunchSequence 이벤트 구독자. 연출(화면·사운드·HUD)은 이 인터페이스로만 발사 로직을 듣는다.
    /// </summary>
    public interface ILaunchSequenceListener
    {
        void OnLaunchPhaseChanged(LaunchPhase phase);
        void OnCountdownTick(int secondsLeft);
    }
}
