using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

using TexturePacker = GreatClock.Common.UGUITools.UITexturePacker;

namespace GreatClock.Common.UGUITools {

	public class SpriteAtlasPacker : EditorWindow {

		private static string ATLAS_SETTINGS_PATH = "ProjectSettings/SpriteAtlasSettings.asset";

		[MenuItem("GreatClock/uGUI Tools/Open Atlas Window")]
		public static void AtlasPacker() {
			SpriteAtlasPacker window = GetWindow<SpriteAtlasPacker>(false, "Sprite Atlas", true);
			window.minSize = new Vector2(360f, 200f);
			window.Show();
		}

		[MenuItem("GreatClock/uGUI Tools/Manual Pack Sprite Atlas")]
		static void ManualPackSpriteAtlas() {
			string folder = EditorUtility.OpenFolderPanel("Select Atlas Images Folder", prev_sprites_folder, "");
			if (string.IsNullOrEmpty(folder)) { return; }
			string folderName = Path.GetFileName(folder);
			string atlasPath = EditorUtility.SaveFilePanelInProject("Select Atlas Sprite Path", folderName, "png", "", prev_atlas_folder);
			if (string.IsNullOrEmpty(atlasPath)) { return; }
			prev_sprites_folder = Path.GetDirectoryName(folder).Replace('\\', '/');
			prev_atlas_folder = Path.GetDirectoryName(atlasPath).Replace('\\', '/');
			Pack(false, folder, atlasPath, default_padding, default_max_size,
				default_trim_alpha, default_unity_packing, default_force_square);
		}

		[MenuItem("GreatClock/uGUI Tools/Pack All Sprite Atlases")]
		static void PackAllSpriteAtlases() {
			ConfigRoot config = LoadAtlasConfig();
			if (config == null) { return; }
			AtlasSetting[] settings = config.atlasSettings;
			if (settings == null) { return; }
			for (int i = 0, imax = settings.Length; i < imax; i++) {
				AtlasSetting setting = settings[i];
				Pack(true, setting.texturesFolder, setting.atlasPath, setting.padding, setting.maxSize,
					setting.trimAlpha, setting.unityPacking, setting.forceSquare);
			}
		}

		#region saved preferences
		private static int default_padding {
			get {
				return EditorPrefs.GetInt(GetPrefKey("default_padding"), 1);
			}
			set {
				EditorPrefs.SetInt(GetPrefKey("default_padding"), value);
			}
		}

		private static int default_max_size {
			get {
				return EditorPrefs.GetInt(GetPrefKey("default_max_size"), 2048);
			}
			set {
				EditorPrefs.SetInt(GetPrefKey("default_max_size"), value);
			}
		}

		private static bool default_unity_packing {
			get {
				return EditorPrefs.GetBool(GetPrefKey("default_unity_packing"), false);
			}
			set {
				EditorPrefs.SetBool(GetPrefKey("default_unity_packing"), value);
			}
		}

		private static bool default_force_square {
			get {
				return EditorPrefs.GetBool(GetPrefKey("default_force_square"), true);
			}
			set {
				EditorPrefs.SetBool(GetPrefKey("default_force_square"), value);
			}
		}

		private static bool default_trim_alpha {
			get {
				return EditorPrefs.GetBool(GetPrefKey("default_trim_alpha"), false);
			}
			set {
				EditorPrefs.SetBool(GetPrefKey("default_trim_alpha"), value);
			}
		}

		private static string prev_sprites_folder {
			get {
				return EditorPrefs.GetString(GetPrefKey("prev_sprites_folder"), ".");
			}
			set {
				EditorPrefs.SetString(GetPrefKey("prev_sprites_folder"), value);
			}
		}

		private static string prev_atlas_folder {
			get {
				return EditorPrefs.GetString(GetPrefKey("prev_atlas_folder"), "Assets");
			}
			set {
				EditorPrefs.SetString(GetPrefKey("prev_atlas_folder"), value);
			}
		}

