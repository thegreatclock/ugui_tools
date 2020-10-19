using System;

namespace UnityEngine.UI {

	[RequireComponent(typeof(RectTransform)), ExecuteInEditMode]
	public class UISafeAreaFit : MonoBehaviour {

		[Serializable]
		public struct Float4 {
			public float left;
			public float bottom;
			public float right;
			public float top;
			public Float4(float left, float bottom, float right, float top) {
				this.left = left;
				this.bottom = bottom;
				this.right = right;
				this.top = top;
			}
		}

		public Float4 SafeFactors {
			get { return mSafeFactors; }
			set { mSafeFactors = value; Flush(); }
		}

		public Float4 SafePaddings {
			get { return mSafePaddings; }
			set { mSafePaddings = value; Flush(); }
		}

		[SerializeField]
		private Float4 mSafeFactors = new Float4(1f, 1f, 1f, 1f);
		[SerializeField]
		private Float4 mSafePaddings = new Float4(0f, 0f, 0f, 0f);

		private RectTransform mTrans;

		void Awake() {
			mTrans = transform as RectTransform;
			Flush();
		}

		private void Flush() {
			Rect safe = Screen.safeArea;
			float left = safe.xMin / Screen.width;
			float bottom = safe.yMin / Screen.height;
			float right = 1f - safe.xMax / Screen.width;
			float top = 1f - safe.yMax / Screen.height;
			mTrans.anchorMin = new Vector2(left * mSafeFactors.left, bottom * mSafeFactors.bottom);
			mTrans.anchorMax = new Vector2(1f - right * mSafeFactors.right, 1f - top * mSafeFactors.top);
			mTrans.sizeDelta = Vector2.zero;
			mTrans.offsetMin = new Vector2(mSafePaddings.left, mSafePaddings.bottom);
			mTrans.offsetMax = new Vector2(-mSafePaddings.right, -mSafePaddings.top);
		}

	}

}
