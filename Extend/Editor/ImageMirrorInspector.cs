using System;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityEditor.UI {

	/// <summary>
	///   <para>Custom Editor for the Image Component.</para>
	/// </summary>
	[CanEditMultipleObjects, CustomEditor(typeof(ImageMirror), true)]
	public class ImageMirrorInspector : GraphicEditor {

		private SerializedProperty m_PreImage;
		private SerializedProperty m_Sliced;
		private SerializedProperty m_FillCenter;
		private SerializedProperty m_Sprite;
		private SerializedProperty m_PreserveAspect;

		private GUIContent m_SpriteContent;
		private GUIContent m_PreImageContent;
		private GUIContent m_SlicedContent;
		private AnimBool m_ShowSliced;
		private AnimBool m_ShowType;

		protected override void OnEnable() {
			base.OnEnable();
			m_SpriteContent = new GUIContent("Source Image");
			m_PreImageContent = new GUIContent("Image Type");
			m_SlicedContent = new GUIContent("Sliced Image");
			m_Sprite = serializedObject.FindProperty("m_Sprite");
			m_PreImage = serializedObject.FindProperty("m_PreImage");
			m_Sliced = serializedObject.FindProperty("m_Sliced");
			m_FillCenter = serializedObject.FindProperty("m_FillCenter");
			m_PreserveAspect = serializedObject.FindProperty("m_PreserveAspect");
			m_ShowType = new AnimBool(m_Sprite.objectReferenceValue != null);
			m_ShowType.valueChanged.AddListener(Repaint);
			ImageMirror.PreImage enumValueIndex = (ImageMirror.PreImage)m_PreImage.enumValueIndex;
			m_ShowSliced = new AnimBool(!m_Sliced.hasMultipleDifferentValues && m_Sliced.boolValue);
			m_ShowSliced.valueChanged.AddListener(Repaint);
			SetShowNativeSize(true);
		}

		/// <summary>
		/// See MonoBehaviour.OnDisable.
		/// </summary>
		protected override void OnDisable() {
			m_ShowType.valueChanged.RemoveListener(Repaint);
			m_ShowSliced.valueChanged.RemoveListener(Repaint);
		}

		/// <summary>
		/// Implement specific ImageEditor inspector GUI code here. If you want to simply extend the existing editor call the base OnInspectorGUI() before doing any custom GUI code.
		/// </summary>
		public override void OnInspectorGUI() {
			serializedObject.Update();
			SpriteGUI();
			AppearanceControlsGUI();
			RaycastControlsGUI();
			m_ShowType.target = (m_Sprite.objectReferenceValue != null);
			if (EditorGUILayout.BeginFadeGroup(m_ShowType.faded)) {
				TypeGUI();
			}
			EditorGUILayout.EndFadeGroup();
			SetShowNativeSize(false);
			if (EditorGUILayout.BeginFadeGroup(m_ShowNativeSize.faded)) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(m_PreserveAspect, new GUILayoutOption[0]);
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndFadeGroup();
			NativeSizeButtonGUI();
			serializedObject.ApplyModifiedProperties();
		}

		private void SetShowNativeSize(bool instant) {
			bool show = m_Sprite.objectReferenceValue != null && !m_Sliced.boolValue;
			SetShowNativeSize(show, instant);
		}

		/// <summary>
		/// GUI for showing the Sprite property.
		/// </summary>
		protected void SpriteGUI() {
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_Sprite, m_SpriteContent, new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck()) {
				Sprite sprite = m_Sprite.objectReferenceValue as Sprite;
				if (sprite != null) {
					if (sprite.border.SqrMagnitude() > 0f) {
						m_Sliced.boolValue = true;
						m_ShowSliced.target = true;
					} else if (m_Sliced.boolValue) {
						m_Sliced.boolValue = false;
						m_ShowSliced.target = false;
					}
				}
			}
		}

		/// <summary>
		/// GUI for showing the image type and associated settings.
		/// </summary>
		protected void TypeGUI() {
			EditorGUILayout.PropertyField(m_PreImage, m_PreImageContent, new GUILayoutOption[0]);
			ImageMirror.PreImage preImg = (ImageMirror.PreImage)m_PreImage.enumValueIndex;
			bool flag = !m_Sliced.hasMultipleDifferentValues && m_Sliced.boolValue;
			if (flag && targets.Length > 1) {
				flag = (from obj in targets select obj as ImageMirror).All((ImageMirror img) => img.hasBorder);
			}
			EditorGUILayout.PropertyField(m_Sliced, m_SlicedContent);
			EditorGUI.indentLevel++;
			m_ShowSliced.target = (flag && !m_Sliced.hasMultipleDifferentValues && m_Sliced.boolValue);
			ImageMirror image = target as ImageMirror;
			if (EditorGUILayout.BeginFadeGroup(m_ShowSliced.faded)) {
				if (image.hasBorder) {
					EditorGUILayout.PropertyField(m_FillCenter);
				} else {
					EditorGUILayout.HelpBox("This Image doesn't have a border.", MessageType.Warning);
				}
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUI.indentLevel--;
		}

		/// <summary>
		/// Can this component be Previewed in its current state?
		/// </summary>
		/// <returns>True if this component can be Previewed in its current state.</returns>
		public override bool HasPreviewGUI() {
			return true;
		}

		/// <summary>
		/// Custom preview for Image component.
		/// </summary>
		/// <param name="rect">Rectangle in which to draw the preview.</param>
		/// <param name="background">Background image.</param>
		public override void OnPreviewGUI(Rect rect, GUIStyle background) {
			ImageMirror image = target as ImageMirror;
			if (!(image == null)) {
				Sprite sprite = image.sprite;
				if (!(sprite == null)) {
					GUISpriteDrawUtility.DrawSprite(sprite, rect, image.canvasRenderer.GetColor());
				}
			}
		}

		/// <summary>
		/// A string cointaining the Image details to be used as a overlay on the component Preview.
		/// </summary>
		/// <returns>The Image details.</returns>
		public override string GetInfoString() {
			ImageMirror image = target as ImageMirror;
			Sprite sprite = image.sprite;
			int num = (sprite == null) ? 0 : Mathf.RoundToInt(sprite.rect.width);
			int num2 = (sprite == null) ? 0 : Mathf.RoundToInt(sprite.rect.height);
			return string.Format("Image Size: {0}x{1}", num, num2);
		}

	}

}
