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
		int bitOffset = 0;
		
		for (int i = 0; i < ins.Length; i++)
		{
			var pin = ins[i].Pin;
			var pinBitCount = pin.bitCount.BitCount;
			
			if (pinBitCount == 1)
			{
				// Single bit pin - use the existing method
				if (bitOffset < iv.Length)
				{
					pin.PlayerInputState.SetFirstBit(iv[bitOffset]);
					bitOffset++;
				}
			}
			else
			{
				// Multi-bit pin - extract the bits for this pin and set them
				ulong pinValue = 0;
				for (int bitIndex = 0; bitIndex < pinBitCount && bitOffset < iv.Length; bitIndex++)
				{
					if (iv[bitOffset])
					{
						pinValue |= (1UL << bitIndex);
					}
					bitOffset++;
				}
				
				// Set the value based on bit count
				if (pinBitCount <= 16)
				{
					pin.PlayerInputState.SetShortValue((ushort)pinValue);
				}
				else if (pinBitCount <= 32)
				{
					pin.PlayerInputState.SetMediumValue((uint)pinValue);
				}
				else
				{
					// For >32 bits, we'd need to use BigValues, but that's complex
					// For now, just set the first 32 bits
					pin.PlayerInputState.SetMediumValue((uint)pinValue);
				}
			}
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

	/// <summary>Read current input pin values from the circuit (PlayerInputState).</summary>
	public BitVector ReadInputs()
	{
		var ins = InputPinsArray;
		if (ins == null || ins.Length == 0) return new BitVector(0, 0);

		ulong raw = 0UL;
		int bitOffset = 0;
		foreach (var inp in ins)
		{
			var pin = inp.Pin;
			var pinBitCount = pin.bitCount.BitCount;
			if (pinBitCount == 1)
			{
				if (pin.PlayerInputState.FirstBitHigh())
					raw |= (1UL << bitOffset);
				bitOffset++;
			}
			else
			{
				uint pinValue = pinBitCount <= 16 ? pin.PlayerInputState.GetShortValues() : pin.PlayerInputState.GetMediumValues();
				for (int bi = 0; bi < pinBitCount; bi++)
				{
					if ((pinValue & (1U << bi)) != 0)
						raw |= (1UL << bitOffset);
					bitOffset++;
				}
			}
		}
		return new BitVector(raw, bitOffset);
	}

	public BitVector ReadOutputs()
	{
		var root = _simChip;
		var dev  = _dev;
		if (root == null || dev == null)
        {
			Debug.Log("Failed to read Outputs");
			return new BitVector(0, 0);
        }

		ulong raw = 0UL;
		int bitOffset = 0;
		var outputPins = dev.GetOutputPins().ToArray();
		
		for (int i = 0; i < outputPins.Length; i++)
		{
			var o = outputPins[i];
			var sPin = root.GetSimPinFromAddress(o.Pin.Address);
			var pinBitCount = o.Pin.bitCount.BitCount;
			
			if (pinBitCount == 1)
			{
				// Single bit pin - read the first bit
				if (sPin.State.FirstBitHigh())
					raw |= (1UL << bitOffset);
				bitOffset++;
			}
			else
			{
				// Multi-bit pin - read all bits
				uint pinValue = 0;
				if (pinBitCount <= 16)
				{
					pinValue = sPin.State.GetShortValues();
				}
				else if (pinBitCount <= 32)
				{
					pinValue = sPin.State.GetMediumValues();
				}
				else
				{
					// For >32 bits, we'd need to use BigValues, but that's complex
					// For now, just read the first 32 bits
					pinValue = sPin.State.GetMediumValues();
				}
				
				// Extract individual bits and add them to the result
				for (int bitIndex = 0; bitIndex < pinBitCount; bitIndex++)
				{
					if ((pinValue & (1U << bitIndex)) != 0)
					{
						raw |= (1UL << bitOffset);
					}
					bitOffset++;
				}
			}
		}
		
		return new BitVector(raw, bitOffset);
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

	/// <summary>
	/// Returns true if the level solution contains any chip type that is disallowed in levels (ROM, Clock, Button, etc.)
	/// anywhere in the hierarchy, including inside custom chips. Used to block score upload for cheating solutions.
	/// </summary>
	public bool ContainsDisallowedSubchips()
	{
		if (_simChip == null) return false;
		return ContainsDisallowedSubchipsRecursive(_simChip);
	}

	private static bool ContainsDisallowedSubchipsRecursive(SimChip chip)
	{
		if (chip == null) return false;

		if (DLS.Description.ChipTypeHelper.IsDisabledInLevels(chip.ChipType))
			return true;

		foreach (var subChip in chip.SubChips)
		{
			if (ContainsDisallowedSubchipsRecursive(subChip))
				return true;
		}

		return false;
	}

}
