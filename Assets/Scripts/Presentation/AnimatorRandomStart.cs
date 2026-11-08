using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 같은 동물이 여러 마리일 때 동작이 똑같이 맞지 않도록, 켜질 때 애니메이션 시작 시점과 속도를 조금씩 다르게 한다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorRandomStart : MonoBehaviour
    {
        [SerializeField] private Vector2 _speedRange = new Vector2(0.8f, 1.2f);

        private void OnEnable()
        {
            var animator = GetComponent<Animator>();
            if (animator.runtimeAnimatorController == null) return;
            animator.speed = Random.Range(_speedRange.x, _speedRange.y);
            animator.Play("Idle", 0, Random.value);
        }
    }
}
