using System;
using System.Reflection;
using UnityEngine;

public static class GUISpriteDrawUtility {

	private static MethodInfo s_draw_sprite_1;
	private static object[] s_draw_sprite_1_args;
	private static MethodInfo s_draw_sprite_2;
	private static object[] s_draw_sprite_2_args;


	public static void DrawSprite(Sprite sprite, Rect drawArea, Color color) {
		if (s_draw_sprite_1 == null) {
			Type type = GetInternalType();
			if (type != null) {
				s_draw_sprite_1 = type.GetMethod("DrawSprite",
					new Type[] { typeof(Sprite), typeof(Rect), typeof(Color) });
			}
			s_draw_sprite_1_args = new object[3];
		}
		if (s_draw_sprite_1 == null) {
			Debug.Log(GetInternalType());
			Debug.LogError("Cannot find method info of 'void DrawSprite(Sprite sprite, Rect drawArea, Color color)' !");
			return;
		}
		s_draw_sprite_1_args[0] = sprite;
		s_draw_sprite_1_args[1] = drawArea;
		s_draw_sprite_1_args[2] = color;
		s_draw_sprite_1.Invoke(null, s_draw_sprite_1_args);
	}

	public static void DrawSprite(Texture tex, Rect drawArea, Rect outer, Rect uv, Color color) {
		if (s_draw_sprite_2 == null) {
			Type type = GetInternalType();
			if (type != null) {
				s_draw_sprite_2 = type.GetMethod("DrawSprite",
					new Type[] { typeof(Texture), typeof(Rect), typeof(Rect), typeof(Rect), typeof(Color) });
			}
			s_draw_sprite_2_args = new object[5];
		}
		if (s_draw_sprite_2 == null) {
			Debug.LogError("Cannot find method info of 'void DrawSprite(Sprite sprite, Rect drawArea, Color color)' !");
			return;
		}
		s_draw_sprite_2_args[0] = tex;
		s_draw_sprite_2_args[1] = drawArea;
		s_draw_sprite_2_args[2] = outer;
		s_draw_sprite_2_args[3] = uv;
		s_draw_sprite_2_args[4] = color;
		s_draw_sprite_2.Invoke(null, s_draw_sprite_2_args);

	}

	private static Type s_internal_type;
	private static Type GetInternalType() {
		if (s_internal_type == null) {
			s_internal_type = Type.GetType("UnityEditor.UI.SpriteDrawUtility,UnityEditor.UI");
		}
		return s_internal_type;
	}

}
