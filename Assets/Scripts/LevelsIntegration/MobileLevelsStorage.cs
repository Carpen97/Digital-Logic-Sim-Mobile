using System.IO;
using DLS.Levels.Host;
using UnityEngine;

public sealed class MobileLevelsStorage : ILevelsStorage
{
	private string RootPath => Path.Combine(Application.persistentDataPath, "levels");

	public bool TryRead(string key, out string json)
	{
		string path = Path.Combine(RootPath, key);
		if (File.Exists(path))
		{
			json = File.ReadAllText(path);
			return true;
		}
		json = null;
		return false;
	}

	public void Write(string key, string json)
	{
		Directory.CreateDirectory(RootPath);
		File.WriteAllText(Path.Combine(RootPath, key), json);
	}
}
