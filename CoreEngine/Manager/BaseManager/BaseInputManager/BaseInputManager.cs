using CoreEngine.CameraSystem;
using CoreEngine.EventBus;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

//public delegate Vector2 MovementDelegate(float x, float y);
//public delegate 
namespace CoreEngine.Input
{
    #region input interface
    public interface IBaseInput<T> { T Value { get; } }

    /// <summary>
    /// wasd 입력
    /// <para>조이스틱L 입력</para>
    /// </summary>
    public interface IMoveInput : IBaseInput<Vector2> { }

    /// <summary>
    /// 마우스 이동 입력
    /// <para>조이스틱 R 입력</para>
    /// </summary>
    public interface ILookInput : IBaseInput<Vector2> { }

    public interface ISprintInput : IBaseInput<bool> { }

    /// <summary>
    /// 마우스 y축 휠 입력
    /// <para>조이스틱 조합입력  ex) B + 조이스틱R 위아래</para>
    /// </summary>
    public interface IScrollDeltaInput : IBaseInput<float> { }

    #endregion

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
