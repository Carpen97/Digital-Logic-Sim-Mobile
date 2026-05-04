namespace DLS.Graphics
{
	/// <summary>
	/// Controls visibility for the experimental Verilog import UI.
	/// Ticket 104: keep this Editor-only so store/release builds cannot expose it.
	/// </summary>
	public static class VerilogImportFeatureGate
	{
#if UNITY_EDITOR
		public const bool IsEnabled = true;
#else
		public const bool IsEnabled = false;
#endif
	}
}
