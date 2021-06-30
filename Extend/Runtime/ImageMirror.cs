using System;
using System.Collections.Generic;
using UnityEngine.Sprites;
using UnityEngine.U2D;

namespace UnityEngine.UI {

	/// <summary>
	/// Displays a Mirrored Sprite for the UI System.
	/// </summary>
	[AddComponentMenu("UI/Image Mirror", 11)]
	public class ImageMirror : MaskableGraphic, ISerializationCallbackReceiver, ILayoutElement, ICanvasRaycastFilter {

		/// <summary>
		/// Position of the PreImage.
		/// </summary>
		public enum PreImage {
			TopLeft, Top, TopRight, Left, Right, BottomLeft, Bottom, BottomRight
		}

		protected static Material s_ETC1DefaultUI = null;

		[SerializeField]
		private Sprite m_Sprite;

		[NonSerialized]
		private Sprite m_OverrideSprite;

		[SerializeField]
		private PreImage m_PreImage = PreImage.Left;

		[SerializeField]
		private bool m_Sliced = false;

		[SerializeField]
		private bool m_PreserveAspect = false;

		[SerializeField]
		private bool m_FillCenter = true;

		private float m_AlphaHitTestMinimumThreshold = 0f;

		// Whether this is being tracked for Atlas Binding.
		private bool m_Tracked = false;

		private static float[] cached_xs = new float[5];
		private static float[] cached_ys = new float[5];
		private static float[] cached_us = new float[5];
		private static float[] cached_vs = new float[5];

		/// <summary>
		/// The sprite that is used to render this image.
		/// </summary>
		public Sprite sprite {
			get {
				return m_Sprite;
			}
			set {
				if (m_Sprite != value) {
					Vector2 size0 = m_Sprite == null ? Vector2.zero : m_Sprite.rect.size;
					Vector2 size1 = value == null ? Vector2.zero : value.rect.size;
					Texture tex0 = m_Sprite == null ? null : m_Sprite.texture;
					Texture tex1 = value == null ? null : value.texture;
					m_SkipLayoutUpdate = size0 == size1;
					m_SkipMaterialUpdate = tex0 == tex1;
					m_Sprite = value;
					SetAllDirty();
					TrackSprite();
				}
			}
		}

		/// <summary>
		/// Set an override sprite to be used for rendering.
		/// </summary>
		public Sprite overrideSprite {
			get {
				return activeSprite;
			}
			set {
				if (m_OverrideSprite != value) {
					m_OverrideSprite = value;
					SetAllDirty();
					TrackSprite();
				}
			}
		}

		private Sprite activeSprite {
			get {
				return m_OverrideSprite == null ? sprite : m_OverrideSprite;
			}
		}

		/// <summary>
		/// The location of preImage.
		/// </summary>
		public PreImage preImage {
			get {
				return m_PreImage;
			}
			set {
				if (m_PreImage != value) {
					m_PreImage = value;
					SetVerticesDirty();
				}
			}
		}

		/// <summary>
		/// Displays the Image as a 9-sliced graphic.
		/// </summary>
		public bool sliced {
			get {
				return m_Sliced;
			}
			set {
				if (m_Sliced != value) {
					m_Sliced = value;
					SetVerticesDirty();
				}
			}
		}

		/// <summary>
		/// Whether this image should preserve its Sprite aspect ratio.
		/// </summary>
		public bool preserveAspect {
			get {
				return m_PreserveAspect;
			}
			set {
				if (m_PreserveAspect != value) {
					m_PreserveAspect = value;
					SetVerticesDirty();
				}
			}
		}

		/// <summary>
		/// Whether or not to render the center of a Tiled or Sliced image.
		/// </summary>
		public bool fillCenter {
			get {
				return m_FillCenter;
			}
			set {
				if (m_FillCenter != value) {
					m_FillCenter = value;
					SetVerticesDirty();
				}
			}
		}

		/// <summary>
		/// The alpha threshold specifies the minimum alpha a pixel must have for the event to considered a "hit" on the Image.
		/// </summary>
		public float alphaHitTestMinimumThreshold {
			get {
				return m_AlphaHitTestMinimumThreshold;
			}
			set {
				m_AlphaHitTestMinimumThreshold = value;
			}
		}

