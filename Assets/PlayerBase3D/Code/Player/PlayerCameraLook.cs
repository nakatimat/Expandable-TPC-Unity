using UnityEngine;
using Unity.Cinemachine;
using nakatimat.InputSystem;

namespace nakatimat.Player
{
    // Rotates CameraTarget (yaw/pitch) from LookInput; CinemachineThirdPersonFollow reads
    // that rotation to place the camera. Done in code instead of CinemachineInputAxisController
    // for direct control over sensitivity and safety clamps.
    // ExecuteAlways so the vcam's Follow/LookAt resolve in the Editor without pressing Play.
    [ExecuteAlways]
    public class PlayerCameraLook : MonoBehaviour
    {
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private Transform _cameraTarget;
        [SerializeField] private CinemachineCamera _vcam;

        [Header("Height (Crouch)")]
        [Tooltip("CameraTarget's local height while standing.")]
        [SerializeField] private float _standingHeight = 1.5f;
        [Tooltip("CameraTarget's local height while crouching. Keeps the camera target from ending up inside low ceilings.")]
        [SerializeField] private float _crouchingHeight = 0.9f;
        [SerializeField] private float _heightLerpSpeed = 8f;

        [Header("Sensitivity")]
        [SerializeField] private Vector2 _mouseSensitivity = new Vector2(2f, 2f);
        [SerializeField] private Vector2 _gamepadSensitivity = new Vector2(150f, 150f);

        [Header("Vertical Clamp")]
        [SerializeField] private float _bottomClamp = -60f;
        [SerializeField] private float _topClamp = 60f;

        [Header("Smoothing")]
        [SerializeField] private bool _enableSmoothing = true;
        [SerializeField, Range(0.01f, 0.5f)] private float _smoothTime = 0.05f;
        [Tooltip("Comfort clamp: max degrees/frame the camera can rotate, even on an input spike (focus change, cursor warp, deltaTime hiccup). Prevents the 'crazy spin' that causes motion sickness.")]
        [SerializeField] private float _maxDegreesPerFrame = 25f;

        private float _targetYaw;
        private float _targetPitch;
        private float _targetHeight;
        private Vector2 _currentLookInput;
        private Vector2 _lookInputVelocity;

        private void Awake()
        {
            if (_inputManager == null) { _inputManager = GetComponent<InputManager>(); }
            if (_playerController == null) { _playerController = GetComponent<PlayerController>(); }
            if (_cameraTarget == null)
            {
                Transform found = transform.Find("CameraTarget");
                if (found != null) { _cameraTarget = found; }
            }
            if (_vcam == null) { _vcam = GetComponentInChildren<CinemachineCamera>(true); }

            // Must target CameraTarget, not the player root, so ThirdPersonFollow builds
            // the arm from the rotation this script applies to CameraTarget.
            if (_vcam != null && _cameraTarget != null)
            {
                _vcam.Follow = _cameraTarget;
                _vcam.LookAt = _cameraTarget;
            }
        }

        private void Start()
        {
            if (_cameraTarget != null) { _targetYaw = _cameraTarget.eulerAngles.y; }
            _targetHeight = _standingHeight;
        }

        private void OnEnable()
        {
            if (_playerController != null) { _playerController.CrouchStateChanged += HandleCrouchStateChanged; }
        }

        private void OnDisable()
        {
            if (_playerController != null) { _playerController.CrouchStateChanged -= HandleCrouchStateChanged; }
        }

        private void HandleCrouchStateChanged(bool isCrouching)
        {
            _targetHeight = isCrouching ? _crouchingHeight : _standingHeight;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) { return; }
            if (_inputManager == null || _cameraTarget == null) { return; }

            Vector2 lookDelta = _inputManager.LookInput;
            _currentLookInput = _enableSmoothing
                ? Vector2.SmoothDamp(_currentLookInput, lookDelta, ref _lookInputVelocity, _smoothTime)
                : lookDelta;

            bool isGamepad = _inputManager.IsGamepad;
            float sensitivityX = isGamepad ? _gamepadSensitivity.x * Time.deltaTime : _mouseSensitivity.x * 0.1f;
            float sensitivityY = isGamepad ? _gamepadSensitivity.y * Time.deltaTime : _mouseSensitivity.y * 0.1f;

            float deltaYaw = 0f;
            float deltaPitch = 0f;
            if (_currentLookInput.sqrMagnitude >= 0.01f)
            {
                deltaYaw = _currentLookInput.x * sensitivityX;
                deltaPitch = _currentLookInput.y * sensitivityY;
            }

            // Comfort clamp: no input spike (deltaTime hiccup, cursor warp, focus change)
            // can rotate the camera more than this in a single frame.
            deltaYaw = Mathf.Clamp(deltaYaw, -_maxDegreesPerFrame, _maxDegreesPerFrame);
            deltaPitch = Mathf.Clamp(deltaPitch, -_maxDegreesPerFrame, _maxDegreesPerFrame);

            _targetYaw = ClampAngle(_targetYaw + deltaYaw, float.MinValue, float.MaxValue);
            _targetPitch = ClampAngle(_targetPitch + deltaPitch, _bottomClamp, _topClamp); 

            _cameraTarget.rotation = Quaternion.Euler(-_targetPitch, _targetYaw, 0f);

            Vector3 localPosition = _cameraTarget.localPosition;
            localPosition.y = Mathf.Lerp(localPosition.y, _targetHeight, _heightLerpSpeed * Time.deltaTime);
            _cameraTarget.localPosition = localPosition;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) { angle += 360f; }
            if (angle > 360f) { angle -= 360f; }
            return Mathf.Clamp(angle, min, max);
        }
    }
}