		private static string GetPrefKey(string key) {
			byte[] bytes = Encoding.UTF8.GetBytes(Application.dataPath + "::" + key);
			MD5 md5 = new MD5CryptoServiceProvider();
			bytes = md5.ComputeHash(bytes);
			return BitConverter.ToString(bytes).Replace("-", "");
		}
		#endregion

		private static void SaveAtlasConfig(List<AtlasSetting> settings) {
			FileStream fs = new FileStream(ATLAS_SETTINGS_PATH, FileMode.Create, FileAccess.Write);
			StreamWriter sw = new StreamWriter(fs);
			XmlSerializer serializer = new XmlSerializer(typeof(ConfigRoot));
			ConfigRoot root = new ConfigRoot();
			root.atlasSettings = settings.ToArray();
			serializer.Serialize(sw, root);
			sw.Close();
			fs.Close();
		}

		private static ConfigRoot LoadAtlasConfig() {
			ConfigRoot cfg = null;
			if (File.Exists(ATLAS_SETTINGS_PATH)) {
				StreamReader reader = File.OpenText(ATLAS_SETTINGS_PATH);
				XmlSerializer serializer = new XmlSerializer(typeof(ConfigRoot));
				cfg = serializer.Deserialize(reader) as ConfigRoot;
				reader.Close();
			}
			if (cfg == null) {
				cfg = new ConfigRoot();
				cfg.atlasSettings = new AtlasSetting[0];
			}
			return cfg;
		}