		/// <summary>
		/// Cache of the default Canvas Ericsson Texture Compression 1 (ETC1) and alpha Material.
		/// </summary>
		public static Material defaultETC1GraphicMaterial {
			get {
				if (s_ETC1DefaultUI == null) {
					s_ETC1DefaultUI = Canvas.GetETC1SupportedCanvasMaterial();
				}
				return s_ETC1DefaultUI;
			}
		}

		/// <summary>
		/// The image's texture. (ReadOnly).
		/// </summary>
		public override Texture mainTexture {
			get {
				Texture result;
				if (activeSprite == null) {
					if (material != null && material.mainTexture != null) {
						result = material.mainTexture;
					} else {
						result = Graphic.s_WhiteTexture;
					}
				} else {
					result = activeSprite.texture;
				}
				return result;
			}
		}

		/// <summary>
		/// True if the sprite used has borders.
		/// </summary>
		public bool hasBorder {
			get {
				return activeSprite != null && activeSprite.border.sqrMagnitude > 0f;
			}
		}

		[SerializeField]
		private float m_PixelsPerUnitMultiplier = 1.0f;

		/// <summary>
		/// Pixel per unit modifier to change how sliced sprites are generated.
		/// </summary>
		public float pixelsPerUnitMultiplier {
			get { return m_PixelsPerUnitMultiplier; }
			set {
				m_PixelsPerUnitMultiplier = Mathf.Max(0.01f, value);
			}
		}

		private float m_CachedReferencePixelsPerUnit = 100f;

		/// <summary>
		/// Pixels per unit considering sprite
		/// </summary>
		public float pixelsPerUnit {
			get {
				float spritePixelsPerUnit = 100f;
				if (activeSprite) {
					spritePixelsPerUnit = activeSprite.pixelsPerUnit;
				}
				if (canvas != null) {
					m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
				}
				return spritePixelsPerUnit / m_CachedReferencePixelsPerUnit;
			}
		}

		protected float multipliedPixelsPerUnit {
			get { return pixelsPerUnit * m_PixelsPerUnitMultiplier; }
		}

		/// <summary>
		/// The specified Material used by this Image. The default Material is used instead if one wasn't specified.
		/// </summary>
		public override Material material {
			get {
				Material result;
				if (m_Material != null) {
					result = m_Material;
				} else if (activeSprite && activeSprite.associatedAlphaSplitTexture != null) {
					result = defaultETC1GraphicMaterial;
				} else {
					result = defaultMaterial;
				}
				return result;
			}
			set {
				material = value;
			}
		}

		/// <summary>
		/// See ILayoutElement.minWidth.
		/// </summary>
		public virtual float minWidth {
			get {
				return 0f;
			}
		}

		/// <summary>
		/// See ILayoutElement.preferredWidth.
		/// </summary>
		public virtual float preferredWidth {
			get {
				Sprite s = activeSprite;
				if (s == null) { return 0f; }
				Rect r = s.rect;
				Vector4 b = s.border;
				float w = 0f;
				switch (preImage) {
					case PreImage.Top:
					case PreImage.Bottom:
						w = sliced && (b.x > 0f || b.z > 0f) ? (b.x + b.z) : r.width;
						break;
					case PreImage.TopLeft:
					case PreImage.Left:
					case PreImage.BottomLeft:
						w = (sliced && b.x > 0f ? b.x : (r.width - b.z)) * 2f;
						break;
					case PreImage.TopRight:
					case PreImage.Right:
					case PreImage.BottomRight:
						w = (sliced && b.z > 0f ? b.z : (r.width - b.x)) * 2f;
						break;
				}
				return w / pixelsPerUnit;
			}
		}

		/// <summary>
		/// See ILayoutElement.flexibleWidth.
		/// </summary>
		public virtual float flexibleWidth {
			get {
				return -1f;
			}
		}

		/// <summary>
		/// See ILayoutElement.minHeight.
		/// </summary>
		public virtual float minHeight {
			get {
				return 0f;
			}
		}

