using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Singletons
{
    public class InputManager : IndestructibleSingletonBehaviour<InputManager>
    {
        public class InputActionEvents
        {
            public Action<InputAction.CallbackContext> Started { get; set; }
            public Action<InputAction.CallbackContext> Performed { get; set; }
            public Action<InputAction.CallbackContext> Canceled { get; set; }
        }

        public enum InputEventType
        {
            Started,
            Performed,
            Canceled
        }

        public InputSystem_Actions GameControls;
        public Dictionary<string, InputActionEvents> InputActionHandlers;

        private Dictionary<string, Action<InputAction.CallbackContext>> _startedCallbacks;
        private Dictionary<string, Action<InputAction.CallbackContext>> _performedCallbacks;
        private Dictionary<string, Action<InputAction.CallbackContext>> _canceledCallbacks;
    

        protected override void Awake()
        {
            base.Awake();
        
            GameControls = new InputSystem_Actions();
            InputActionHandlers = new Dictionary<string, InputActionEvents>();

            _startedCallbacks = new Dictionary<string, Action<InputAction.CallbackContext>>();
            _performedCallbacks = new Dictionary<string, Action<InputAction.CallbackContext>>();
            _canceledCallbacks = new Dictionary<string, Action<InputAction.CallbackContext>>();

            foreach (var input in GameControls)
            {
                InputActionEvents inputActionEvents = new InputActionEvents();
                var action = GameControls.FindAction(input.name);

                Action<InputAction.CallbackContext> startedCallback = ctx => HandleInputTrigger(ctx, InputEventType.Started);
                _startedCallbacks.Add(input.name, startedCallback);
                action.started += startedCallback;

                Action<InputAction.CallbackContext> performedCallback = ctx => HandleInputTrigger(ctx, InputEventType.Performed);
                _performedCallbacks.Add(input.name, performedCallback);
                action.performed += performedCallback;

                Action<InputAction.CallbackContext> canceledCallback = ctx => HandleInputTrigger(ctx, InputEventType.Canceled);
                _canceledCallbacks.Add(input.name, canceledCallback);
                action.canceled += canceledCallback;

                InputActionHandlers.Add(input.name, inputActionEvents);
            }
        
            // todo:: do ctrl-shift-f for this and you'll see, maybe we don't need to do it 3 more times
            GameControls.Player.Enable();
            GameControls.UI.Enable();
        }


        private void HandleInputTrigger(InputAction.CallbackContext context, InputEventType eventType)
        {
            if (InputActionHandlers.TryGetValue(context.action.name, out var actionEvents))
            {
                if (eventType == InputEventType.Started)
                    actionEvents.Started?.Invoke(context);
                else if (eventType == InputEventType.Performed)
                    actionEvents.Performed?.Invoke(context);
                else if (eventType == InputEventType.Canceled)
                    actionEvents.Canceled?.Invoke(context);
            }
            else { Debug.LogWarning($"No handler found for action: {context.action.name}"); }
        }
    
        public void ClearInputActionSubscribers(string actionName, InputEventType eventType)
        {
            if (InputActionHandlers.TryGetValue(actionName, out var actionEvents))
            {
                if (eventType == InputEventType.Started)
                    actionEvents.Started = null;
                else if (eventType == InputEventType.Performed)
                    actionEvents.Performed = null;
                else if (eventType == InputEventType.Canceled)
                    actionEvents.Canceled = null;
            }
        }

        protected override void OnDestroy()
        {
            if (InputActionHandlers != null && GameControls != null)
            {
                //GameControls.Disable();
                foreach (var entry in InputActionHandlers)
                {
                    var action = GameControls.FindAction(entry.Key);
                    if (action != null)
                    {
                        action.started -= _startedCallbacks[entry.Key];
                        action.performed -= _performedCallbacks[entry.Key];
                        action.canceled -= _canceledCallbacks[entry.Key];
                    }
                }
            }
            base.OnDestroy();
        }
    }
}
