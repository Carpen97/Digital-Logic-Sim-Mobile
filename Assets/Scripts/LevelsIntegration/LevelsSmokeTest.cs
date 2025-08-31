using UnityEngine;
using DLS.Levels;
using DLS.Levels.Host;

public class LevelsSmokeTest : MonoBehaviour
{
	void Start()
	{
		// Load JSON from Resources
		TextAsset json = Resources.Load<TextAsset>("level_0001");
		if (json == null)
		{
			Debug.LogError("Sample level not found!");
			return;
		}

		var def = LevelRepository.ParseLevel(json.text);
		Debug.Log($"Loaded level: {def.name} with {def.testVectors.Count} test vectors");

		// Use our stub sim adapter for now
		ISimulationAdapter sim = new MobileSimulationAdapter();
		var validator = new LevelValidator(sim);

		var report = validator.Validate(def);

		Debug.Log($"Validation result: stars={report.Stars}, passedAll={report.PassedAll}");
		foreach (var fail in report.Failures)
			Debug.LogWarning($"Fail: {fail.Inputs} → {fail.Message}");
	}
}
