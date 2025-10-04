namespace DLS.Levels.Host
{
	public interface ILevelsStorage
	{
		bool TryRead(string key, out string json);
		void Write(string key, string json);
	}
}
