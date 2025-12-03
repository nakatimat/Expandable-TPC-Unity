using UnityEngine;
using nakatimat.InputSystem;

namespace nakatimat.Player 
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Ref")]
        private InputManager input;
        private CharacterController _charController;
        [SerializeField] private PlayerAnimationHandler _playerAnimationHandler;
        private Transform _camera;

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _crouchSpeed = 2f;
        [SerializeField] private float _sprintSpeed = 6f;
        [SerializeField] private float _rotationSmoothing = 10f;
        [SerializeField] private float _speedChangeDamping = 10f;

        [Header("Jump & Gravity")]
        [SerializeField] private float _jumpForce = 7f;
        [SerializeField] private float _gravityMultiplayer = 2f;
        [SerializeField] private float _groundedOffset = -0.1f;
        [SerializeField] private float _groundCheckRadius = 0.1f;
        [SerializeField] private LayerMask _groundLayerMask;



        [Header("Runtime Variables")]
        private Vector3 _moveDirection;
        private float _currentSpeed;
        private float _verticalVelocity;
        [SerializeField] private bool _isSprint;
        [SerializeField] private bool _isGrounded;
        [SerializeField] private bool _isCrouching;
        [SerializeField] private bool _isJumping;

        void Start()
        {
            CursorBehaviour();
            input = InputManager.Input;
            SubscribeInput(true);

            _charController = GetComponent<CharacterController>();
            _camera = Camera.main.transform;

            if(_playerAnimationHandler == null) { _playerAnimationHandler = GetComponent<PlayerAnimationHandler>(); }
        }

        private void OnDisable()
        {
            SubscribeInput(false);
        }

        void Update()
        {
            HandleInput();
            HandleMovement();
            UpdateSpeedState();
            GroundCheck();
            ApplyGravity();
            //HandleJumpAndFall();
            HandleAnimations();
        }

        private void SubscribeInput(bool subscribe)
        {
            if (input == null) { return; }
            if (subscribe == true)
            {
                input.onJumpPerformed += Jump;
                input.onAttackPerformed += Attack;
                input.onSprintPerformed += SprintPerformed;
                input.onSprintCanceled += SprintCanceled;
                input.onCrouchToggle += OnCrouchToggle;
            }
            else
            {
                input.onJumpPerformed -= Jump;
                input.onAttackPerformed -= Attack;
                input.onSprintPerformed -= SprintPerformed;
                input.onSprintCanceled -= SprintCanceled;
                input.onCrouchToggle -= OnCrouchToggle;
            }

        }

        void HandleInput()
        {
            _moveDirection = GetCameraRelativeDirection(input.GetAxis());
        }

        private Vector3 GetCameraRelativeDirection(Vector2 axis)
        {
            Vector3 camForward = _camera.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = _camera.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 desired = (camForward * axis.y) + (camRight * axis.x);
            if (desired.sqrMagnitude > 1f)
            {
                desired.Normalize();
            }

            return desired;
        }

        private void HandleMovement()
        {
            Vector3 moveXZ = _moveDirection * _currentSpeed;
            Vector3 velocity = moveXZ + Vector3.up * _verticalVelocity;

            _charController.Move(velocity * Time.deltaTime);

            bool shouldRotate = new Vector2(_moveDirection.x, _moveDirection.z).magnitude > 0.1f;
            if (shouldRotate == true)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSmoothing * Time.deltaTime);
            }
        }

        private void UpdateSpeedState()
        {
            float inputMagnitude = new Vector2(_moveDirection.x, _moveDirection.z).magnitude;

            if(_isCrouching == true && inputMagnitude > 0.01f)
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, _crouchSpeed, _speedChangeDamping * Time.deltaTime);
            }
            if (_isSprint == true && inputMagnitude > 0.01f)
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, _sprintSpeed, _speedChangeDamping * Time.deltaTime);
            }
            else if (inputMagnitude > 0.01f)
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, _walkSpeed, _speedChangeDamping * Time.deltaTime);
            }
            else
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0, _speedChangeDamping * Time.deltaTime);
            }
        }

        private void GroundCheck()
        {
            Vector3 spherePos = transform.position + Vector3.up * _groundedOffset;
            _isGrounded = Physics.CheckSphere(spherePos, _groundCheckRadius, _groundLayerMask, QueryTriggerInteraction.Ignore) == true;
        }

        private void ApplyGravity()
        {
            if (_isGrounded == true && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            _verticalVelocity += Physics.gravity.y * _gravityMultiplayer * Time.deltaTime;
        }

        private void HandleJumpAndFall()
        {
            if(_isGrounded == false) 
            {
                _isJumping = _verticalVelocity > 0.1f;
            }
            else
            {
                _isJumping = false;
            }
        }

        private void HandleAnimations()
        {
            _playerAnimationHandler?.UpdateGrounded(_isGrounded);
            _playerAnimationHandler?.UpdateJumped(_isJumping);
            _playerAnimationHandler?.UpdateLocomotion(_currentSpeed, _isCrouching);
        }

        private void Attack()
        {

        }

        private void Jump()
        {
            if (_isGrounded == true)
            {
                _verticalVelocity = _jumpForce;
            }
        }

        private void SprintPerformed()
        {
            _isCrouching = false;
            _isSprint = true;
        }
        private void SprintCanceled()
        {
            _isSprint = false;
        }

        private void OnCrouchToggle()
        {
            _isCrouching = !_isCrouching;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 spherePos = transform.position + Vector3.up * _groundedOffset;
            Gizmos.DrawWireSphere(spherePos, _groundCheckRadius);
        }

        private void CursorBehaviour()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}

