using UnityEditor;

namespace UnityEngine.UI {

	[CanEditMultipleObjects, CustomEditor(typeof(UISafeAreaFit))]
	public class UISafeAreaFitInspector : Editor {

		private UISafeAreaFit mFit;

		void OnEnable() {
			mFit = target as UISafeAreaFit;
		}

		public override void OnInspectorGUI() {
			UISafeAreaFit.Float4 factors = mFit.SafeFactors;
			UISafeAreaFit.Float4 paddings = mFit.SafePaddings;
			if (DrawFloat4("Safe Factors", ref factors)) {
				mFit.SafeFactors = factors;
			}
			if (DrawFloat4("Safe Paddings", ref paddings)) {
				mFit.SafePaddings = paddings;
			}
		}

		private static bool DrawFloat4(string label, ref UISafeAreaFit.Float4 float4) {
			GUILayout.Label(label);
			GUILayout.BeginHorizontal();
			GUILayout.Space(8f);
			GUILayout.BeginVertical();
			EditorGUI.BeginChangeCheck();
			float4.left = EditorGUILayout.FloatField("Left", float4.left);
			float4.bottom = EditorGUILayout.FloatField("Bottom", float4.bottom);
			float4.right = EditorGUILayout.FloatField("Right", float4.right);
			float4.top = EditorGUILayout.FloatField("Top", float4.top);
			bool ret = EditorGUI.EndChangeCheck();
			GUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			return ret;
		}

	}

}
