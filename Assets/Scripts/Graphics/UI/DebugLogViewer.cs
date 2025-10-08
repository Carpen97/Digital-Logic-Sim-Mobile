using System.Collections.Generic;
using UnityEngine;
using System;

namespace DLS.Graphics
{
	public class DebugLogViewer : MonoBehaviour
	{
		private static DebugLogViewer instance;
		private List<LogEntry> logs = new List<LogEntry>();
		private bool showLogs = false;
		private Vector2 scrollPosition;
		private const int MAX_LOGS = 100;
		
		private struct LogEntry
		{
			public string message;
			public LogType type;
			public string timestamp;
		}

		void Awake()
		{
			if (instance == null)
			{
				instance = this;
				DontDestroyOnLoad(gameObject);
				Application.logMessageReceived += HandleLog;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		void HandleLog(string logString, string stackTrace, LogType type)
		{
			// Only capture logs related to Main and project creation/loading
			if (logString.Contains("[Main]") || logString.Contains("[Saver]") || 
			    logString.Contains("[Loader]") || type == LogType.Error || type == LogType.Exception)
			{
				LogEntry entry = new LogEntry
				{
					message = logString + (type == LogType.Exception || type == LogType.Error ? "\n" + stackTrace : ""),
					type = type,
					timestamp = DateTime.Now.ToString("HH:mm:ss")
				};
				
				logs.Add(entry);
				
				if (logs.Count > MAX_LOGS)
				{
					logs.RemoveAt(0);
				}
			}
		}

		void Update()
		{
			// Toggle logs with 3-finger triple tap
			if (Input.touchCount == 3 && Input.GetTouch(0).phase == TouchPhase.Began && 
			    Input.GetTouch(0).tapCount == 3)
			{
				showLogs = !showLogs;
			}
			
			// Or use keyboard for testing
			if (Input.GetKeyDown(KeyCode.F12))
			{
				showLogs = !showLogs;
			}
		}

		void OnGUI()
		{
			if (!showLogs) return;

			float width = Screen.width * 0.95f;
			float height = Screen.height * 0.8f;
			float x = Screen.width * 0.025f;
			float y = Screen.height * 0.1f;

			GUI.Box(new Rect(x, y, width, height), "Debug Logs (3-finger triple-tap to close)");

			float buttonHeight = 40;
			if (GUI.Button(new Rect(x + 10, y + height - buttonHeight - 10, 100, buttonHeight), "Clear"))
			{
				logs.Clear();
			}
			
			if (GUI.Button(new Rect(x + 120, y + height - buttonHeight - 10, 100, buttonHeight), "Close"))
			{
				showLogs = false;
			}

			GUILayout.BeginArea(new Rect(x + 10, y + 30, width - 20, height - buttonHeight - 50));
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			foreach (var log in logs)
			{
				Color originalColor = GUI.contentColor;
				
				switch (log.type)
				{
					case LogType.Error:
					case LogType.Exception:
						GUI.contentColor = Color.red;
						break;
					case LogType.Warning:
						GUI.contentColor = Color.yellow;
						break;
					default:
						GUI.contentColor = Color.white;
						break;
				}

				GUILayout.Label($"[{log.timestamp}] {log.message}");
				GUI.contentColor = originalColor;
			}

			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		void OnDestroy()
		{
			if (instance == this)
			{
				Application.logMessageReceived -= HandleLog;
			}
		}
	}
}

