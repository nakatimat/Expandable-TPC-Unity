using UnityEngine;
using nakatimat.Animation;

namespace nakatimat.Player
{
    public class PlayerAnimationHandler : MonoBehaviour
    {
        [SerializeField] private AnimationBrain _brain;

        [Header("Nomes dos Estados (Animator Controller)")]
        [SerializeField] private string _idleState = "Idle";
        [SerializeField] private string _walkState = "Walk";
        [SerializeField] private string _runState = "Run";
        [SerializeField] private string _crouchState = "Crouch";
        [SerializeField] private string _crouchWalkState = "CrouchWalk";
        [SerializeField] private string _jumpState = "Jump";
        [SerializeField] private string _fallState = "Fall";
        [SerializeField] private string _landState = "Land";

        [Header("Configuracao")]
        [SerializeField] private float _walkSpeedThreshold = 0.1f;
        [Tooltip("Acima desse valor de speed entra em Run em vez de Walk. Ajuste conforme _walkSpeed/_sprintSpeed do PlayerController.")]
        [SerializeField] private float _runSpeedThreshold = 4.5f;
        [SerializeField] private float _transitionDuration = 0.15f;
        [Tooltip("Blend usado especificamente ao entrar em Idle/Crouch parado — mais curto pra parar sem 'arrastar'.")]
        [SerializeField] private float _stopTransitionDuration = 0.08f;
        [Tooltip("Quanto tempo o estado de pouso fica tocando antes de voltar pra locomocao.")]
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
