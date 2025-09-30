using System;
using System.Linq;               // ToArray()
using System.Reflection;         // reflection
using UnityEngine;
using DLS.Game;
using DLS.Simulation;
using DLS.Description;           // ChipType
using DLS.Levels.Host;
using DLS.Levels;

public sealed class MobileSimulationAdapter : ISimulationAdapter
{
	private Project _proj => Project.ActiveProject;
	private DevChipInstance _dev => _proj?.ViewedChip;
	private SimChip _simChip => _proj?.rootSimChip;

	// Always work with an array for stable indexing/stepping
	private DevPinInstance[] InputPinsArray =>
		_dev?.GetInputPins() as DevPinInstance[] ??
		_dev?.GetInputPins()?.ToArray() ??
		Array.Empty<DevPinInstance>();

	public void ApplyInputs(BitVector iv)
	{
		var ins = InputPinsArray;
		int n = Mathf.Min(ins.Length, iv.Length);
		for (int i = 0; i < n; i++)
		{
			bool on = ((iv.Raw >> i) & 1UL) != 0UL;
			ins[i].Pin.PlayerInputState.SetFirstBit(on);
		}
	}

	/// <summary>Advance the simulator synchronously by up to maxSteps ticks.</summary>
	public bool SettleWithin(int maxSteps, out int stepsTaken)
	{
		stepsTaken = 0;
		if (_simChip == null) return false;

		maxSteps = Mathf.Max(1, maxSteps);

		// Try to bind to Simulator.RunSimulationStep at runtime (supports both overloads).
		var simType = typeof(Simulator);
		var m3 = simType.GetMethod(
			"RunSimulationStep",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
			binder: null,
			types: new[] { typeof(SimChip), typeof(DevPinInstance[]), typeof(SimAudio) },
			modifiers: null
		);
		var m2 = m3 == null
			? simType.GetMethod(
				"RunSimulationStep",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
				binder: null,
				types: new[] { typeof(SimChip), typeof(SimAudio) },
				modifiers: null)
			: null;

		var audio = new SimAudio();
		var ins = InputPinsArray;

		for (; stepsTaken < maxSteps; stepsTaken++)
		{
			if (m3 != null)
			{
				m3.Invoke(null, new object[] { _simChip, ins, audio });
			}
			else if (m2 != null)
			{
				m2.Invoke(null, new object[] { _simChip, audio });
			}
			else
			{
				// Fallback: request a single step (non-blocking).
				_proj.advanceSingleSimStep = true;
			}
		}
		return true;
	}

	public BitVector ReadOutputs()
	{
		var root = _simChip;
		var dev  = _dev;
		if (root == null || dev == null) return new BitVector(0, 0);

		ulong raw = 0UL;
		int i = 0;
		foreach (var o in dev.GetOutputPins())
		{
			var sPin = root.GetSimPinFromAddress(o.Pin.Address);
			if (sPin.State.FirstBitHigh())
				raw |= (1UL << i);
			i++;
		}
		return new BitVector(raw, i);
	}

	public ComponentCounts MeasureComponents()
	{
		int parts = _dev?.Elements.Count ?? 0;
		int wires = _dev?.Wires.Count ?? 0;
		return new ComponentCounts { Parts = parts, Wires = wires };
	}

	/// <summary>
	/// Counts the total number of NAND gates used in the solution, including nested NAND gates within other chips.
	/// This provides a more meaningful score since NAND gates are the fundamental building blocks.
	/// </summary>
	public int CountNandGates()
	{
		if (_simChip == null) return 0;
		return CountNandGatesRecursive(_simChip);
	}

	/// <summary>
	/// Recursively counts NAND gates in a chip and all its sub-chips.
	/// </summary>
	private int CountNandGatesRecursive(SimChip chip)
	{
		if (chip == null) return 0;

		int count = 0;
		
		// Count this chip if it's a NAND gate
		if (chip.ChipType == ChipType.Nand)
		{
			count++;
		}
		
		// Recursively count NAND gates in all sub-chips
		foreach (var subChip in chip.SubChips)
		{
			count += CountNandGatesRecursive(subChip);
		}
		
		return count;
	}

	public bool IsDesignCombinational() => true;
}
