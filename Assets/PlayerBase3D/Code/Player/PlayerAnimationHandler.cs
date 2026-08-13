using UnityEngine;
using nakatimat.Animation;

namespace nakatimat.Player
{
    public class PlayerAnimationHandler : MonoBehaviour
    {
        [SerializeField] private AnimationBrain _brain;

        [Header("State Names (Animator Controller)")]
        [SerializeField] private string _idleState = "Idle";
        [SerializeField] private string _walkState = "Walk";
        [SerializeField] private string _runState = "Run";
        [SerializeField] private string _crouchState = "Crouch";
        [SerializeField] private string _crouchWalkState = "CrouchWalk";
        [SerializeField] private string _jumpState = "Jump";
        [SerializeField] private string _fallState = "Fall";
        [SerializeField] private string _landState = "Land";

        [Header("Configuration")]
        [SerializeField] private float _walkSpeedThreshold = 0.1f;
        [Tooltip("Above this speed it enters Run instead of Walk. Tune alongside PlayerController's _walkSpeed/_sprintSpeed.")]
        [SerializeField] private float _runSpeedThreshold = 4.5f;
        [SerializeField] private float _transitionDuration = 0.15f;
        [Tooltip("Blend used specifically when settling into Idle/Crouch — shorter, so stopping doesn't \"drag\".")]
        [SerializeField] private float _stopTransitionDuration = 0.08f;
        [Tooltip("How long the landing state plays before returning to locomotion.")]
        [SerializeField] private float _landDuration = 0.2f;

        private bool _isGrounded = true;
        private bool _isJumping;
        private bool _isCrouching;
        private float _speed;
        private float _landingTimer;

        private void Awake()
        {
            if (_brain == null) { _brain = GetComponent<AnimationBrain>(); }
        }

        public void UpdateGrounded(bool isGrounded)
        {
            if (isGrounded && !_isGrounded) { _landingTimer = _landDuration; }
            _isGrounded = isGrounded;
        }

        public void UpdateJumped(bool isJumping)
        {
            _isJumping = isJumping;
        }

        public void UpdateLocomotion(float speed, bool isCrouching)
        {
            _speed = speed;
            _isCrouching = isCrouching;
            Evaluate();
        }

        private void Evaluate()
        {
            if (_brain == null) { return; }

            if (!_isGrounded)
            {
                _landingTimer = 0f;
                _brain.CrossFade(_isJumping ? _jumpState : _fallState, _transitionDuration);
                return;
            }

            if (_landingTimer > 0f)
            {
                _landingTimer -= Time.deltaTime;
                _brain.CrossFade(_landState, _transitionDuration);
                return;
            }

            bool isMoving = _speed > _walkSpeedThreshold;
            bool isRunning = _speed > _runSpeedThreshold;

            string targetState;
            if (_isCrouching)
            {
                targetState = isMoving ? _crouchWalkState : _crouchState;
            }
            else if (isRunning)
            {
                targetState = _runState;
            }
            else
            {
                targetState = isMoving ? _walkState : _idleState;
            }

            float duration = isMoving ? _transitionDuration : _stopTransitionDuration;
            _brain.CrossFade(targetState, duration);
        }
    }
}
