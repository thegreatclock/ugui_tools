using UnityEngine;
using System.Reflection;
using System;
using UnityEngine.UI;

namespace UnityEditor.UI {

	public static class MenuOptionsEx {


		[MenuItem("GameObject/UI/Image Mirror", false, 2003)]
		public static void AddRawImage(MenuCommand menuCommand) {
			GameObject element = CreateUIElementRoot("ImageMirror", new Vector2(100f, 100f));
			element.AddComponent<ImageMirror>();
			PlaceUIElementRoot(element, menuCommand);
		}

		private static MethodInfo get_standard_resources;
		private static MethodInfo playe_ui_element_root;
		private static void PlaceUIElementRoot(GameObject element, MenuCommand menuCommand) {
			if (get_standard_resources == null) {
				Type typeMenuOptions = Type.GetType("UnityEditor.UI.MenuOptions,UnityEditor.UI");
				get_standard_resources = typeMenuOptions.GetMethod("GetStandardResources", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			}
			get_standard_resources.Invoke(null, null);
			if (playe_ui_element_root == null) {
				Type typeMenuOptions = Type.GetType("UnityEditor.UI.MenuOptions,UnityEditor.UI");
				playe_ui_element_root = typeMenuOptions.GetMethod("PlaceUIElementRoot", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			}
			playe_ui_element_root.Invoke(null, new object[] { element, menuCommand });
		}

		private static GameObject CreateUIElementRoot(string name, Vector2 size) {
			GameObject gameObject = new GameObject(name);
			RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
			rectTransform.sizeDelta = size;
			return gameObject;
		}

	}

}