		/// <summary>
		/// See ILayoutElement.preferredHeight.
		/// </summary>
		public virtual float preferredHeight {
			get {
				Sprite s = activeSprite;
				if (s == null) { return 0f; }
				Rect r = s.rect;
				Vector4 b = s.border;
				float h = 0f;
				switch (preImage) {
					case PreImage.Top:
					case PreImage.Bottom:
						h = sliced && (b.y > 0f || b.w > 0f) ? (b.y + b.w) : r.height;
						break;
					case PreImage.TopLeft:
					case PreImage.Left:
					case PreImage.BottomLeft:
						h = (sliced && b.y > 0f ? b.y : (r.height - b.w)) * 2f;
						break;
					case PreImage.TopRight:
					case PreImage.Right:
					case PreImage.BottomRight:
						h = (sliced && b.w > 0f ? b.w : (r.height - b.y)) * 2f;
						break;
				}
				return h / pixelsPerUnit;
			}
		}

		/// <summary>
		/// See ILayoutElement.flexibleHeight.
		/// </summary>
		public virtual float flexibleHeight {
			get {
				return -1f;
			}
		}

		/// <summary>
		/// See ILayoutElement.layoutPriority.
		/// </summary>
		public virtual int layoutPriority {
			get {
				return 0;
			}
		}

		protected ImageMirror() {
			useLegacyMeshGeneration = false;
		}

		/// <summary>
		/// Serialization Callback.
		/// </summary>
		public virtual void OnBeforeSerialize() { }

		/// <summary>
		/// Serialization Callback.
		/// </summary>
		public virtual void OnAfterDeserialize() { }

		/// <summary>
		/// Adjusts the image size to make it pixel-perfect.
		/// </summary>
		public override void SetNativeSize() {
			if (activeSprite != null) {
				rectTransform.anchorMax = rectTransform.anchorMin;
				rectTransform.sizeDelta = GetNativeSpriteSize() / pixelsPerUnit;
				SetAllDirty();
			}
		}

		private Vector2 GetNativeSpriteSize() {
			Sprite s = activeSprite;
			if (s == null) { return Vector2.zero; }
			Rect rect = s.rect;
			Vector4 border = s.border;
			float width = rect.width;
			float height = rect.height;
			switch (preImage) {
				case PreImage.TopLeft:
				case PreImage.Left:
				case PreImage.BottomLeft:
					width = (width - border.z) * 2f;
					break;
				case PreImage.TopRight:
				case PreImage.Right:
				case PreImage.BottomRight:
					width = (width - border.x) * 2f;
					break;
			}
			switch (preImage) {
				case PreImage.TopLeft:
				case PreImage.Top:
				case PreImage.TopRight:
					height = (height - border.y) * 2f;
					break;
				case PreImage.BottomLeft:
				case PreImage.Bottom:
				case PreImage.BottomRight:
					height = (height - border.w) * 2f;
					break;
			}
			return new Vector2(width, height);
		}