		static void Pack(bool noDialog, string folder, string atlasPath, int padding, int maxSize, bool trimAlpha, bool unityPacking, bool forceSquare) {
			if (string.IsNullOrEmpty(folder) || !IsAtlasPathValid(atlasPath) ||
				padding < 0 || maxSize < 32 || ((maxSize - 1) & maxSize) != 0) {
				return;
			}
			string atlasName = Path.GetFileNameWithoutExtension(atlasPath);
			const float progress_gathing = 0.05f;
			const float progress_loading = 0.7f;
			const float progress_packing = 0.95f;
			string progressTitle = string.Format("Packing Sprite Atlas '{0}'", atlasName);
			EditorUtility.DisplayProgressBar(progressTitle, "Gathering Files ...", 0f);
			Dictionary<string, SpriteMetaData> sprites = new Dictionary<string, SpriteMetaData>();
			TextureImporter ti = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
			if (ti != null && ti.spritesheet != null) {
				SpriteMetaData[] spritesheet = ti.spritesheet;
				for (int i = 0, imax = spritesheet.Length; i < imax; i++) {
					SpriteMetaData data = spritesheet[i];
					if (string.IsNullOrEmpty(data.name)) { continue; }
					if (sprites.ContainsKey(data.name)) {
						Debug.LogErrorFormat("Duplicated sprite name '{0}' !", data.name);
						continue;
					}
					sprites.Add(data.name, data);
				}
			}
			List<Texture2D> textures = new List<Texture2D>();
			string[] paths = Directory.GetFiles(folder);
			for (int i = 0, imax = paths.Length; i < imax; i++) {
				string path = paths[i];
				string fileName = Path.GetFileNameWithoutExtension(path);
				EditorUtility.DisplayProgressBar(progressTitle,
					string.Format("({0}/{1}) Loading '{2}' ...", (i + 1), imax, fileName),
					Mathf.Lerp(progress_gathing, progress_loading, i / (float)imax));
				string ext = Path.GetExtension(path).ToLower();
				if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") {
					continue;
				}
				byte[] bytes = null;
				try {
					bytes = File.ReadAllBytes(path);
				} catch (Exception e) {
					Debug.LogException(e);
				}
				if (bytes == null) { continue; }
				Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
				tex.LoadImage(bytes, false);
				tex.name = fileName;
				if (trimAlpha) {
					Color32[] pixels = tex.GetPixels32();
					int texW = tex.width;
					int texH = tex.height;
					int xMin = texW;
					int xMax = 0;
					int yMin = texH;
					int yMax = 0;
					for (int y = 0; y < texH; y++) {
						int indexFrom = y * texW;
						for (int x = 0; x < texW; x++) {
							Color32 color = pixels[indexFrom + x];
							if (color.a != 0) {
								if (xMin > x) { xMin = x; }
								if (xMax < x) { xMax = x; }
								if (yMin > y) { yMin = y; }
								if (yMax < y) { yMax = y; }
							}
						}
					}
					int nW = xMax - xMin + 1;
					int nH = yMax - yMin + 1;
					if (nW != texW || nH != texH) {
						if (nW > 0 && nH > 0) {
							DestroyImmediate(tex);
							Color32[] nPixels = new Color32[nW * nH];
							int count = 0;
							for (int y = yMin; y <= yMax; y++) {
								Array.Copy(pixels, y * texW + xMin, nPixels, count, nW);
								count += nW;
							}
							tex = new Texture2D(nW, nH, TextureFormat.RGBA32, false, false);
							tex.name = fileName;
							tex.SetPixels32(nPixels);
						} else {
							tex.Resize(1, 1, TextureFormat.RGBA32, false);
							tex.SetPixel(0, 0, Color.clear);
						}
						tex.Apply(false, false);
					}
				}
				tex.wrapMode = TextureWrapMode.Clamp;
				tex.filterMode = FilterMode.Trilinear;
				textures.Add(tex);
			}
			EditorUtility.DisplayProgressBar(progressTitle, string.Format("Packing Atlas '{0}' ...", atlasName), progress_packing);
			Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
			float scale = 1f;
			Rect[] rects = null;
			if (unityPacking) {
				rects = atlas.PackTextures(textures.ToArray(), padding, maxSize, false);
			} else {
				rects = TexturePacker.PackTextures(atlas, textures.ToArray(), 4, 4, padding, maxSize, forceSquare, out scale);
			}
			if (rects == null) {
				EditorUtility.ClearProgressBar();
				string errorTitle = "Sprite Atlas Packer";
				string errorContent = string.Format("Fail to generate atlas '{0}' ! Perhaps it is because the atlas is not big enough .", atlasName);
				Debug.LogErrorFormat("<color=white>[{0}]</color> {1}", errorTitle, errorContent);
				if (!noDialog) {
					EditorUtility.DisplayDialog(errorTitle, errorContent, "OK");
				}
				return;
			}
			int atlasWidth = atlas.width;
			int atlasHeight = atlas.height;
			SpriteMetaData[] spriteDatas = new SpriteMetaData[textures.Count];
			for (int i = 0, imax = textures.Count; i < imax; i++) {
				Texture2D tex = textures[i];
				string spriteName = tex.name;
				Rect rect = rects[i];
				rect.x *= atlasWidth;
				rect.y *= atlasHeight;
				rect.width *= atlasWidth;
				rect.height *= atlasHeight;
				SpriteMetaData data;
				if (sprites.TryGetValue(spriteName, out data)) {
					sprites.Remove(spriteName);
				} else {
					data = new SpriteMetaData();
					data.name = spriteName;
					data.alignment = 0;
					data.pivot = new Vector2(0.5f, 0.5f);
				}
				data.rect = rect;
				spriteDatas[i] = data;
				if (unityPacking) {
					scale *= rect.width * rect.height / (tex.width * tex.height);
				}
				DestroyImmediate(tex);
			}
			if (unityPacking) {
				scale = Mathf.Pow(scale, 0.5f / textures.Count);
			}
			File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
			DestroyImmediate(atlas);
			if (ti != null) {
				ti.textureType = TextureImporterType.Sprite;
				ti.spriteImportMode = SpriteImportMode.Multiple;
				ti.spritesheet = spriteDatas;
				ti.maxTextureSize = maxSize;
			}
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			if (ti == null) {
				ti = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
				ti.textureType = TextureImporterType.Sprite;
				ti.spriteImportMode = SpriteImportMode.Multiple;
				ti.sRGBTexture = true;
				ti.mipmapEnabled = false;
				ti.alphaIsTransparency = true;
				ti.spritesheet = spriteDatas;
				ti.maxTextureSize = maxSize;
				ti.SaveAndReimport();
			}
			EditorUtility.ClearProgressBar();
			if (scale != 1f) {
				string errorTitle = "Sprite Atlas Packer";
				string errorContent = string.Format("Sprites in atlas '{0}' are scaled at average factor '{1}%' !\nBorders may not work properly !",
					atlasName, scale * 100f);
				Debug.LogWarningFormat("<color=white>[{0}]</color> {1}", errorTitle, errorContent);
				if (!noDialog) {
					EditorUtility.DisplayDialog(errorTitle, errorContent, "OK");
				}
			}
			Texture2D target = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
			Selection.activeObject = target;
			EditorGUIUtility.PingObject(target);
		}

