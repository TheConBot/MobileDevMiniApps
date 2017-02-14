﻿#if UNITY_4_6 || UNITY_4_7 || UNITY_5
namespace InControl
{
	using UnityEngine;
	using UnityEngine.EventSystems;
	using UnityEngine.Serialization;

	[AddComponentMenu( "Event/InControl Input Module" )]
	public class InControlInputModule : StandaloneInputModule
	{
		public enum Button : int
		{
			Action1 = InputControlType.Action1,
			Action2 = InputControlType.Action2,
			Action3 = InputControlType.Action3,
			Action4 = InputControlType.Action4
		}

		public new Button submitButton = Button.Action1;
		public new Button cancelButton = Button.Action2;
		[Range( 0.1f, 0.9f )]
		public float analogMoveThreshold = 0.5f;
		public float moveRepeatFirstDuration = 0.8f;
		public float moveRepeatDelayDuration = 0.1f;

		// Deprecated.
		bool allowMobileDevice;

		[FormerlySerializedAs( "allowMobileDevice" )]
#if UNITY_5 && !(UNITY_5_0)
		new public bool forceModuleActive;
#else
		public bool forceModuleActive;
#endif

		public bool allowMouseInput = true;
		public bool focusOnMouseHover;

		InputDevice inputDevice;
		Vector3 thisMousePosition;
		Vector3 lastMousePosition;
		Vector2 thisVectorState;
		Vector2 lastVectorState;
		bool thisSubmitState;
		bool lastSubmitState;
		bool thisCancelState;
		bool lastCancelState;
		float nextMoveRepeatTime;
		float lastVectorPressedTime;
		TwoAxisInputControl direction;

		public PlayerAction SubmitAction { get; set; }
		public PlayerAction CancelAction { get; set; }
		public PlayerTwoAxisAction MoveAction { get; set; }


		protected InControlInputModule()
		{
			direction = new TwoAxisInputControl();
			direction.StateThreshold = analogMoveThreshold;
		}


		public override void UpdateModule()
		{
			lastMousePosition = thisMousePosition;
			thisMousePosition = Input.mousePosition;
		}


		public override bool IsModuleSupported()
		{
#if UNITY_WII || UNITY_PS3 || UNITY_PS4 || UNITY_XBOX360 || UNITY_XBOXONE
			return true;
#endif

			if (forceModuleActive || Input.mousePresent)
			{
				return true;
			}

#if UNITY_5
			if (Input.touchSupported)
			{
				return true;
			}
#endif

			return false;
		}


		public override bool ShouldActivateModule()
		{
			if (!(enabled && gameObject.activeInHierarchy))
			{
				return false;
			}

			UpdateInputState();

			var shouldActivate = false;
			shouldActivate |= SubmitWasPressed;
			shouldActivate |= CancelWasPressed;
			shouldActivate |= VectorWasPressed;

#if !UNITY_IOS || UNITY_EDITOR
			if (allowMouseInput)
			{
				shouldActivate |= MouseHasMoved;
				shouldActivate |= MouseButtonIsPressed;
			}
#endif

			if (Input.touchCount > 0)
			{
				shouldActivate = true;
			}

			return shouldActivate;
		}


		public override void ActivateModule()
		{
			base.ActivateModule();

			thisMousePosition = Input.mousePosition;
			lastMousePosition = Input.mousePosition;

			var selectObject = eventSystem.currentSelectedGameObject;

			if (selectObject == null)
			{
				selectObject = eventSystem.firstSelectedGameObject;
			}

			eventSystem.SetSelectedGameObject( selectObject, GetBaseEventData() );
		}


		public override void Process()
		{
			var usedEvent = SendUpdateEventToSelectedObject();

			if (eventSystem.sendNavigationEvents)
			{
				if (!usedEvent)
				{
					usedEvent = SendVectorEventToSelectedObject();
				}

				if (!usedEvent)
				{
					SendButtonEventToSelectedObject();
				}
			}

#if UNITY_5 && !(UNITY_5_0)
			if (ProcessTouchEvents())
			{
				return;
			}
#endif

#if !UNITY_IOS || UNITY_EDITOR
			if (allowMouseInput)
			{
				ProcessMouseEvent();
			}
#endif
		}


#if UNITY_5 && !(UNITY_5_0)
		bool ProcessTouchEvents()
		{
			var touchCount = Input.touchCount;
			for (var i = 0; i < touchCount; ++i)
			{
				var touch = Input.GetTouch( i );

				if (touch.type == UnityEngine.TouchType.Indirect)
				{
					continue;
				}

				bool released;
				bool pressed;
				var pointer = GetTouchPointerEventData( touch, out pressed, out released );

				ProcessTouchPress( pointer, pressed, released );

				if (!released)
				{
					ProcessMove( pointer );
					ProcessDrag( pointer );
				}
				else
				{
					RemovePointerData( pointer );
				}
			}

			return touchCount > 0;
		}
#endif


