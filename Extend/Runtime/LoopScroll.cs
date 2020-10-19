#pragma warning disable 649

namespace UnityEngine.UI {

	[RequireComponent(typeof(ScrollRect))]
	public class LoopScroll : MonoBehaviour {

		public enum ePointerType {
			None, NewRowOrColumn, Left, Right, Up, Down
		}

		public delegate void ApplyDelegate(GameObject go, int i, object data, ePointerType prevDirType, ePointerType nextDirType);

		[SerializeField]
		private eDirection m_Direction = eDirection.Vertical;
		[SerializeField]
		private bool m_SnakeLayout = false;
		[SerializeField]
		private GameObject m_Template;

		[SerializeField]
		private Vector2 m_Margin = new Vector2(8f, 8f);
		[SerializeField]
		private Vector2 m_Padding = new Vector2(4f, 4f);
		[SerializeField]
		private float m_CreateBuffer = 20f;
		[SerializeField]
		private float m_RemoveBuffer = 40f;
		[SerializeField]
		private bool m_AutoDisableScroll = false;

		private ScrollRect mScrollRect;
		private Vector2 mItemSize;
		private int mColumns;
		private int mRows;

		private object[] mDatas;
		private ApplyDelegate mOnApply;

		private ItemData mItemHead;
		private ItemData mItemEnd;

		private ItemData mCachedItemHead;
		private ItemData mCachedItemEnd;

		private Bounds mPrevBounds;

		void Awake() {
			if (m_Template != null && m_Template.activeSelf) {
				m_Template.SetActive(false);
				mItemHead = new ItemData();
				mItemEnd = new ItemData();
				mItemHead.next = mItemEnd;
				mItemEnd.prev = mItemHead;
				mCachedItemHead = new ItemData();
				mCachedItemEnd = new ItemData();
				mCachedItemHead.next = mCachedItemEnd;
				mCachedItemEnd.prev = mCachedItemHead;
			}
		}

		void Update() {
			if (mScrollRect != null) {
				Flush();
			}
		}

		public void SetDatas(object[] datas, int focusIndex, ApplyDelegate onApply) {
			if (m_Template == null) { return; }
			if (mScrollRect == null) {
				mScrollRect = GetComponent<ScrollRect>();
				RectTransform tItem = m_Template.transform as RectTransform;
				Rect itemRect = tItem.rect;
				mItemSize = new Vector2(itemRect.width, itemRect.height);
			}
			mDatas = datas;
			mOnApply = onApply;
			int count = datas.Length;
			mItemHead.index = -1;
			mItemEnd.index = count;
			RectTransform content = mScrollRect.content;
			Rect viewportRect = mScrollRect.viewport.rect;
			if (m_Direction == eDirection.Vertical) {
				float width = content.rect.width - m_Padding.x - m_Padding.x;
				mColumns = Mathf.Max(Mathf.FloorToInt((width + m_Margin.x) / (mItemSize.x + m_Margin.x)), 1);
				mRows = Mathf.CeilToInt(count / (float)mColumns);
				float height = mRows * (mItemSize.y + m_Margin.y) - m_Margin.y + m_Padding.y + m_Padding.y;
				content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
				mScrollRect.vertical = !m_AutoDisableScroll || height > viewportRect.height;
			} else {
				float height = content.rect.height - m_Padding.y - m_Padding.y;
				mRows = Mathf.Max(Mathf.FloorToInt((height + m_Margin.y) / (mItemSize.y + m_Margin.y)), 1);
				mColumns = Mathf.CeilToInt(count / (float)mRows);
				float width = mColumns * (mItemSize.x + m_Margin.x) - m_Margin.x + m_Padding.x + m_Padding.x;
				content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
				mScrollRect.horizontal = !m_AutoDisableScroll || width > viewportRect.width;
			}
			if (focusIndex >= 0) {
				Vector2 pos = content.anchoredPosition;
				if (m_Direction == eDirection.Vertical) {
					int r = Mathf.FloorToInt(focusIndex / (float)mColumns);
					pos.y = Mathf.Clamp(r * (mItemSize.y + m_Margin.y), 0f, Mathf.Max(content.rect.height - viewportRect.height, 0f));
				} else {
					int c = Mathf.FloorToInt(focusIndex / (float)mRows);
					pos.x = Mathf.Clamp(-c * (mItemSize.x + m_Margin.x), Mathf.Min(viewportRect.width - content.rect.width, 0f), 0f);
				}
				content.anchoredPosition = pos;
			}
			Flush();
		}

