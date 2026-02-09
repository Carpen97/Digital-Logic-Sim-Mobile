namespace DLS.Levels.Host
{
	public interface ISimulationAdapter
	{
		void ApplyInputs(DLS.Levels.BitVector inputs);
		bool SettleWithin(int maxSteps, out int stepsTaken);
		DLS.Levels.BitVector ReadOutputs();
	}

	public struct ComponentCounts
	{
		public int Parts;
		public int Wires;
	}
}
