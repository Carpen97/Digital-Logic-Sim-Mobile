using DLS.Graphics;
using DLS.Levels;
using DLS.Game.LevelsIntegration;
using UnityEngine.SceneManagement;
using DLS.Simulation;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Game
{
	public class UnityMain : MonoBehaviour
	{
		[Header("Dev Settings (editor only)")]
		public bool openSaveDirectory;
		public bool openInMainMenu;
		#if UNITY_EDITOR
		[Tooltip("When enabled in editor, skips menus and starts this level id after project load")]
		public bool startInLevel;
		[Tooltip("Level id from Resources/levels.json (e.g. lvl.halfadder.1)")]
		public string startLevelId;
		[Tooltip("If true and progress exists, keep it; otherwise clear progress for this level before start")]
		public bool continueLevelFromSave = true;
		#endif

		public string testProjectName;
		public bool openA = true;
		public string chipToOpenA;
		public string chipToOpenB;

		[Header("Temp test vars")] public Vector2 testVecA;
		public Vector2 testVecB;
		public Vector2 testVecC;
		public Vector2 testVecD;
		public Color testColA;
		public Color testColB;
		public Color testColC;
		public Color testColD;
		public ButtonTheme testButtonTheme;
		public bool testbool;
		public Anchor testAnchor;


		public string testString;
		public string testString2;
		public uint testUint;
		public uint testUint2;
		public bool removeZeros;
		public ushort testUshort;

		[Header("Audio test")]
		[Range(0,255)]public int noteIndex;
		public int numNoteDivisions;
		public double noteFreq;
		public double refNoteFreq;
		public bool useRef;
		public int refIndex;
		public float perceptualGain;
		
		public AudioState.WaveType waveType;

		public bool restart;
		public float speed = 1;
		public float staccatoDelay;
		public int waveIts = 20;
		public bool songTestMode;
		public NoteTest[] notes;
		
		// References
		public static UnityMain instance;
		AudioUnity audioUnity;

	void Awake()
	{
		instance = this;
		audioUnity = FindFirstObjectByType<AudioUnity>();
		ResetStatics();

		// Add debug log viewer for remote debugging
		#if UNITY_IOS || UNITY_ANDROID
		if (FindFirstObjectByType<DLS.Graphics.DebugLogViewer>() == null)
		{
			GameObject debugViewer = new GameObject("DebugLogViewer");
			debugViewer.AddComponent<DLS.Graphics.DebugLogViewer>();
		}
		#endif

		AudioState audioState = new();
		audioUnity.audioState = audioState;

		Main.Init(audioState);


		if (openInMainMenu || !Application.isEditor) Main.LoadMainMenu();
		else Main.CreateOrLoadProject(testProjectName, openA ? chipToOpenA : chipToOpenB);

		#if UNITY_EDITOR
		// In editor: optionally start directly in a specific level
		if (Application.isEditor && startInLevel)
		{
			var def = TryFindLevelById(startLevelId);
			if (def != null)
			{
				var mgr = GetOrCreateLevelManager();
				if (!continueLevelFromSave && !string.IsNullOrEmpty(def.id))
				{
					DLS.Levels.LevelProgressService.ClearLevelProgress(def.id);
				}
				mgr.StartLevel(def);
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
			}
			else
			{
				Debug.LogWarning($"[UnityMain] startInLevel enabled but level id not found: '{startLevelId}'");
			}
		}
		#endif

	}

		void Update()
		{
			noteFreq = SimAudio.CalculateFrequency(noteIndex / (double)numNoteDivisions);
			refNoteFreq = SimAudio.CalculateFrequency(refIndex / (double)numNoteDivisions);
			if (Application.isEditor) EditorDebugUpdate();

			if (songTestMode) SongTest();
			else Main.Update();
		}
		
		void OnGUI()
		{
			// Render queued textures using OnGUI
			UI.RenderOnGUITextures();
		}

		void SongTest()
		{
			/*
			audioUnity.audioState.waveIterations = waveIts;
			audioUnity.audioState.waveType = waveType;
			audioUnity.audioState.InitFrame();

			if (restart)
			{
				restart = false;
				time = -0.2f;
			}

			float playT = 0;
			time += Time.deltaTime * speed;
			int i = 0;
			foreach (NoteTest n in notes)
			{
				float startTime = playT + n.delay + staccatoDelay;
				float endTime = startTime + n.duration;
				if (time > startTime && time < endTime)
				{
					audioUnity.audioState.RegisterNote(n.noteIndex, n.isSharp, 15);
					Debug.Log(i + ": " + n.noteIndex + (n.isSharp ? " Sharp " : "Natural") + $"  Freq = {audioUnity.audioState.GetFrequency(n.noteIndex, n.isSharp):0.00}");
				}

				i++;
				playT = endTime;
			}

			audioUnity.audioState.NotifyAllNotesRegistered();
			*/
		}

		void EditorDebugUpdate()
		{
			if (InputHelper.AltIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.P))
			{
				if (InteractionState.PinUnderMouse != null)
				{
					SimPin simPin = Project.ActiveProject.rootSimChip.GetSimPinFromAddress(InteractionState.PinUnderMouse.Address);
					uint bitData = simPin.State.GetValue();
					uint tristateFlags = simPin.State.GetTristatedFlags() ;
					string bitString = StringHelper.CreateBinaryString(bitData, false);
					string triStateString = StringHelper.CreateBinaryString(tristateFlags, false);

					string displayString = "";
					for (int i = 0; i < bitString.Length; i++)
					{
						if (triStateString[i] == '1')
						{
							displayString += bitString[i] == '1' ? "?" : "x";
						}
						else
						{
							displayString += bitString[i];
						}
					}

					Debug.Log($"Pin state: {displayString}");
				}
			}
		}

		void OnDestroy()
		{
			if (Project.ActiveProject != null) Project.ActiveProject.NotifyExit();
		}

		void OnValidate()
		{
			if (openSaveDirectory)
			{
				openSaveDirectory = false;
				Main.OpenSaveDataFolderInFileBrowser();
			}
		}

		// Ensure static stuff gets properly reset (on account of domain-reloading being disabled in editor)
		static void ResetStatics()
		{
			Simulator.Reset();
			UIDrawer.Reset();
			InteractionState.Reset();
			CameraController.Reset();
			WorldDrawer.Reset();
		}

		#if UNITY_EDITOR
		static LevelDefinition TryFindLevelById(string levelId)
		{
			if (string.IsNullOrEmpty(levelId)) return null;
			var levelsText = Resources.Load<TextAsset>("levels");
			if (levelsText == null)
			{
				Debug.LogWarning("[UnityMain] Resources/levels.json not found (Resources key 'levels')");
				return null;
			}
			try
			{
				// Try parse as LocalLevelPack structure
				var pack = JsonUtility.FromJson<DLS.Levels.LocalLevelPack>(levelsText.text);
				if (pack?.chapters != null)
				{
					foreach (var ch in pack.chapters)
					{
						if (ch?.levels == null) continue;
						foreach (var def in ch.levels)
						{
							if (def != null && def.id == levelId) return def;
						}
					}
				}
			}
			catch { /* will try fallbacks below */ }

			try
			{
				// Fallback wrapper { "levels": [...] }
				var wrapper = JsonUtility.FromJson<DefsWrapper>(levelsText.text);
				if (wrapper?.levels != null)
				{
					foreach (var def in wrapper.levels)
						if (def != null && def.id == levelId) return def;
				}
			}
			catch { }

			try
			{
				// Fallback top-level array
				var arr = FromJsonArray<LevelDefinition>(levelsText.text);
				if (arr != null)
				{
					foreach (var def in arr)
						if (def != null && def.id == levelId) return def;
				}
			}
			catch { }

			return null;
		}

		// Mirrors LevelsMenu utility
		static LevelManager GetOrCreateLevelManager()
		{
			var runner = Object.FindFirstObjectByType<LevelManager>();
			if (runner != null) return runner;
			var go = new GameObject("LevelManager");
			Object.DontDestroyOnLoad(go);
			return go.AddComponent<LevelManager>();
		}

		// Local wrapper to support fallback parse
		[System.Serializable]
		class DefsWrapper { public LevelDefinition[] levels; }

		[System.Serializable]
		class ArrayWrapper<T> { public T[] Items; }
		static T[] FromJsonArray<T>(string json)
		{
			if (string.IsNullOrEmpty(json)) return null;
			try
			{
				// Wrap the array to make it parsable by JsonUtility
				string wrapped = "{\"Items\":" + json + "}";
				var wrapper = JsonUtility.FromJson<ArrayWrapper<T>>(wrapped);
				return wrapper?.Items;
			}
			catch { return null; }
		}
		#endif

		[System.Serializable]
		public struct NoteTest
		{
			public int noteIndex;
			public bool isSharp;
			public float delay;
			public float duration;
		}
	}
}