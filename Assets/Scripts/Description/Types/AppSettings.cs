using UnityEngine;

namespace DLS.Description
{
	public struct AppSettings
	{
		public int ResolutionX;
		public int ResolutionY;
		public FullScreenMode fullscreenMode;
		public bool AutoResolution;
		public bool orientationIsLeftLandscape;
		public bool VSyncEnabled;
        public int showScrollingButtons;
        public int UIScaling;
        public bool EnableDiscordRichPresence;

        public static AppSettings Default() =>
			new()
			{
				ResolutionX = 1920,
				ResolutionY = 1080,
				fullscreenMode = FullScreenMode.Windowed,
				VSyncEnabled = true,
				EnableDiscordRichPresence = true,
				#if UNITY_ANDROID || UNITY_IOS
				orientationIsLeftLandscape = false,
				showScrollingButtons = 0,
				UIScaling = 1,
				AutoResolution = true
				#else
				// Desktop defaults - these fields are not used on desktop
				orientationIsLeftLandscape = false,
				showScrollingButtons = 0,
				UIScaling = 1,
				AutoResolution = false
				#endif
			};
	}
}