		private static void PackSingleAtlas(AtlasSetting setting) {
			Pack(false, setting.texturesFolder, setting.atlasPath, setting.padding, setting.maxSize,
				setting.trimAlpha, setting.unityPacking, setting.forceSquare);
		}

		private static bool IsAtlasPathValid(string path) {
			if (string.IsNullOrEmpty(path)) { return false; }
			if (!path.StartsWith("Assets/")) { return false; }
			if (Path.GetExtension(path).ToLower() != ".png") { return false; }
			return true;
		}

		#region GUI

		private bool mGUIStyleInited = false;
		private GUIStyle mStyleTextArea;
		private GUIStyle mStyleBoldLabel;
		private GUIStyle mStyleSearch;
		private GUIStyle mStyleSearchCancel;
		private GUIStyle mStyleWhiteLabel;

		private List<AtlasSetting> mSettings = null;

		private bool mDefaultUnityPacking;
		private bool mDefaultForceSquare;
		private bool mDefaultTrimAlpha;
		private int mDefaultPadding;
		private int mDefaultMaxSize;
		private string mSearchKey = "";
		private string mSearchKeyLower = "";
		private Dictionary<int, AnimBool> mHideAtlases = new Dictionary<int, AnimBool>();
		private AnimBool mDefaultAtlasShow = new AnimBool(true);

		private Vector2 mAtlasesScroll = Vector2.zero;

