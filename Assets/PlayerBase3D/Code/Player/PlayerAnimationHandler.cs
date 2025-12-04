using UnityEngine;

namespace nakatimat.Player
{
    public class PlayerAnimationHandler : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private int CurrentSpeedHash = Animator.StringToHash("CurrentSpeed");
        private int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private int IsJumpingHash = Animator.StringToHash("IsJumping");


        private void Awake()
        {
            if(_animator == null) { _animator = GetComponent<Animator>(); }
        }
        
        public void UpdateLocomotion(float speed, bool isCrouching)
        {
            _animator.SetFloat(CurrentSpeedHash, speed);
            _animator.SetBool(IsCrouchingHash , isCrouching);
        }

        public void UpdateGrounded(bool isGrounded)
        {
            _animator.SetBool(IsGroundedHash, isGrounded);
        }

        public void UpdateJumped(bool IsJumping)
        {
            _animator.SetBool(IsJumpingHash, IsJumping);
        }
    }
}

