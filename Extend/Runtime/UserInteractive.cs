using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI {

	public class UserInteractive : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {

		public enum ePointerType { Left, Right, LeftAndRight, Touch }

		private const double DOUBLE_CLICK_MAX_INTERVAL = 0.6;
		private const double LONG_PRESS_START = 0.5;

		private const int TOUCH_MOUSE_OFFSET = 3;

		[SerializeField]
		private float m_DoubleClickThreshold = 16f;

		#region events
		public class UIEvent : UnityEvent<Vector2, ePointerType> { }
		public class UIEventDrag : UnityEvent<Vector2, Vector2, ePointerType> { }
		public class UIEventMulti : UnityEvent<Vector2, Vector2> { }
		public class UIEventMulti2 : UnityEvent<Vector2, Vector2, Vector2, Vector2> { }

		private UIEvent mOnClick = new UIEvent();
		/// <summary>
		/// OnClick(Vector2 pos, bool isRightClick)
		/// </summary>
		public UIEvent OnClick { get { return mOnClick; } }

		private UIEvent mOnDoubleClick = new UIEvent();
		/// <summary>
		/// OnDoubleClick(Vector2 pos, bool isRightClick)
		/// </summary>
		public UIEvent OnDoubleClick { get { return mOnDoubleClick; } }

		private UIEvent mOnDoubleLongPressClick = new UIEvent();
		/// <summary>
		/// OnDoubleLongPressClick(Vector2 pos, bool isRightClick)
		/// </summary>
		public UIEvent OnDoubleLongPressClick { get { return mOnDoubleLongPressClick; } }

		private UIEvent mOnLongPress = new UIEvent();
		/// <summary>
		/// OnLongPress(Vector2 pos, bool isRightClick)
		/// </summary>
		public UIEvent OnLongPress { get { return mOnLongPress; } }

		private UIEvent mOnLongPressClick = new UIEvent();
		/// <summary>
		/// OnLongPressClick(Vector2 pos, bool isRightClick)
		/// </summary>
		public UIEvent OnLongPressClick { get { return mOnLongPressClick; } }

		private UIEvent mOnLongPress2 = new UIEvent();
		/// <summary>
		/// OnLongPress2(Vector2 pos, bool isRightClick)
		/// </summary>
		public UIEvent OnLongPress2 { get { return mOnLongPress2; } }

		private UIEvent mOnDragStart = new UIEvent();
		/// <summary>
		/// OnDragStart(Vector2 startPos, bool isRightClick)
		/// </summary>
		public UIEvent OnDragStart { get { return mOnDragStart; } }

		private UIEventDrag mOnDragging = new UIEventDrag();
		/// <summary>
		/// OnDragging(Vector2 startPos, Vector2 delta, bool isRightClick)
		/// </summary>
		public UIEventDrag OnDragging { get { return mOnDragging; } }

		private UIEventDrag mOnDragEnd = new UIEventDrag();
		/// <summary>
		/// OnDragEnd(Vector2 startPos, Vector2 endPos, bool isRightClick)
		/// </summary>
		public UIEventDrag OnDragEnd { get { return mOnDragEnd; } }

		private UIEvent mOnLongPressDragStart = new UIEvent();
		/// <summary>
		/// OnLongPressDragStart(Vector2 startPos, bool isRightClick)
		/// </summary>
		public UIEvent OnLongPressDragStart { get { return mOnLongPressDragStart; } }

		private UIEventDrag mOnLongPressDragging = new UIEventDrag();
		/// <summary>
		/// OnLongPressDragging(Vector2 startPos, Vector2 delta, bool isRightClick)
		/// </summary>
		public UIEventDrag OnLongPressDragging { get { return mOnLongPressDragging; } }

		private UIEventDrag mOnLongPressDragEnd = new UIEventDrag();
		/// <summary>
		/// OnLongPressDragEnd(Vector2 startPos, Vector2 endPos, bool isRightClick)
		/// </summary>
		public UIEventDrag OnLongPressDragEnd { get { return mOnLongPressDragEnd; } }

		private UIEvent mOnDoubleClickDragStart = new UIEvent();
		/// <summary>
		/// OnDoubleClickDragStart(Vector2 startPos, bool isRightClick)
		/// </summary>
		public UIEvent OnDoubleClickDragStart { get { return mOnDoubleClickDragStart; } }

		private UIEventDrag mOnDoubleClickDragging = new UIEventDrag();
		/// <summary>
		/// OnDoubleClickDragging(Vector2 startPos, Vector2 delta, bool isRightClick)
		/// </summary>
		public UIEventDrag OnDoubleClickDragging { get { return mOnDoubleClickDragging; } }

		private UIEventDrag mOnDoubleClickDragEnd = new UIEventDrag();
		/// <summary>
		/// OnDoubleClickDragEnd(Vector2 startPos, Vector2 endPos, bool isRightClick)
		/// </summary>
		public UIEventDrag OnDoubleClickDragEnd { get { return mOnDoubleClickDragEnd; } }

		private UIEvent mOnDoubleClickLongPressDragStart = new UIEvent();
		/// <summary>
		/// OnDoubleClickLongPressDragStart(Vector2 startPos, bool isRightClick)
		/// </summary>
		public UIEvent OnDoubleClickLongPressDragStart { get { return mOnDoubleClickLongPressDragStart; } }

		private UIEventDrag mOnDoubleClickLongPressDragging = new UIEventDrag();
		/// <summary>
		/// OnDoubleClickLongPressDragging(Vector2 startPos, Vector2 delta, bool isRightClick)
		/// </summary>
		public UIEventDrag OnDoubleClickLongPressDragging { get { return mOnDoubleClickLongPressDragging; } }

		private UIEventDrag mOnDoubleClickLongPressDragEnd = new UIEventDrag();
		/// <summary>
		/// OnDoubleClickLongPressDragEnd(Vector2 startPos, Vector2 endPos, bool isRightClick)
		/// </summary>
		public UIEventDrag OnDoubleClickLongPressDragEnd { get { return mOnDoubleClickLongPressDragEnd; } }

		private UIEventMulti mOnMultiTouchBegin = new UIEventMulti();
		/// <summary>
		/// OnMultiTouchBegin(Vector2 startPos0, Vector2 startPos1)
		/// </summary>
		public UIEventMulti OnMultiTouchBegin { get { return mOnMultiTouchBegin; } }

		private UIEventMulti2 mOnMultiTouchDrag = new UIEventMulti2();
		/// <summary>
		/// OnMultiTouchDrag(Vector2 startPos0, Vector2 startPos1, Vector2 curPos0, Vector2 curPos1)
		/// </summary>
		public UIEventMulti2 OnMultiTouchDrag { get { return mOnMultiTouchDrag; } }

		private UIEventMulti2 mOnMultiTouchEnd = new UIEventMulti2();
		/// <summary>
		/// OnMultiTouchEnd(Vector2 startPos0, Vector2 startPos1, Vector2 curPos0, Vector2 curPos1)
		/// </summary>
		public UIEventMulti2 OnMultiTouchEnd { get { return mOnMultiTouchEnd; } }

		#endregion

		public Func<Vector2, int> OnTouchDown;

		private RectTransform mRectTransform;
		private List<PointerState> mPointerStates = new List<PointerState>();

		void Awake() {
			mRectTransform = transform as RectTransform;
		}

		public void OnPointerDown(PointerEventData eventData) {
			DateTime now = DateTime.UtcNow;
			Vector2 pos = GetPosition(eventData);
			int index = eventData.pointerId + TOUCH_MOUSE_OFFSET;
			int cellId = OnTouchDown == null ? -1 : OnTouchDown(pos);
			int isDoubleClick = 0;
			for (int i = mPointerStates.Count - 1; i >= 0; i--) {
				PointerState ps = mPointerStates[i];
				if (ps.DoubleClick > 0 || ps.Dragging) { continue; }
				if ((now - ps.DownTime).TotalSeconds > DOUBLE_CLICK_MAX_INTERVAL) { continue; }
				if (cellId >= 0) {
					if (cellId != ps.CellId) { continue; }
				} else {
					if ((ps.DownPos - pos).sqrMagnitude > m_DoubleClickThreshold * m_DoubleClickThreshold) { continue; }
				}
				if (ps.Active) {
					if (i < TOUCH_MOUSE_OFFSET && index < TOUCH_MOUSE_OFFSET) {
						isDoubleClick = 2;
						ps.DoubleClick = 2;
					}
				} else {
					isDoubleClick = 1;
				}
			}
			while (eventData.pointerId + TOUCH_MOUSE_OFFSET >= mPointerStates.Count) { mPointerStates.Add(new PointerState()); }
			PointerState state = mPointerStates[index];
			state.CellId = cellId;
			state.DoubleClick = isDoubleClick;
			state.LongPress = 0;
			state.Dragging = false;
			state.DownPos = pos;
			state.PrevPos = pos;
			state.DownTime = now;
			state.Active = true;
			PointerState p0, p1;
			if (GetFirst2ActiveTouch(out p0, out p1) && (p0 == state || p1 == state)) {
				state.DoubleClick = 0;
				state.LongPress = -1;
				p0.DownPos = p0.PrevPos;
				p1.DownPos = p1.PrevPos;
				mOnMultiTouchBegin.Invoke(p0.DownPos, p1.DownPos);
			}
		}

		public void OnDrag(PointerEventData eventData) {
			int index = eventData.pointerId + TOUCH_MOUSE_OFFSET;
			if (mPointerStates.Count <= index) { return; }
			PointerState state = mPointerStates[index];
			if (!state.Active) { return; }
			bool firstDrag = !state.Dragging;
			state.Dragging = true;
			Vector2 pos = GetPosition(eventData);
			Vector2 delta = pos - state.PrevPos;
			state.PrevPos = pos;
			PointerState p0, p1;
			if (GetFirst2ActiveTouch(out p0, out p1) && (p0 == state || p1 == state)) {
				p0.Dragging = true;
				p1.Dragging = true;
				mOnMultiTouchDrag.Invoke(p0.DownPos, p1.DownPos, p0.PrevPos, p1.PrevPos);
				return;
			}
			ePointerType pt = ePointerType.Touch;
			if (eventData.pointerId < 0) {
				pt = eventData.pointerId == -2 ? ePointerType.Right : ePointerType.Left;
			}
			if (state.DoubleClick == 2) {
				pt = ePointerType.LeftAndRight;
			}
			if (firstDrag) {
				if (state.DoubleClick > 0) {
					(state.LongPress == 1 ? mOnDoubleClickLongPressDragStart : mOnDoubleClickDragStart).Invoke(state.DownPos, pt);
				} else {
					(state.LongPress == 1 ? mOnLongPressDragStart : mOnDragStart).Invoke(state.DownPos, pt);
				}
			}
			if (state.DoubleClick > 0) {
				(state.LongPress == 1 ? mOnDoubleClickLongPressDragging : mOnDoubleClickDragging).Invoke(state.DownPos, delta, pt);
			} else {
				(state.LongPress == 1 ? mOnLongPressDragging : mOnDragging).Invoke(state.DownPos, delta, pt);
			}
		}

		public void OnPointerUp(PointerEventData eventData) {
			if (mPointerStates.Count <= eventData.pointerId + TOUCH_MOUSE_OFFSET) { return; }
			int index = eventData.pointerId + TOUCH_MOUSE_OFFSET;
			PointerState state = mPointerStates[index];
			PointerState p0, p1;
			if (GetFirst2ActiveTouch(out p0, out p1) && (state == p0 || state == p1)) {
				mOnMultiTouchEnd.Invoke(p0.DownPos, p1.DownPos, p0.PrevPos, p1.PrevPos);
			}
			if (!state.Active) { return; }
			state.Active = false;
			Vector2 pos = GetPosition(eventData);
			ePointerType pt = ePointerType.Touch;
			if (eventData.pointerId < 0) {
				pt = eventData.pointerId == -2 ? ePointerType.Right : ePointerType.Left;
			}
			if (state.DoubleClick == 2) {
				pt = ePointerType.LeftAndRight;
				for (int i = mPointerStates.Count - 1; i >= 0; i--) {
					if (i == index) { continue; }
					PointerState ps = mPointerStates[i];
					if (!ps.Active) { continue; }
					if (ps.DoubleClick == 2) {
						ps.Active = false;
					}
				}
			}
			if (state.Dragging) {
				if (state.DoubleClick > 0) {
					(state.LongPress == 1 ? mOnDoubleClickLongPressDragEnd : mOnDoubleClickDragEnd).Invoke(state.DownPos, pos, pt);
				} else {
					(state.LongPress == 1 ? mOnLongPressDragEnd : mOnDragEnd).Invoke(state.DownPos, pos, pt);
				}
			} else {
				if (state.DoubleClick > 0) {
					(state.LongPress == 1 ? mOnDoubleLongPressClick : mOnDoubleClick).Invoke(state.DownPos, pt);
				} else {
					(state.LongPress == 1 ? mOnLongPressClick : mOnClick).Invoke(state.DownPos, pt);
				}
			}
			if (GetFirst2ActiveTouch(out p0, out p1) && (state == p0 || state == p1)) {
				p0.DownPos = p0.PrevPos;
				p1.DownPos = p1.PrevPos;
				p0.LongPress = -1;
				p1.LongPress = -1;
				mOnMultiTouchBegin.Invoke(p0.DownPos, p1.DownPos);
			}
		}

		void Update() {
			int activeCount = 0;
			int activeIndex = -1;
			for (int i = mPointerStates.Count - 1; i >= 0; i--) {
				if (mPointerStates[i].Active) { activeCount++; activeIndex = i; }
			}
			if (activeCount != 1) { return; }
			PointerState state = mPointerStates[activeIndex];
			if (!state.Active || state.LongPress != 0 || state.Dragging) { return; }
			if ((DateTime.UtcNow - state.DownTime).TotalSeconds > LONG_PRESS_START) {
				state.LongPress = 1;
				ePointerType pt = ePointerType.Touch;
				switch (activeIndex - TOUCH_MOUSE_OFFSET) {
					case -1: pt = ePointerType.Left; break;
					case -2: pt = ePointerType.Right; break;
				}
				if (state.DoubleClick == 2) {
					pt = ePointerType.LeftAndRight;
				}
				(state.DoubleClick > 0 ? mOnLongPress2 : mOnLongPress).Invoke(state.DownPos, pt);
			}
		}

		private Vector2 GetPosition(PointerEventData eventData) {
			Vector2 pos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(mRectTransform,
				eventData.position, eventData.pressEventCamera, out pos);
			return pos;
		}

		private bool GetFirst2ActiveTouch(out PointerState p0, out PointerState p1) {
			int len = mPointerStates.Count;
			int count = 0;
			p0 = null;
			p1 = null;
			for (int i = TOUCH_MOUSE_OFFSET; i < len; i++) {
				PointerState state = mPointerStates[i];
				if (!state.Active || state.DoubleClick == 2) { continue; }
				count++;
				if (count == 1) { p0 = state; } else { p1 = state; }
				if (count >= 2) { break; }
			}
			return count >= 2;
		}

		private class PointerState {
			public bool Active;
			public int CellId;
			public Vector2 DownPos;
			public DateTime DownTime;
			public Vector2 PrevPos;
			public int LongPress;
			public bool Dragging;
			public int DoubleClick;
		}

	}

}