		public void Clear() {
			if (mScrollRect == null) { return; }
			while (mItemHead.next != mItemEnd) {
				CacheItem(mItemHead.next);
			}
			mDatas = null;
			mScrollRect.normalizedPosition = Vector2.zero;
			mPrevBounds = new Bounds();
		}

		void Flush() {
			RectTransform viewport = mScrollRect.viewport;
			RectTransform content = mScrollRect.content;
			Bounds bound = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, content);
			if (bound == mPrevBounds) { return; }
			float createBuffer = m_CreateBuffer;
			float removeBuffer = m_RemoveBuffer;
			if (removeBuffer < createBuffer) {
				removeBuffer = createBuffer;
			}
			//Log.d(bound + "   " + bound.min + "    " + bound.max);
			Rect viewportRect = viewport.rect;
			if (m_Direction == eDirection.Vertical) {
				float maxY = -Mathf.Clamp(bound.max.y, 0f, content.rect.height - viewportRect.height);
				float minY = maxY - viewport.rect.height;
				maxY += mItemSize.y;
				if (bound.center.y < mPrevBounds.center.y) {
					ItemData end = mItemEnd.prev;
					while (end != mItemHead) {
						if (end.rt.anchoredPosition.y > minY - m_RemoveBuffer) { break; }
						end = end.prev;
						CacheItem(end.next);
					}
					int i = mItemHead.next.index - 1;
					while (i >= 0) {
						int r = Mathf.FloorToInt(i / (float)mColumns);
						float y = -r * (mItemSize.y + m_Margin.y) - m_Padding.y;
						if (y > maxY + m_RemoveBuffer) { break; }
						if (y > minY - m_CreateBuffer) {
							ItemData item = GetItem();
							//Log.d("New Item At " + i);
							item.index = i;
							item.next = mItemHead.next;
							item.prev = mItemHead;
							item.prev.next = item;
							item.next.prev = item;
							int c = i - r * mColumns;
							bool reverse = m_SnakeLayout && (r & 1) != 0;
							if (reverse) { c = mColumns - 1 - c; }
							item.rt.anchoredPosition = new Vector2(c * (mItemSize.x + m_Margin.x) + m_Padding.x, y);
							Apply(item.go, i, mDatas[i], r, c, reverse);
						}
						i--;
					}
				} else {
					ItemData head = mItemHead.next;
					while (head != mItemEnd) {
						if (head.rt.anchoredPosition.y < maxY + m_RemoveBuffer) { break; }
						head = head.next;
						CacheItem(head.prev);
					}
					int i = mItemEnd.prev.index + 1;
					while (i < mDatas.Length) {
						int r = Mathf.FloorToInt(i / (float)mColumns);
						float y = -r * (mItemSize.y + m_Margin.y) - m_Padding.y;
						if (y < minY - m_RemoveBuffer) { break; }
						if (y < maxY + m_CreateBuffer) {
							ItemData item = GetItem();
							//Log.d("New Item At " + i);
							item.index = i;
							item.next = mItemEnd;
							item.prev = mItemEnd.prev;
							item.prev.next = item;
							item.next.prev = item;
							int c = i - r * mColumns;
							bool reverse = m_SnakeLayout && (r & 1) != 0;
							if (reverse) { c = mColumns - 1 - c; }
							item.rt.anchoredPosition = new Vector2(c * (mItemSize.x + m_Margin.x) + m_Padding.x, y);
							Apply(item.go, i, mDatas[i], r, c, reverse);
						}
						i++;
					}
				}
			} else {
				float minX = -Mathf.Clamp(bound.min.x, viewportRect.width - content.rect.width, 0f);
				float maxX = minX + viewportRect.width;
				minX -= mItemSize.x;
				if (bound.center.x < mPrevBounds.center.x) {
					ItemData head = mItemHead.next;
					while (head != mItemEnd) {
						if (head.rt.anchoredPosition.x > minX - m_RemoveBuffer) { break; }
						head = head.next;
						CacheItem(head.prev);
					}
					int i = mItemEnd.prev.index + 1;
					while (i < mDatas.Length) {
						int c = Mathf.FloorToInt(i / (float)mRows);
						float x = c * (mItemSize.x + m_Margin.x) + m_Padding.x;
						if (x > maxX + m_RemoveBuffer) { break; }
						if (x > minX - m_CreateBuffer) {
							ItemData item = GetItem();
							//Log.d("New Item At " + i);
							item.index = i;
							item.next = mItemEnd;
							item.prev = mItemEnd.prev;
							item.prev.next = item;
							item.next.prev = item;
							int r = i - c * mRows;
							bool reverse = m_SnakeLayout && (c & 1) != 0;
							if (reverse) { r = mColumns - 1 - r; }
							item.rt.anchoredPosition = new Vector2(x, -r * (mItemSize.y + m_Margin.y) - m_Padding.y);
							Apply(item.go, i, mDatas[i], r, c, reverse);
						}
						i++;
					}
				} else {
					ItemData end = mItemEnd.prev;
					while (end != mItemHead) {
						if (end.rt.anchoredPosition.x < maxX + m_RemoveBuffer) { break; }
						end = end.prev;
						CacheItem(end.next);
					}
					int i = mItemHead.next.index - 1;
					while (i >= 0) {
						int c = Mathf.FloorToInt(i / (float)mRows);
						float x = c * (mItemSize.x + m_Margin.x) + m_Padding.x;
						if (x < minX - m_RemoveBuffer) { break; }
						if (x < maxX + m_CreateBuffer) {
							ItemData item = GetItem();
							//Log.d("New Item At " + i);
							item.index = i;
							item.next = mItemHead.next;
							item.prev = mItemHead;
							item.prev.next = item;
							item.next.prev = item;
							int r = i - c * mRows;
							bool reverse = m_SnakeLayout && (c & 1) != 0;
							if (reverse) { r = mColumns - 1 - r; }
							item.rt.anchoredPosition = new Vector2(x, -r * (mItemSize.y + m_Margin.y) - m_Padding.y);
							Apply(item.go, i, mDatas[i], r, c, reverse);
						}
						i--;
					}
				}
			}
			mPrevBounds = bound;
		}

