using CoreEngine.CameraSystem;
using CoreEngine.EventBus;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

//public delegate Vector2 MovementDelegate(float x, float y);
//public delegate 
namespace CoreEngine.Manager
{
    public abstract class BaseInputManager<TInputAction> : BaseManager, IManager
        where TInputAction : class, IInputActionCollection2, IDisposable, new()
    {
        protected TInputAction inputAction { get; private set; }


        protected override void OnEnable()
        {
            base.OnEnable();
            inputAction?.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            inputAction?.Disable();
        }

        public override void Exit()
        {
            base.Exit();
            if (inputAction != null)
            {
                inputAction.Disable();
                inputAction = null;
            }
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        public override IEnumerator Initialize()
        {
            yield return base.Initialize();
            inputAction ??= new TInputAction();

            if (inputAction != null)
            {
                inputAction.Enable();
            }
            InputSystem.onDeviceChange += OnDeviceChange;
            yield return null;
        }

        protected void OnMouseLock(InputAction.CallbackContext context)
        {
            OnMouseLock(!context.ReadValueAsButton());
        }

        private void OnMouseLock(bool isMouseLock)
        {
            SetCursorState(isMouseLock);
            EventBus<ToggleMouseLockEvent>.Publish(new ToggleMouseLockEvent(isMouseLock));
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Disconnected)
            {
                Debug.Log("일시정지 및 UI 팝업 이벤트 발생!");
                // DOTO: 일시정지 및 UI 팝업 이벤트 발생!
            }
        }

        protected void SetCursorState(bool isMouseLock)
        {
            Cursor.lockState = isMouseLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isMouseLock;
        }
    }
}