		void OnGUI() {
			if (!mGUIStyleInited) {
				mGUIStyleInited = true;
				mStyleTextArea = GUI.skin.FindStyle("TextArea") ?? GUI.skin.FindStyle("AS TextArea");
				mStyleBoldLabel = "BoldLabel";
				mStyleSearch = "SearchTextField";
				mStyleSearchCancel = "SearchCancelButton";
				mStyleWhiteLabel = "WhiteLabel";
			}
			if (mSettings == null) {
				mSettings = new List<AtlasSetting>();
				mSettings.AddRange(LoadAtlasConfig().atlasSettings);
				mDefaultUnityPacking = default_unity_packing;
				mDefaultForceSquare = default_force_square;
				mDefaultTrimAlpha = default_trim_alpha;
				mDefaultPadding = default_padding;
				mDefaultMaxSize = default_max_size;
			}
			Color cachedColor;
			GUILayout.Space(2f);
			EditorGUILayout.LabelField("Default Settings :", mStyleBoldLabel);
			EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(10f));
			GUILayout.Space(12f);
			EditorGUILayout.BeginHorizontal(mStyleTextArea, GUILayout.MinHeight(10f));
			GUILayout.Space(4f);
			EditorGUILayout.BeginVertical();
			EditorGUI.BeginChangeCheck();
			mDefaultUnityPacking = EditorGUILayout.ToggleLeft(" unity packing", mDefaultUnityPacking, GUILayout.Width(140f));
			if (EditorGUI.EndChangeCheck()) {
				default_unity_packing = mDefaultUnityPacking;
			}
			EditorGUI.BeginDisabledGroup(mDefaultUnityPacking);
			EditorGUI.BeginChangeCheck();
			mDefaultForceSquare = EditorGUILayout.ToggleLeft(" force square atlas", mDefaultForceSquare, GUILayout.Width(140f));
			if (EditorGUI.EndChangeCheck()) {
				default_force_square = mDefaultForceSquare;
			}
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginChangeCheck();
			mDefaultTrimAlpha = EditorGUILayout.ToggleLeft(" trim alpha", mDefaultTrimAlpha, GUILayout.Width(140f));
			if (EditorGUI.EndChangeCheck()) {
				default_trim_alpha = mDefaultTrimAlpha;
			}
			EditorGUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Padding", GUILayout.Width(56f));
			cachedColor = GUI.backgroundColor;
			GUI.backgroundColor = mDefaultPadding < 0 ? Color.red : cachedColor;
			EditorGUI.BeginChangeCheck();
			mDefaultPadding = EditorGUILayout.IntField(mDefaultPadding, GUILayout.Width(64f));
			if (EditorGUI.EndChangeCheck() && mDefaultPadding >= 0) {
				default_padding = mDefaultPadding;
			}
			GUI.backgroundColor = cachedColor;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Max Size", GUILayout.Width(56f));
			cachedColor = GUI.backgroundColor;
			GUI.backgroundColor = mDefaultMaxSize < 32 || ((mDefaultMaxSize - 1) & mDefaultMaxSize) != 0 ? Color.red : cachedColor;
			EditorGUI.BeginChangeCheck();
			mDefaultMaxSize = EditorGUILayout.IntField(mDefaultMaxSize, GUILayout.Width(64f));
			if (EditorGUI.EndChangeCheck() && mDefaultMaxSize >= 32 && ((mDefaultMaxSize - 1) & mDefaultMaxSize) == 0) {
				default_max_size = mDefaultMaxSize;
			}
			GUI.backgroundColor = cachedColor;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(2f);
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(6f);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(4f);
			EditorGUILayout.LabelField("Sprite Atlas :", mStyleBoldLabel, GUILayout.Width(90f));
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Find : ", GUILayout.Width(36f));
			EditorGUI.BeginChangeCheck();
			mSearchKey = GUILayout.TextField(mSearchKey, mStyleSearch, GUILayout.Width(190f));
			if (EditorGUI.EndChangeCheck()) {
				mSearchKeyLower = mSearchKey.ToLower();
			}
			if (GUILayout.Button("", mStyleSearchCancel)) { mSearchKey = ""; mSearchKeyLower = ""; }
			EditorGUILayout.EndHorizontal();
			bool isDirty = false;
			mAtlasesScroll = EditorGUILayout.BeginScrollView(mAtlasesScroll, false, false);
			EditorGUILayout.BeginHorizontal();//h1
			GUILayout.Space(4f);
			EditorGUILayout.BeginVertical();
			int removeAt = -1;
			int insertAt = -1;
			for (int i = 0; i < mSettings.Count; i++) {
				bool changed = false;
				AtlasSetting setting = mSettings[i];
				if (setting.atlasPath.StartsWith("Assets/") ?
					setting.atlasPath.IndexOf(mSearchKeyLower, 7, StringComparison.OrdinalIgnoreCase) < 0 :
					setting.atlasPath.IndexOf(mSearchKeyLower, StringComparison.OrdinalIgnoreCase) < 0 &&
					setting.texturesFolder.IndexOf(mSearchKeyLower, StringComparison.OrdinalIgnoreCase) < 0) {
					continue;
				}
				GUILayout.Space(3f);
				cachedColor = GUI.backgroundColor;
				GUI.backgroundColor = (i & 1) == 0 ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.7f, 0.7f, 0.7f);
				EditorGUILayout.BeginVertical(mStyleTextArea, GUILayout.MinHeight(20f));
				GUI.backgroundColor = cachedColor;
				EditorGUILayout.BeginHorizontal();//h2
				int hashSetting = setting.GetHashCode();
				AnimBool show;
				if (!mHideAtlases.TryGetValue(hashSetting, out show)) {
					show = mDefaultAtlasShow;
				}
				string atlasLabel = (show.value ? "\u25BC " : "\u25BA ") + setting.atlasPath;
				if (GUILayout.Button(atlasLabel, mStyleWhiteLabel)) {
					if (show == mDefaultAtlasShow) {
						show = new AnimBool(mDefaultAtlasShow.value, Repaint);
						mHideAtlases.Add(hashSetting, show);
					}
					show.target = !show.target;
				}
				bool packable = IsAtlasPathValid(setting.atlasPath) && !string.IsNullOrEmpty(setting.texturesFolder) &&
					setting.padding >= 0 && setting.maxSize >= 32 && ((setting.maxSize - 1) & setting.maxSize) == 0;
				cachedColor = GUI.backgroundColor;
				GUI.backgroundColor = packable ? Color.green : cachedColor;
				EditorGUI.BeginDisabledGroup(!packable);
				if (GUILayout.Button("Pack", GUILayout.Width(64f))) {
					PackSingleAtlas(setting);
				}
				EditorGUI.EndDisabledGroup();
				GUI.backgroundColor = Color.red;
				if (GUILayout.Button("X", GUILayout.Width(20f))) {
					removeAt = i;
				}
				GUI.backgroundColor = cachedColor;
				EditorGUILayout.EndHorizontal();//h2
				GUILayout.Space(4f);
				if (EditorGUILayout.BeginFadeGroup(show.faded)) {
					EditorGUILayout.BeginHorizontal();//h2
					GUILayout.Space(2f);
					EditorGUILayout.BeginHorizontal(mStyleTextArea);//h3
					GUILayout.Space(8f);
					EditorGUILayout.BeginVertical();
					GUILayout.Space(2f);
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.BeginHorizontal();//h4
					EditorGUILayout.LabelField("Textures Folder", GUILayout.Width(96f));
					cachedColor = GUI.backgroundColor;
					GUI.backgroundColor = string.IsNullOrEmpty(setting.texturesFolder) ? Color.red : cachedColor;
					setting.texturesFolder = EditorGUILayout.TextField(setting.texturesFolder);
					GUI.backgroundColor = cachedColor;
					if (GUILayout.Button("Browse", GUILayout.Width(60f))) {
						string t = EditorUtility.OpenFolderPanel("Textures Folder", setting.texturesFolder, "");
						if (!string.IsNullOrEmpty(t)) {
							prev_sprites_folder = Path.GetDirectoryName(t).Replace('\\', '/');
							string projPath = Application.dataPath;
							projPath = projPath.Substring(0, projPath.Length - 7);
							if (t.StartsWith(projPath)) {
								t = t.Substring(projPath.Length + 1);
							}
							setting.texturesFolder = t;
							changed = true;
						}
					}
					EditorGUILayout.EndHorizontal();//h4
					EditorGUILayout.BeginHorizontal();//h4
					EditorGUILayout.LabelField("SpriteAtals Path", GUILayout.Width(96f));
					cachedColor = GUI.backgroundColor;
					GUI.backgroundColor = IsAtlasPathValid(setting.atlasPath) ? cachedColor : Color.red;
					setting.atlasPath = EditorGUILayout.TextField(setting.atlasPath);
					GUI.backgroundColor = cachedColor;
					if (GUILayout.Button("Browse", GUILayout.Width(60f))) {
						string t = EditorUtility.SaveFilePanelInProject("Atlas Folder", setting.atlasPath, "png", "", prev_atlas_folder);
						if (!string.IsNullOrEmpty(t)) {
							prev_atlas_folder = Path.GetDirectoryName(t).Replace('\\', '/');
							setting.atlasPath = t;
							changed = true;
						}
					}
					EditorGUILayout.EndHorizontal();//h4
					EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(10f));//h4
					EditorGUILayout.BeginVertical();
					setting.unityPacking = GUILayout.Toggle(setting.unityPacking, " unity packing", GUILayout.Width(140f));
					EditorGUI.BeginDisabledGroup(setting.unityPacking);
					setting.forceSquare = GUILayout.Toggle(setting.forceSquare, " force square atlas", GUILayout.Width(140f));
					EditorGUI.EndDisabledGroup();
					setting.trimAlpha = GUILayout.Toggle(setting.trimAlpha, " trim alpha", GUILayout.Width(140f));
					EditorGUILayout.EndVertical();
					GUILayout.FlexibleSpace();
					GUILayout.FlexibleSpace();
					EditorGUILayout.BeginVertical();
					EditorGUILayout.BeginHorizontal();//h5
					EditorGUILayout.LabelField("Padding", GUILayout.Width(56f));
					cachedColor = GUI.backgroundColor;
					GUI.backgroundColor = setting.padding < 0 ? Color.red : cachedColor;
					setting.padding = EditorGUILayout.IntField(setting.padding, GUILayout.Width(64f));
					GUI.backgroundColor = cachedColor;
					EditorGUILayout.EndHorizontal();//h5
					EditorGUILayout.BeginHorizontal();//h5
					EditorGUILayout.LabelField("Max Size", GUILayout.Width(56f));
					cachedColor = GUI.backgroundColor;
					GUI.backgroundColor = setting.maxSize < 32 || ((setting.maxSize - 1) & setting.maxSize) != 0 ? Color.red : cachedColor;
					setting.maxSize = EditorGUILayout.IntField(setting.maxSize, GUILayout.Width(64f));
					GUI.backgroundColor = cachedColor;
					EditorGUILayout.EndHorizontal();//h5
					EditorGUILayout.EndVertical();
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();//h4
					GUILayout.Space(2f);
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();//h3
					if (EditorGUI.EndChangeCheck()) { changed = true; }
					GUILayout.Space(2f);
					EditorGUILayout.EndHorizontal();//h2
				}
				EditorGUILayout.EndFadeGroup();
				EditorGUILayout.EndVertical();
				if (changed) {
					isDirty = true;
				}
			}
			EditorGUILayout.EndVertical();
			GUILayout.Space(2f);
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(2f);
			cachedColor = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Add Atlas")) {
				insertAt = mSettings.Count;
			}
			GUI.backgroundColor = cachedColor;
			if (insertAt >= 0) {
				AtlasSetting setting = new AtlasSetting();
				setting.texturesFolder = "";
				setting.atlasPath = "";
				setting.padding = default_padding;
				setting.maxSize = default_max_size;
				setting.unityPacking = default_unity_packing;
				setting.forceSquare = default_force_square;
				setting.trimAlpha = default_trim_alpha;
				mSettings.Insert(insertAt, setting);
				isDirty = true;
			}
			if (removeAt >= 0) {
				mSettings.RemoveAt(removeAt);
				isDirty = true;
			}
			EditorGUILayout.EndScrollView();
			if (isDirty) {
				SaveAtlasConfig(mSettings);
			}
			Color cacheBgColor = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Pack All Atlas", GUILayout.Height(28f))) {
				for (int i = 0, imax = mSettings.Count; i < imax; i++) {
					AtlasSetting setting = mSettings[i];
					Pack(true, setting.texturesFolder, setting.atlasPath, setting.padding, setting.maxSize,
						setting.trimAlpha, setting.unityPacking, setting.forceSquare);
				}
			}
			GUI.backgroundColor = cacheBgColor;
		}

		[Serializable, XmlType("ConfigRoot")]
		public class ConfigRoot {
			[XmlElement("AtlasSetting")]
			public AtlasSetting[] atlasSettings = new AtlasSetting[] { };
		}

		[Serializable, XmlType("AtlasSetting")]
		public class AtlasSetting {
			[XmlAttribute("texturesFolder")]
			public string texturesFolder;
			[XmlAttribute("atlasPath")]
			public string atlasPath;
			[XmlAttribute("padding")]
			public int padding;
			[XmlAttribute("maxSize")]
			public int maxSize;
			[XmlAttribute("unityPacking")]
			public bool unityPacking;
			[XmlAttribute("forceSquare")]
			public bool forceSquare;
			[XmlAttribute("trimAlpha")]
			public bool trimAlpha;
		}

		#endregion
	}

}