		protected override void OnPopulateMesh(VertexHelper toFill) {
			if (activeSprite == null) {
				base.OnPopulateMesh(toFill);
			} else {
				toFill.Clear();
				Sprite asp = activeSprite;
				Vector4 outer = DataUtility.GetOuterUV(asp);
				Vector4 inner = DataUtility.GetInnerUV(asp);
				Vector4 padding = DataUtility.GetPadding(asp);
				Vector4 border = asp.border;
				Rect rect = GetDrawingRect();
				border = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect);
				padding /= multipliedPixelsPerUnit;
				float cw = rect.width * 0.5f + rect.xMin;
				float ch = rect.height * 0.5f + rect.yMin;
				int cx = 0;
				int cy = 0;
				int flags = 0;
				switch (preImage) {
					case PreImage.TopLeft:
						if (sliced) {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = rect.xMin + border.x;
							cached_xs[2] = cw;
							cached_xs[3] = rect.xMax - border.x;
							cached_xs[4] = rect.xMax - padding.x;
							cached_ys[0] = rect.yMin + padding.w;
							cached_ys[1] = rect.yMin + border.w;
							cached_ys[2] = ch;
							cached_ys[3] = rect.yMax - border.w;
							cached_ys[4] = rect.yMax - padding.w;
							cached_us[0] = outer.x;
							cached_us[1] = inner.x;
							cached_us[2] = inner.z;
							cached_us[3] = inner.x;
							cached_us[4] = outer.x;
							cached_vs[0] = outer.w;
							cached_vs[1] = inner.w;
							cached_vs[2] = inner.y;
							cached_vs[3] = inner.w;
							cached_vs[4] = outer.w;
							cx = 5;
							cy = 5;
							if (!fillCenter) { flags = 1632; }
						} else {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = cw;
							cached_xs[2] = rect.xMax - padding.x;
							cached_ys[0] = rect.yMin + padding.w;
							cached_ys[1] = ch;
							cached_ys[2] = rect.yMax - padding.w;
							cached_us[0] = outer.x;
							cached_us[1] = inner.z;
							cached_us[2] = outer.x;
							cached_vs[0] = outer.w;
							cached_vs[1] = inner.y;
							cached_vs[2] = outer.w;
							cx = 3;
							cy = 3;
						}
						break;
					case PreImage.Top:
						if (sliced) {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = rect.xMin + border.x;
							cached_xs[2] = rect.xMax - border.z;
							cached_xs[3] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin + padding.w;
							cached_ys[1] = rect.yMin + border.w;
							cached_ys[2] = ch;
							cached_ys[3] = rect.yMax - border.w;
							cached_ys[4] = rect.yMax - padding.w;
							cached_us[0] = outer.x;
							cached_us[1] = inner.x;
							cached_us[2] = inner.z;
							cached_us[3] = outer.z;
							cached_vs[0] = outer.w;
							cached_vs[1] = inner.w;
							cached_vs[2] = inner.y;
							cached_vs[3] = inner.w;
							cached_vs[4] = outer.w;
							cx = 4;
							cy = 5;
							if (!fillCenter) { flags = 144; }
						} else {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin + padding.w;
							cached_ys[1] = ch;
							cached_ys[2] = rect.yMax - padding.w;
							cached_us[0] = outer.x;
							cached_us[1] = outer.z;
							cached_vs[0] = outer.w;
							cached_vs[1] = inner.y;
							cached_vs[2] = outer.w;
							cx = 2;
							cy = 3;
						}
						break;
					case PreImage.TopRight:
						if (sliced) {
							cached_xs[0] = rect.xMin + padding.z;
							cached_xs[1] = rect.xMin + border.z;
							cached_xs[2] = cw;
							cached_xs[3] = rect.xMax - border.z;
							cached_xs[4] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin + padding.w;
							cached_ys[1] = rect.yMin + border.w;
							cached_ys[2] = ch;
							cached_ys[3] = rect.yMax - border.w;
							cached_ys[4] = rect.yMax - padding.w;
							cached_us[0] = outer.z;
							cached_us[1] = inner.z;
							cached_us[2] = inner.x;
							cached_us[3] = inner.z;
							cached_us[4] = outer.z;
							cached_vs[0] = outer.w;
							cached_vs[1] = inner.w;
							cached_vs[2] = inner.y;
							cached_vs[3] = inner.w;
							cached_vs[4] = outer.w;
							cx = 5;
							cy = 5;
							if (!fillCenter) { flags = 1632; }
						} else {
							cached_xs[0] = rect.xMin + padding.z;
							cached_xs[1] = cw;
							cached_xs[2] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin + padding.w;
							cached_ys[1] = ch;
							cached_ys[2] = rect.yMax - padding.w;
							cached_us[0] = outer.z;
							cached_us[1] = inner.x;
							cached_us[2] = outer.z;
							cached_vs[0] = outer.w;
							cached_vs[1] = inner.y;
							cached_vs[2] = outer.w;
							cx = 3;
							cy = 3;
						}
						break;
					case PreImage.Left:
						if (sliced) {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = rect.xMin + border.x;
							cached_xs[2] = cw;
							cached_xs[3] = rect.xMax - border.x;
							cached_xs[4] = rect.xMax - padding.x;
							cached_ys[0] = rect.yMin + padding.y;
							cached_ys[1] = rect.yMin + border.y;
							cached_ys[2] = rect.yMax - border.w;
							cached_ys[3] = rect.yMax - padding.w;
							cached_us[0] = outer.x;
							cached_us[1] = inner.x;
							cached_us[2] = inner.z;
							cached_us[3] = inner.x;
							cached_us[4] = outer.x;
							cached_vs[0] = outer.y;
							cached_vs[1] = inner.y;
							cached_vs[2] = inner.w;
							cached_vs[3] = outer.w;
							cx = 5;
							cy = 4;
							if (!fillCenter) { flags = 96; }
						} else {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = cw;
							cached_xs[2] = rect.xMax - padding.x;
							cached_ys[0] = rect.yMin + padding.y;
							cached_ys[1] = rect.yMax - padding.w;
							cached_us[0] = outer.x;
							cached_us[1] = inner.z;
							cached_us[2] = outer.x;
							cached_vs[0] = outer.y;
							cached_vs[1] = outer.w;
							cx = 3;
							cy = 2;
						}
						break;
					case PreImage.Right:
						if (sliced) {
							cached_xs[0] = rect.xMin + padding.z;
							cached_xs[1] = rect.xMin + border.z;
							cached_xs[2] = cw;
							cached_xs[3] = rect.xMax - border.z;
							cached_xs[4] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin + padding.y;
							cached_ys[1] = rect.yMin + border.y;
							cached_ys[2] = rect.yMax - border.w;
							cached_ys[3] = rect.yMax - padding.w;
							cached_us[0] = outer.z;
							cached_us[1] = inner.z;
							cached_us[2] = inner.x;
							cached_us[3] = inner.z;
							cached_us[4] = outer.z;
							cached_vs[0] = outer.y;
							cached_vs[1] = inner.y;
							cached_vs[2] = inner.w;
							cached_vs[3] = outer.w;
							cx = 5;
							cy = 4;
							if (!fillCenter) { flags = 96; }
						} else {
							cached_xs[0] = rect.xMin + padding.z;
							cached_xs[1] = cw;
							cached_xs[2] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin + padding.y;
							cached_ys[1] = rect.yMax - padding.w;
							cached_us[0] = outer.z;
							cached_us[1] = inner.x;
							cached_us[2] = outer.z;
							cached_vs[0] = outer.y;
							cached_vs[1] = outer.w;
							cx = 3;
							cy = 2;
						}
						break;
					case PreImage.BottomLeft:
						if (sliced) {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = rect.xMin + border.x;
							cached_xs[2] = cw;
							cached_xs[3] = rect.xMax - border.x;
							cached_xs[4] = rect.xMax - padding.x;
							cached_ys[0] = rect.yMin + padding.y;
							cached_ys[1] = rect.yMin + border.y;
							cached_ys[2] = ch;
							cached_ys[3] = rect.yMax - border.y;
							cached_ys[4] = rect.yMax - padding.y;
							cached_us[0] = outer.x;
							cached_us[1] = inner.x;
							cached_us[2] = inner.z;
							cached_us[3] = inner.x;
							cached_us[4] = outer.x;
							cached_vs[0] = outer.y;
							cached_vs[1] = inner.y;
							cached_vs[2] = inner.w;
							cached_vs[3] = inner.y;
							cached_vs[4] = outer.y;
							cx = 5;
							cy = 5;
							if (!fillCenter) { flags = 1632; }
						} else {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = cw;
							cached_xs[2] = rect.xMax - padding.x;
							cached_ys[0] = rect.yMin - padding.y;
							cached_ys[1] = ch;
							cached_ys[2] = rect.yMax - padding.y;
							cached_us[0] = outer.x;
							cached_us[1] = inner.z;
							cached_us[2] = outer.x;
							cached_vs[0] = outer.y;
							cached_vs[1] = inner.w;
							cached_vs[2] = outer.y;
							cx = 3;
							cy = 3;
						}
						break;
					case PreImage.Bottom:
						if (sliced) {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = rect.xMin + border.x;
							cached_xs[2] = rect.xMax - border.z;
							cached_xs[3] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin + padding.y;
							cached_ys[1] = rect.yMin + border.y;
							cached_ys[2] = ch;
							cached_ys[3] = rect.yMax - border.y;
							cached_ys[4] = rect.yMax - padding.y;
							cached_us[0] = outer.x;
							cached_us[1] = inner.x;
							cached_us[2] = inner.z;
							cached_us[3] = outer.z;
							cached_vs[0] = outer.y;
							cached_vs[1] = inner.y;
							cached_vs[2] = inner.w;
							cached_vs[3] = inner.y;
							cached_vs[4] = outer.y;
							cx = 4;
							cy = 5;
							if (!fillCenter) { flags = 144; }
						} else {
							cached_xs[0] = rect.xMin + padding.x;
							cached_xs[1] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin - padding.y;
							cached_ys[1] = ch;
							cached_ys[2] = rect.yMax - padding.y;
							cached_us[0] = outer.x;
							cached_us[1] = outer.z;
							cached_vs[0] = outer.y;
							cached_vs[1] = inner.w;
							cached_vs[2] = outer.y;
							cx = 2;
							cy = 3;
						}
						break;
					case PreImage.BottomRight:
						if (sliced) {
							cached_xs[0] = rect.xMin + padding.z;
							cached_xs[1] = rect.xMin + border.z;
							cached_xs[2] = cw;
							cached_xs[3] = rect.xMax - border.z;
							cached_xs[4] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin + padding.y;
							cached_ys[1] = rect.yMin + border.y;
							cached_ys[2] = ch;
							cached_ys[3] = rect.yMax - border.y;
							cached_ys[4] = rect.yMax - padding.y;
							cached_us[0] = outer.z;
							cached_us[1] = inner.z;
							cached_us[2] = inner.x;
							cached_us[3] = inner.z;
							cached_us[4] = outer.z;
							cached_vs[0] = outer.y;
							cached_vs[1] = inner.y;
							cached_vs[2] = inner.w;
							cached_vs[3] = inner.y;
							cached_vs[4] = outer.y;
							cx = 5;
							cy = 5;
							if (!fillCenter) { flags = 1632; }
						} else {
							cached_xs[0] = rect.xMin + padding.z;
							cached_xs[1] = cw;
							cached_xs[2] = rect.xMax - padding.z;
							cached_ys[0] = rect.yMin - padding.y;
							cached_ys[1] = ch;
							cached_ys[2] = rect.yMax - padding.y;
							cached_us[0] = outer.z;
							cached_us[1] = inner.x;
							cached_us[2] = outer.z;
							cached_vs[0] = outer.y;
							cached_vs[1] = inner.w;
							cached_vs[2] = outer.y;
							cx = 3;
							cy = 3;
						}
						break;
				}
				AddQuads(toFill, cx, cy, cached_xs, cached_ys, color, cached_us, cached_vs, ~flags);
			}
		}

