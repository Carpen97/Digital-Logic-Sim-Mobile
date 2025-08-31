using System.Linq;
using DLS.Levels;
using DLS.Levels.Host;
using DLS.Game;
using DLS.Simulation;
using UnityEngine;

public sealed class MobileSimulationAdapter : ISimulationAdapter
{
	private Project _proj => Project.ActiveProject;
	private DevChipInstance _dev => _proj?.ViewedChip;
	private SimChip _simChip => _proj?.rootSimChip;

	private DevPinInstance[] _inputs => _dev?.GetInputPins();
	private DevPinInstance[] _outputsCached;

	public bool IsDesignCombinational()
	{
		// Reuse SimChip’s predicate (same as caching logic)
		return _simChip != null && _simChip.IsCombinational();
	}

	public void ApplyInputs(BitVector inputs)
	{
		if (_dev == null) { UnityEngine.Debug.LogWarning("Levels: no ViewedChip"); return; }
		DevPinInstance[] ins = _dev.GetInputPins();
		DevPinInstance[] outs = _dev.GetOutputPinsAsArray();

		if (ins == null || ins.Length == 0) { UnityEngine.Debug.LogWarning("Levels: no INPUT pins in chip"); return; }
		if (outs == null || outs.Length == 0) { UnityEngine.Debug.LogWarning("Levels: no OUTPUT pins in chip"); }

		int n = Mathf.Min(inputs.Length, ins.Length);
		for (int i = 0; i < n; i++)
		{
			bool bit = ((inputs.Raw >> i) & 1UL) != 0;
			var s = ins[i].Pin.PlayerInputState;
			s.SetFirstBit(bit);
			ins[i].Pin.PlayerInputState = s;
		}
	}

	public bool SettleWithin(int maxSteps, out int stepsTaken)
	{
		stepsTaken = 0;
		if (_proj == null || _simChip == null || _inputs == null) return false;

		for (; stepsTaken < maxSteps; stepsTaken++)
		{
			Simulator.RunSimulationStep(_simChip, _inputs, _proj.audioState.simAudio);
			_dev.UpdateStateFromSim(_proj.ViewedSimChip, updateInputPins: false);

			// DEBUG: log outputs each step
			var outs = _dev.GetOutputPins();
			string bits = string.Join("", outs.Select(o => o.Pin.State.FirstBitHigh() ? "1" : "0"));
			UnityEngine.Debug.Log($"[Step {stepsTaken}] outputs={bits}");
		}

		return true;
	}



	public BitVector ReadOutputs()
	{
		if (_dev == null)
			return new BitVector(0, 0);

		_outputsCached ??= _dev.GetOutputPins().ToArray();

		// Pack first N outputs into bits (LSB = first output)
		ulong val = 0;
		int len = _outputsCached.Length;
		for (int i = 0; i < len; i++)
		{
			bool high = _outputsCached[i].Pin.State.FirstBitHigh();
			if (high) val |= (1UL << i);
		}
		return new BitVector(val, len);
	}

	public ComponentCounts MeasureComponents()
	{
		// MVP: rough counts; refine later if you have a stats API
		int parts = _dev?.Elements.Count ?? 0;
		int wires = _dev?.Wires.Count ?? 0;
		return new ComponentCounts { Parts = parts, Wires = wires };
	}
}
