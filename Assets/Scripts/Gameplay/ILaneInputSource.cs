namespace Car3C.Gameplay
{
    /// <summary>
    /// 차선 변경 입력 공급자. 키보드/터치 외에 향후 AI나 네트워크 입력으로 교체할 수 있도록 분리한다.
    /// </summary>
    public interface ILaneInputSource
    {
        /// <summary>이번 프레임의 차선 변경 요청(-1 왼쪽, +1 오른쪽)이 있으면 꺼내고 true를 반환한다.</summary>
        bool TryConsumeLaneDelta(out int delta);
    }
}
