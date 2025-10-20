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

	public BitVector ReadOutputs()
	{
		var root = _simChip;
		var dev  = _dev;
		if (root == null || dev == null) return new BitVector(0, 0);

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

	/// <summary>
	/// Reset the circuit state for sequential circuit testing.
	/// This ensures a clean state before running test sequences.
	/// For SR-latches, this performs a proper initialization sequence.
	/// </summary>
	public void ResetCircuitState()
	{
		var ins = InputPinsArray;
		if (ins.Length >= 2)
		{
			// Check if this is a D flip-flop (D, CLK inputs)
			if (ins.Length == 2)
			{
				// D flip-flop initialization sequence
				// Step 1: Set D=0, CLK=0 to establish known state
				ins[0].Pin.PlayerInputState.SetFirstBit(false);  // D = 0
				ins[1].Pin.PlayerInputState.SetFirstBit(false);  // CLK = 0
				SettleWithin(3, out _); // Let it settle
				
				// Step 2: Apply clock edge to ensure Q=0
				ins[0].Pin.PlayerInputState.SetFirstBit(false);  // D = 0
				ins[1].Pin.PlayerInputState.SetFirstBit(true);   // CLK = 1
				SettleWithin(3, out _); // Clock edge: Q should become 0
				
				// Step 3: Return to hold state
				ins[0].Pin.PlayerInputState.SetFirstBit(false);  // D = 0
				ins[1].Pin.PlayerInputState.SetFirstBit(false);  // CLK = 0
				SettleWithin(5, out _); // Hold state: Q should remain 0
			}
			else
			{
				// SR-latch initialization sequence (existing logic)
				// Step 1: Set both inputs to 1 (invalid state) to break any race conditions
				ins[0].Pin.PlayerInputState.SetFirstBit(true);  // S = 1
				ins[1].Pin.PlayerInputState.SetFirstBit(true);  // R = 1
				SettleWithin(3, out _); // Let it settle in invalid state
				
				// Step 2: Set both inputs to 0 (hold state) to establish known state
				ins[0].Pin.PlayerInputState.SetFirstBit(false); // S = 0
				ins[1].Pin.PlayerInputState.SetFirstBit(false); // R = 0
				SettleWithin(5, out _); // Let it settle in hold state
			}
		}
		else
		{
			// Fallback for circuits with fewer than 2 inputs
			foreach (var pin in ins)
			{
				pin.Pin.PlayerInputState.SetFirstBit(false);
			}
			SettleWithin(5, out _);
		}
	}

	/// <summary>
	/// Enhanced reset specifically for SR-latches.
	/// Performs a complete initialization sequence to ensure consistent starting state.
	/// </summary>
	public void ResetSRLatchState()
	{
		var ins = InputPinsArray;
		if (ins.Length < 2) return;
		
		// SR-latch initialization sequence:
		// 1. Set S=1, R=0 (Set state) - forces Q=1, Qn=0
		ins[0].Pin.PlayerInputState.SetFirstBit(true);   // S = 1
		ins[1].Pin.PlayerInputState.SetFirstBit(false);  // R = 0
		SettleWithin(3, out _);
		
		// 2. Set S=0, R=0 (Hold state) - maintains Q=1, Qn=0
		ins[0].Pin.PlayerInputState.SetFirstBit(false);  // S = 0
		ins[1].Pin.PlayerInputState.SetFirstBit(false);  // R = 0
		SettleWithin(5, out _);
		
		// Now the latch is in a known state: Q=1, Qn=0
	}
}
