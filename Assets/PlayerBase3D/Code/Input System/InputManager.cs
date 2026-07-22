using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nakatimat.InputSystem
{
    public class InputManager : MonoBehaviour, Controls.IMainActions
    {
        public event Action JumpPerformed;
        public event Action AttackPerformed;
        public event Action SprintPerformed;
        public event Action SprintCanceled;
        public event Action CrouchToggled;

        public Vector2 MoveInput { get; private set; }

        private Controls _controls;

        private void Awake() => _controls = new Controls();

        private void OnEnable()
        {
            _controls.Main.SetCallbacks(this);
            _controls.Enable();
        }

        private void OnDisable() => _controls.Disable();

        public void OnMovement(InputAction.CallbackContext context) => MoveInput = context.ReadValue<Vector2>();

        public void OnLook(InputAction.CallbackContext context) { }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed) { JumpPerformed?.Invoke(); }
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed) { AttackPerformed?.Invoke(); }
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started) { SprintPerformed?.Invoke(); }
            if (context.canceled) { SprintCanceled?.Invoke(); }
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.performed) { CrouchToggled?.Invoke(); }
        }
    }
}