		bool SendButtonEventToSelectedObject()
		{
			if (eventSystem.currentSelectedGameObject == null)
			{
				return false;
			}

			var eventData = GetBaseEventData();

			if (SubmitWasPressed)
			{
				//				ExecuteEvents.Execute( eventSystem.currentSelectedGameObject, new PointerEventData( EventSystem.current ), ExecuteEvents.pointerDownHandler );
				ExecuteEvents.Execute( eventSystem.currentSelectedGameObject, eventData, ExecuteEvents.submitHandler );
			}
			else
			if (SubmitWasReleased)
			{
				//				ExecuteEvents.Execute( eventSystem.currentSelectedGameObject, new PointerEventData( EventSystem.current ), ExecuteEvents.pointerUpHandler );
			}

			if (CancelWasPressed)
			{
				ExecuteEvents.Execute( eventSystem.currentSelectedGameObject, eventData, ExecuteEvents.cancelHandler );
			}

			return eventData.used;
		}


		bool SendVectorEventToSelectedObject()
		{
			if (!VectorWasPressed)
			{
				return false;
			}

			var axisEventData = GetAxisEventData( thisVectorState.x, thisVectorState.y, 0.5f );

			if (axisEventData.moveDir != MoveDirection.None)
			{
				if (eventSystem.currentSelectedGameObject == null)
				{
					eventSystem.SetSelectedGameObject( eventSystem.firstSelectedGameObject, GetBaseEventData() );
				}
				else
				{
					ExecuteEvents.Execute( eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler );
				}
				SetVectorRepeatTimer();
			}

			return axisEventData.used;
		}


		protected override void ProcessMove( PointerEventData pointerEvent )
		{
			var lastPointerEnter = pointerEvent.pointerEnter;

			base.ProcessMove( pointerEvent );

			if (focusOnMouseHover && lastPointerEnter != pointerEvent.pointerEnter)
			{
				var selectObject = ExecuteEvents.GetEventHandler<ISelectHandler>( pointerEvent.pointerEnter );
				eventSystem.SetSelectedGameObject( selectObject, pointerEvent );
			}
		}


		void Update()
		{
			direction.Filter( Device.Direction, Time.deltaTime );
		}


		void UpdateInputState()
		{
			lastVectorState = thisVectorState;
			thisVectorState = Vector2.zero;

			var dir = MoveAction ?? direction;

			if (Utility.AbsoluteIsOverThreshold( dir.X, analogMoveThreshold ))
			{
				thisVectorState.x = Mathf.Sign( dir.X );
			}

			if (Utility.AbsoluteIsOverThreshold( dir.Y, analogMoveThreshold ))
			{
				thisVectorState.y = Mathf.Sign( dir.Y );
			}

			if (VectorIsReleased)
			{
				nextMoveRepeatTime = 0.0f;
			}

			if (VectorIsPressed)
			{
				if (lastVectorState == Vector2.zero)
				{
					if (Time.realtimeSinceStartup > lastVectorPressedTime + 0.1f)
					{
						nextMoveRepeatTime = Time.realtimeSinceStartup + moveRepeatFirstDuration;
					}
					else
					{
						nextMoveRepeatTime = Time.realtimeSinceStartup + moveRepeatDelayDuration;
					}
				}

				lastVectorPressedTime = Time.realtimeSinceStartup;
			}

			lastSubmitState = thisSubmitState;
			thisSubmitState = SubmitAction == null ? SubmitButton.IsPressed : SubmitAction.IsPressed;

			lastCancelState = thisCancelState;
			thisCancelState = CancelAction == null ? CancelButton.IsPressed : CancelAction.IsPressed;
		}


		public InputDevice Device
		{
			set
			{
				inputDevice = value;
			}

			get
			{
				return inputDevice ?? InputManager.ActiveDevice;
			}
		}


		InputControl SubmitButton
		{
			get
			{
				return Device.GetControl( (InputControlType) submitButton );
			}
		}


		InputControl CancelButton
		{
			get
			{
				return Device.GetControl( (InputControlType) cancelButton );
			}
		}


		void SetVectorRepeatTimer()
		{
			nextMoveRepeatTime = Mathf.Max( nextMoveRepeatTime, Time.realtimeSinceStartup + moveRepeatDelayDuration );
		}


		bool VectorIsPressed
		{
			get
			{
				return thisVectorState != Vector2.zero;
			}
		}


		bool VectorIsReleased
		{
			get
			{
				return thisVectorState == Vector2.zero;
			}
		}


