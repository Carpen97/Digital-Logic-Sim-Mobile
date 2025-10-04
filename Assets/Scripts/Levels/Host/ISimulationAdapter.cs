namespace DLS.Levels.Host
{
	public interface ISimulationAdapter
	{
		bool IsDesignCombinational();
		void ApplyInputs(DLS.Levels.BitVector inputs);
		bool SettleWithin(int maxSteps, out int stepsTaken);
		DLS.Levels.BitVector ReadOutputs();
		ComponentCounts MeasureComponents();
	}

	public struct ComponentCounts
	{
		public int Parts;
		public int Wires;
	}
}