		private void TrackSprite() {
			if (activeSprite != null && activeSprite.texture == null) {
				TrackImage(this);
				m_Tracked = true;
			}
		}

		protected override void OnEnable() {
			base.OnEnable();
			TrackSprite();
		}

		protected override void OnDisable() {
			base.OnDisable();
			if (m_Tracked) {
				UnTrackImage(this);
			}
		}

		protected override void UpdateMaterial() {
			base.UpdateMaterial();
			if (activeSprite == null) {
				canvasRenderer.SetAlphaTexture(null);
			} else {
				Texture2D associatedAlphaSplitTexture = activeSprite.associatedAlphaSplitTexture;
				if (associatedAlphaSplitTexture != null) {
					canvasRenderer.SetAlphaTexture(associatedAlphaSplitTexture);
				}
			}
		}

		private static void AddQuads(VertexHelper vertexHelper, int w, int h, float[] xs, float[] ys, Color32 color, float[] us, float[] vs, int flags) {
			int index = vertexHelper.currentVertCount;
			int m = 1;
			for (int j = 1; j < h; j++) {
				int jj = j - 1;
				float y0 = ys[jj];
				float y1 = ys[j];
				float v0 = vs[jj];
				float v1 = vs[j];
				for (int i = 1; i < w; i++, m <<= 1) {
					if ((flags & m) == 0) { continue; }
					int ii = i - 1;
					float x0 = xs[ii];
					float x1 = xs[i];
					float u0 = us[ii];
					float u1 = us[i];
					vertexHelper.AddVert(new Vector3(x0, y0, 0f), color, new Vector2(u0, v0));
					vertexHelper.AddVert(new Vector3(x0, y1, 0f), color, new Vector2(u0, v1));
					vertexHelper.AddVert(new Vector3(x1, y1, 0f), color, new Vector2(u1, v1));
					vertexHelper.AddVert(new Vector3(x1, y0, 0f), color, new Vector2(u1, v0));
					vertexHelper.AddTriangle(index, index + 1, index + 2);
					vertexHelper.AddTriangle(index + 2, index + 3, index);
					index += 4;
				}
			}
		}

