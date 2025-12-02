using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace nakatimat.InputSystem 
{
    public class InputManager : MonoBehaviour, Controls.IMainActions
    {
        public static InputManager Input;

        private Controls inputActions;

        private Vector2 _moveComposite;

        [Header("Action Events")]
        public Action onJumpPerformed;
        public Action onAttackPerformed;
        public Action onSprintPerformed;
        public Action onSprintCanceled;

        private void Awake()
        {
            if(Input != null && Input != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Input = this;

            inputActions = new Controls();
            inputActions.Main.SetCallbacks(this);
            inputActions.Enable();
        }

        private void OnDisable()
        {
            if(inputActions != null) { inputActions.Disable();  }
        }

        #region Public API
        public Vector2 GetAxis()
        {
            return _moveComposite;
        }
        #endregion

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed == false) { return; }
            onAttackPerformed?.Invoke();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if(context.performed == false) { return; }
            onJumpPerformed?.Invoke();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            
        }

        public void OnMovement(InputAction.CallbackContext context)
        {
            _moveComposite = context.ReadValue<Vector2>();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.performed == false) 
            { 
                onSprintPerformed?.Invoke();
            }
            
            if (context.canceled == true)
            {
                onSprintCanceled?.Invoke();
            }
        }

    }
}