		bool VectorHasChanged
		{
			get
			{
				return thisVectorState != lastVectorState;
			}
		}


		bool VectorWasPressed
		{
			get
			{
				if (VectorIsPressed && Time.realtimeSinceStartup > nextMoveRepeatTime)
				{
					return true;
				}

				return VectorIsPressed && lastVectorState == Vector2.zero;
			}
		}


		bool SubmitWasPressed
		{
			get
			{
				return thisSubmitState && thisSubmitState != lastSubmitState;
			}
		}


		bool SubmitWasReleased
		{
			get
			{
				return !thisSubmitState && thisSubmitState != lastSubmitState;
			}
		}


		bool CancelWasPressed
		{
			get
			{
				return thisCancelState && thisCancelState != lastCancelState;
			}
		}


		bool MouseHasMoved
		{
			get
			{
				return (thisMousePosition - lastMousePosition).sqrMagnitude > 0.0f;
			}
		}


		bool MouseButtonIsPressed
		{
			get
			{
				return Input.GetMouseButtonDown( 0 );
			}
		}


		// Copied from StandaloneInputModule where these are marked private instead of protected in Unity 5.0 / 5.1
		#region Unity 5.0 / 5.1 compatibility.

#if UNITY_5_0 || UNITY_5_1

		bool SendUpdateEventToSelectedObject()
		{
			if (eventSystem.currentSelectedGameObject == null)
			{
				return false;
			}
			BaseEventData baseEventData = GetBaseEventData();
			ExecuteEvents.Execute<IUpdateSelectedHandler>( eventSystem.currentSelectedGameObject, baseEventData, ExecuteEvents.updateSelectedHandler );
			return baseEventData.used;
		}


		void ProcessMouseEvent()
		{
			var mousePointerEventData = this.GetMousePointerEventData();
			var pressed = mousePointerEventData.AnyPressesThisFrame();
			var released = mousePointerEventData.AnyReleasesThisFrame();
			var eventData = mousePointerEventData.GetButtonState( PointerEventData.InputButton.Left ).eventData;
			if (!UseMouse( pressed, released, eventData.buttonData ))
			{
				return;
			}
			ProcessMousePress( eventData );
			ProcessMove( eventData.buttonData );
			ProcessDrag( eventData.buttonData );
			ProcessMousePress( mousePointerEventData.GetButtonState( PointerEventData.InputButton.Right ).eventData );
			ProcessDrag( mousePointerEventData.GetButtonState( PointerEventData.InputButton.Right ).eventData.buttonData );
			ProcessMousePress( mousePointerEventData.GetButtonState( PointerEventData.InputButton.Middle ).eventData );
			ProcessDrag( mousePointerEventData.GetButtonState( PointerEventData.InputButton.Middle ).eventData.buttonData );
			if (!Mathf.Approximately( eventData.buttonData.scrollDelta.sqrMagnitude, 0 ))
			{
				var eventHandler = ExecuteEvents.GetEventHandler<IScrollHandler>( eventData.buttonData.pointerCurrentRaycast.gameObject );
				ExecuteEvents.ExecuteHierarchy<IScrollHandler>( eventHandler, eventData.buttonData, ExecuteEvents.scrollHandler );
			}
		}