		private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect) {
			Rect rect = rectTransform.rect;
			for (int i = 0; i <= 1; i++) {
				if (rect.size[i] != 0f) {
					float num = adjustedRect.size[i] / rect.size[i];
					border[i] = border[i] * num;
					border[i + 2] = border[i + 2] * num;
				}
				float num2 = border[i] + border[i + 2];
				if (adjustedRect.size[i] < num2 && num2 != 0f) {
					float num = adjustedRect.size[i] / num2;
					border[i] = border[i] * num;
					border[i + 2] = border[i + 2] * num;
				}
			}
			return border;
		}

		private Rect GetDrawingRect() {
			Rect rect = GetPixelAdjustedRect();
			if (!sliced && preserveAspect) {
				Vector2 size = GetNativeSpriteSize();
				if (size.x > 0f && size.y > 0f) {
					float aspectSprite = size.x / size.y;
					float aspectRect = rect.width / rect.height;
					if (aspectSprite > aspectRect) {
						float height = rect.height;
						rect.height = rect.width / aspectSprite;
						rect.y += (height - rect.height) * rectTransform.pivot.y;
					} else {
						float width = rect.width;
						rect.width = rect.height * aspectSprite;
						rect.x += (width - rect.width) * rectTransform.pivot.x;
					}
				}
			}
			return rect;
		}

		/// <summary>
		/// See ILayoutElement.CalculateLayoutInputHorizontal.
		/// </summary>
		public virtual void CalculateLayoutInputHorizontal() { }

		/// <summary>
		/// See ILayoutElement.CalculateLayoutInputVertical.
		/// </summary>
		public virtual void CalculateLayoutInputVertical() { }

		/// <summary>
		/// See:ICanvasRaycastFilter.
		/// </summary>
		/// <param name="screenPoint"></param>
		/// <param name="eventCamera"></param>
		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) {
			bool result;
			Vector2 local;
			Sprite asp = activeSprite;
			if (alphaHitTestMinimumThreshold <= 0f) {
				result = true;
			} else if (alphaHitTestMinimumThreshold > 1f) {
				result = false;
			} else if (asp == null) {
				result = true;
			} else if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local)) {
				result = false;
			} else {
				Rect rect = GetDrawingRect();
				Vector4 padding = DataUtility.GetPadding(asp) / pixelsPerUnit;
				rect.Set(rect.x + padding.x, rect.y + padding.y,
					rect.width - padding.x - padding.z, rect.height - padding.y - padding.w);
				if (local.x < rect.xMin || local.x > rect.xMax || local.y < rect.yMin || local.y > rect.yMax) { return false; }
				local.x = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
				local.y = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
				Vector4 outer = DataUtility.GetOuterUV(asp);
				Vector4 inner = DataUtility.GetInnerUV(asp);
				Vector4 border = GetAdjustedBorders(asp.border / pixelsPerUnit, rect);
				Vector2 uv = Vector2.zero;
				float x, y;
				switch (preImage) {
					case PreImage.TopLeft:
					case PreImage.Left:
					case PreImage.BottomLeft:
						x = 1f - Mathf.Abs(1f - 2f * local.x);
						if (sliced) {
							x *= rect.width * 0.5f;
							if (x < border.x) {
								uv.x = Mathf.Lerp(outer.x, inner.x, Mathf.InverseLerp(0f, border.x, x));
							} else {
								uv.x = Mathf.Lerp(inner.x, inner.z, Mathf.InverseLerp(border.x, rect.width * 0.5f, x));
							}
						} else {
							uv.x = Mathf.Lerp(outer.x, inner.z, x);
						}
						break;
					case PreImage.TopRight:
					case PreImage.Right:
					case PreImage.BottomRight:
						x = 1f - Mathf.Abs(1f - 2f * local.x);
						if (sliced) {
							x *= rect.width * 0.5f;
							if (x < border.z) {
								uv.x = Mathf.Lerp(outer.z, inner.z, Mathf.InverseLerp(0f, border.z, x));
							} else {
								uv.x = Mathf.Lerp(inner.z, inner.x, Mathf.InverseLerp(border.z, rect.width * 0.5f, x));
							}
						} else {
							uv.x = Mathf.Lerp(outer.z, inner.x, x);
						}
						break;
					default:
						if (sliced) {
							x = local.x * rect.width;
							if (x < border.x) {
								uv.x = Mathf.Lerp(outer.x, inner.x, Mathf.InverseLerp(0f, border.x, x));
							} else if (x > rect.width - border.z) {
								uv.x = Mathf.Lerp(inner.z, outer.z, Mathf.InverseLerp(rect.width - border.z, rect.width, x));
							} else {
								uv.x = Mathf.Lerp(inner.x, inner.z, Mathf.InverseLerp(border.x, rect.width - border.z, x));
							}
						} else {
							uv.x = Mathf.Lerp(outer.x, outer.z, local.x);
						}
						break;
				}
				switch (preImage) {
					case PreImage.TopLeft:
					case PreImage.Top:
					case PreImage.TopRight:
						y = 1f - Mathf.Abs(1f - 2f * local.y);
						if (sliced) {
							y *= rect.height * 0.5f;
							if (y < border.w) {
								uv.y = Mathf.Lerp(outer.w, inner.w, Mathf.InverseLerp(0f, border.w, y));
							} else {
								uv.y = Mathf.Lerp(inner.w, inner.y, Mathf.InverseLerp(border.w, rect.height * 0.5f, y));
							}
						} else {
							uv.y = Mathf.Lerp(outer.w, inner.y, y);
						}
						break;
					case PreImage.BottomLeft:
					case PreImage.Bottom:
					case PreImage.BottomRight:
						y = 1f - Math.Abs(1f - 2f * local.y);
						if (sliced) {
							y *= rect.height * 0.5f;
							if (y < border.y) {
								uv.y = Mathf.Lerp(outer.y, inner.y, Mathf.InverseLerp(0f, border.y, y));
							} else {
								uv.y = Mathf.Lerp(inner.y, inner.w, Mathf.InverseLerp(border.y, rect.height * 0.5f, y));
							}
						} else {
							uv.y = Mathf.Lerp(outer.y, inner.w, y);
						}
						break;
					default:
						if (sliced) {
							y = local.y * rect.height;
							if (y < border.y) {
								uv.y = Mathf.Lerp(outer.y, inner.y, Mathf.InverseLerp(0f, border.y, y));
							} else if (y > rect.height - border.w) {
								uv.y = Mathf.Lerp(inner.w, outer.w, Mathf.InverseLerp(rect.height - border.w, rect.height, y));
							} else {
								uv.y = Mathf.Lerp(inner.y, inner.w, Mathf.InverseLerp(border.y, rect.height - border.w, y));
							}
						} else {
							uv.y = Mathf.Lerp(outer.y, outer.w, local.y);
						}
						break;
				}
				try {
					result = (!sliced || fillCenter || (uv.x <= inner.x || uv.x >= inner.z || uv.y <= inner.y || uv.y >= inner.w)) &&
						asp.texture.GetPixelBilinear(uv.x, uv.y).a > alphaHitTestMinimumThreshold;
				} catch (UnityException e) {
					Debug.LogError("Using alphaHitTestMinimumThreshold greater than 0 on Image whose sprite texture cannot be read. " + e.Message + " Also make sure to disable sprite packing for this sprite.", this);
					result = true;
				}
			}
			return result;
		}

		static List<ImageMirror> m_TrackedTexturelessImages = new List<ImageMirror>();
		static bool s_Initialized;

		static void RebuildImage(SpriteAtlas spriteAtlas) {
			for (var i = m_TrackedTexturelessImages.Count - 1; i >= 0; i--) {
				var g = m_TrackedTexturelessImages[i];
				if (null != g.activeSprite && spriteAtlas.CanBindTo(g.activeSprite)) {
					g.SetAllDirty();
					m_TrackedTexturelessImages.RemoveAt(i);
				}
			}
		}

		private static void TrackImage(ImageMirror g) {
			if (!s_Initialized) {
				SpriteAtlasManager.atlasRegistered += RebuildImage;
				s_Initialized = true;
			}
			m_TrackedTexturelessImages.Add(g);
		}

		private static void UnTrackImage(ImageMirror g) {
			m_TrackedTexturelessImages.Remove(g);
		}

		protected override void OnDidApplyAnimationProperties() {
			SetMaterialDirty();
			SetVerticesDirty();
		}

#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();
			m_PixelsPerUnitMultiplier = Mathf.Max(0.01f, m_PixelsPerUnitMultiplier);
		}
#endif

	}

}
