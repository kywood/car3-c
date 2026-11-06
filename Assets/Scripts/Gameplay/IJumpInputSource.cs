namespace Car3C.Gameplay
{
    /// <summary>점프 입력 공급자(키보드·터치, 향후 네트워크 입력으로 교체 가능).</summary>
    public interface IJumpInputSource
    {
        /// <summary>이번 프레임에 점프 요청이 있었으면 꺼내고 true.</summary>
        bool TryConsumeJump();
    }
}