		void ProcessMousePress( PointerInputModule.MouseButtonEventData data )
		{
			var buttonData = data.buttonData;
			var gameObject = buttonData.pointerCurrentRaycast.gameObject;
			if (data.PressedThisFrame())
			{
				buttonData.eligibleForClick = true;
				buttonData.delta = Vector2.zero;
				buttonData.dragging = false;
				buttonData.useDragThreshold = true;
				buttonData.pressPosition = buttonData.position;
				buttonData.pointerPressRaycast = buttonData.pointerCurrentRaycast;
				DeselectIfSelectionChanged( gameObject, buttonData );
				var gameObject2 = ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>( gameObject, buttonData, ExecuteEvents.pointerDownHandler );
				if (gameObject2 == null)
				{
					gameObject2 = ExecuteEvents.GetEventHandler<IPointerClickHandler>( gameObject );
				}
				var unscaledTime = Time.unscaledTime;
				if (gameObject2 == buttonData.lastPress)
				{
					var num = unscaledTime - buttonData.clickTime;
					if (num < 0.3f)
					{
						buttonData.clickCount++;
					}
					else
					{
						buttonData.clickCount = 1;
					}
					buttonData.clickTime = unscaledTime;
				}
				else
				{
					buttonData.clickCount = 1;
				}
				buttonData.pointerPress = gameObject2;
				buttonData.rawPointerPress = gameObject;
				buttonData.clickTime = unscaledTime;
				buttonData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>( gameObject );
				if (buttonData.pointerDrag != null)
				{
					ExecuteEvents.Execute<IInitializePotentialDragHandler>( buttonData.pointerDrag, buttonData, ExecuteEvents.initializePotentialDrag );
				}
			}
			if (data.ReleasedThisFrame())
			{
				ExecuteEvents.Execute<IPointerUpHandler>( buttonData.pointerPress, buttonData, ExecuteEvents.pointerUpHandler );
				var eventHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>( gameObject );
				if (buttonData.pointerPress == eventHandler && buttonData.eligibleForClick)
				{
					ExecuteEvents.Execute<IPointerClickHandler>( buttonData.pointerPress, buttonData, ExecuteEvents.pointerClickHandler );
				}
				else
				{
					if (buttonData.pointerDrag != null)
					{
						ExecuteEvents.ExecuteHierarchy<IDropHandler>( gameObject, buttonData, ExecuteEvents.dropHandler );
					}
				}
				buttonData.eligibleForClick = false;
				buttonData.pointerPress = null;
				buttonData.rawPointerPress = null;
				if (buttonData.pointerDrag != null && buttonData.dragging)
				{
					ExecuteEvents.Execute<IEndDragHandler>( buttonData.pointerDrag, buttonData, ExecuteEvents.endDragHandler );
				}
				buttonData.dragging = false;
				buttonData.pointerDrag = null;
				if (gameObject != buttonData.pointerEnter)
				{
					HandlePointerExitAndEnter( buttonData, null );
					HandlePointerExitAndEnter( buttonData, gameObject );
				}
			}
		}


		static bool UseMouse( bool pressed, bool released, PointerEventData pointerData )
		{
			return pressed || released || pointerData.IsPointerMoving() || pointerData.IsScrolling();
		}

#endif

		#endregion


		// Copied from StandaloneInputModule where these are marked private instead of protected in Unity 5.3 / 5.4
		#region Unity 5.3 / 5.4 compatibility.

#if UNITY_5_3 || UNITY_5_4
		void ProcessTouchPress( PointerEventData pointerEvent, bool pressed, bool released )
		{
			var go = pointerEvent.pointerCurrentRaycast.gameObject;
			if (pressed)
			{
				pointerEvent.eligibleForClick = true;
				pointerEvent.delta = Vector2.zero;
				pointerEvent.dragging = false;
				pointerEvent.useDragThreshold = true;
				pointerEvent.pressPosition = pointerEvent.position;
				pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;
				DeselectIfSelectionChanged( go, pointerEvent );
				if (pointerEvent.pointerEnter != go)
				{
					HandlePointerExitAndEnter( pointerEvent, go );
					pointerEvent.pointerEnter = go;
				}
				var go2 = ExecuteEvents.ExecuteHierarchy( go, pointerEvent, ExecuteEvents.pointerDownHandler );
				if (go2 == null)
				{
					go2 = ExecuteEvents.GetEventHandler<IPointerClickHandler>( go );
				}
				float unscaledTime = Time.unscaledTime;
				if (go2 == pointerEvent.lastPress)
				{
					float num = unscaledTime - pointerEvent.clickTime;
					if (num < 0.3f)
					{
						pointerEvent.clickCount++;
					}
					else
					{
						pointerEvent.clickCount = 1;
					}
					pointerEvent.clickTime = unscaledTime;
				}
				else
				{
					pointerEvent.clickCount = 1;
				}
				pointerEvent.pointerPress = go2;
				pointerEvent.rawPointerPress = go;
				pointerEvent.clickTime = unscaledTime;
				pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>( go );
				if (pointerEvent.pointerDrag != null)
				{
					ExecuteEvents.Execute( pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag );
				}
			}
			if (released)
			{
				ExecuteEvents.Execute( pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler );
				var eventHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>( go );
				if (pointerEvent.pointerPress == eventHandler && pointerEvent.eligibleForClick)
				{
					ExecuteEvents.Execute( pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler );
				}
				else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
				{
					ExecuteEvents.ExecuteHierarchy( go, pointerEvent, ExecuteEvents.dropHandler );
				}
				pointerEvent.eligibleForClick = false;
				pointerEvent.pointerPress = null;
				pointerEvent.rawPointerPress = null;
				if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
				{
					ExecuteEvents.Execute( pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler );
				}
				pointerEvent.dragging = false;
				pointerEvent.pointerDrag = null;
				if (pointerEvent.pointerDrag != null)
				{
					ExecuteEvents.Execute( pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler );
				}
				pointerEvent.pointerDrag = null;
				ExecuteEvents.ExecuteHierarchy( pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler );
				pointerEvent.pointerEnter = null;
			}
		}
#endif

		#endregion
	}
}
#endif
