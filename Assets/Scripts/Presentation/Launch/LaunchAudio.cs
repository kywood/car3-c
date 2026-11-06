using Car3C.Gameplay.Launch;
using UnityEngine;

namespace Car3C.Presentation.Launch
{
    /// <summary>
    /// 발사 사운드 연결 지점. 클립이 비어 있으면 아무것도 재생하지 않으므로, 사운드 에셋을 나중에 넣기만 하면 된다.
    /// 소리는 발사장 위치(AudioSource)에서 3D로 재생한다.
    /// </summary>
    public sealed class LaunchAudio : MonoBehaviour, ILaunchSequenceListener
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _countdownBeep;
        [SerializeField] private AudioClip _ignition;
        [SerializeField] private AudioClip _liftoffRumble;

        public void OnCountdownTick(int secondsLeft)
        {
            Play(_countdownBeep);
        }

        public void OnLaunchPhaseChanged(LaunchPhase phase)
        {
            switch (phase)
            {
                case LaunchPhase.Ignition: Play(_ignition); break;
                case LaunchPhase.Ascent: Play(_liftoffRumble); break;
                case LaunchPhase.Idle:
                case LaunchPhase.Complete:
                    if (_source != null) _source.Stop();
                    break;
            }
        }

        private void Play(AudioClip clip)
        {
            if (_source == null || clip == null) return;
            _source.PlayOneShot(clip);
        }
    }
}
