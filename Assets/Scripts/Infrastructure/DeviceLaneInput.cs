using System.Collections.Generic;
using Car3C.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Car3C.Infrastructure
{
    /// <summary>
    /// 키보드(A/D, ←/→, Space/W/↑)와 포인터(터치 스와이프/탭, 마우스 클릭) 입력을 차선 변경·점프 요청으로 모은다.
    /// 위로 스와이프하면 점프, 좌우 스와이프나 화면 좌우 탭은 차선 변경.
    /// 요청은 큐에 쌓였다가 차량이 자기 Update에서 꺼내 쓴다.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public sealed class DeviceLaneInput : MonoBehaviour, ILaneInputSource, IJumpInputSource
    {
        private const int MaxBufferedRequests = 2;

        [SerializeField] private float _minSwipeDistanceRatio = 0.05f;

        private readonly Queue<int> _pending = new Queue<int>(MaxBufferedRequests);
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(4);
        private PointerEventData _pointerEventData;
        private bool _isPointerTracking;
        private float _pointerStartX;
        private float _pointerStartY;
        private bool _jumpRequested;
        private bool _viewToggleRequested;

        /// <summary>UI 버튼(JUMP) 등에서 점프를 요청한다.</summary>
        public void RequestJump()
        {
            _jumpRequested = true;
        }

        public bool TryConsumeJump()
        {
            if (!_jumpRequested) return false;
            _jumpRequested = false;
            return true;
        }

        /// <summary>UI 버튼(VIEW)에서 시점 전환(추적 ↔ 운전석)을 요청한다.</summary>
        public void RequestViewToggle()
        {
            _viewToggleRequested = true;
        }

        public bool TryConsumeViewToggle()
        {
            if (!_viewToggleRequested) return false;
            _viewToggleRequested = false;
            return true;
        }

        public bool TryConsumeLaneDelta(out int delta)
        {
            if (_pending.Count == 0)
            {
                delta = 0;
                return false;
            }

            delta = _pending.Dequeue();
            return true;
        }

        private void Update()
        {
            ReadKeyboard(Keyboard.current);
            ReadPointer(Pointer.current);
        }

        private void OnDisable()
        {
            _pending.Clear();
            _isPointerTracking = false;
            _jumpRequested = false;
            _viewToggleRequested = false;
        }

        private void ReadKeyboard(Keyboard keyboard)
        {
            if (keyboard == null) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Enqueue(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Enqueue(1);
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) _jumpRequested = true;
            if (keyboard.cKey.wasPressedThisFrame || keyboard.vKey.wasPressedThisFrame) _viewToggleRequested = true;
        }

        private void ReadPointer(Pointer pointer)
        {
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                Vector2 position = pointer.position.ReadValue();
                _isPointerTracking = !IsOverUi(position);
                _pointerStartX = position.x;
                _pointerStartY = position.y;
            }
            else if (_isPointerTracking && pointer.press.wasReleasedThisFrame)
            {
                _isPointerTracking = false;
                Vector2 end = pointer.position.ReadValue();
                float minSwipe = Screen.width * _minSwipeDistanceRatio;
                if (LaneGestureResolver.IsJumpSwipe(_pointerStartX, _pointerStartY, end.x, end.y, minSwipe)) _jumpRequested = true;
                else Enqueue(LaneGestureResolver.Resolve(_pointerStartX, end.x, Screen.width, minSwipe));
            }
        }

        /// <summary>버튼 등 UI 위를 누른 경우 차선 변경으로 해석하지 않는다.</summary>
        private bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            _pointerEventData ??= new PointerEventData(eventSystem);
            _pointerEventData.Reset();
            _pointerEventData.position = screenPosition;
            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _raycastResults);
            return _raycastResults.Count > 0;
        }

        private void Enqueue(int delta)
        {
            if (_pending.Count >= MaxBufferedRequests) return;
            _pending.Enqueue(delta);
        }
    }
}