		private void Apply(GameObject go, int i, object data, int row, int column, bool reversed) {
			if (mOnApply != null) {
				ePointerType prevDirType = ePointerType.Left;
				ePointerType nextDirType = ePointerType.Right;
				if (m_Direction == eDirection.Vertical) {
					if (reversed) {
						prevDirType = ePointerType.Right;
						nextDirType = ePointerType.Left;
						if (column == 0) {
							nextDirType = ePointerType.Down;
						}
						if (column == mColumns - 1) {
							prevDirType = ePointerType.Up;
						}
					} else {
						prevDirType = ePointerType.Left;
						nextDirType = ePointerType.Right;
						if (column == 0) {
							prevDirType = m_SnakeLayout ? ePointerType.Up : ePointerType.NewRowOrColumn;
						}
						if (column == mColumns - 1) {
							nextDirType = m_SnakeLayout ? ePointerType.Down : ePointerType.NewRowOrColumn;
						}
					}
				} else {
					if (reversed) {
						prevDirType = ePointerType.Down;
						nextDirType = ePointerType.Up;
						if (row == 0) {
							nextDirType = ePointerType.Right;
						}
						if (row == mRows - 1) {
							prevDirType = ePointerType.Left;
						}
					} else {
						prevDirType = ePointerType.Up;
						nextDirType = ePointerType.Down;
						if (row == 0) {
							prevDirType = m_SnakeLayout ? ePointerType.Left : ePointerType.NewRowOrColumn;
						}
						if (row == mRows - 1) {
							nextDirType = m_SnakeLayout ? ePointerType.Right : ePointerType.NewRowOrColumn;
						}
					}
				}
				if (i == 0) { prevDirType = ePointerType.None; }
				if (i == mDatas.Length - 1) { nextDirType = ePointerType.None; }
				try { mOnApply(go, i, data, prevDirType, nextDirType); } catch (System.Exception e) { Debug.LogException(e); }
			}
		}

		private ItemData GetItem() {
			ItemData item = mCachedItemHead.next;
			if (item == mCachedItemEnd) {
				item = new ItemData();
				item.go = Instantiate<GameObject>(m_Template, m_Template.transform.parent);
				item.rt = item.go.transform as RectTransform;
				//Log.d(item.go);
			} else {
				item.prev.next = item.next;
				item.next.prev = item.prev;
			}
			item.go.SetActive(true);
			return item;
		}

		private void CacheItem(ItemData item) {
			item.go.SetActive(false);
			item.prev.next = item.next;
			item.next.prev = item.prev;
			item.prev = mCachedItemEnd.prev;
			item.next = mCachedItemEnd;
			item.prev.next = item;
			item.next.prev = item;
		}

		private enum eDirection { Horizontal, Vertical }

		private class ItemData {
			public int index;
			public GameObject go;
			public RectTransform rt;
			public ItemData prev;
			public ItemData next;
		}

	}

}