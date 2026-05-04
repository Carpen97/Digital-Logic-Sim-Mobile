using System;
using System.Collections.Generic;
using DLS.Description;
using DLS.Game;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.SaveSystem
{
	/// <summary>
	/// Creates DLS group/chip descriptions from structural Verilog circuits.
	/// Layout: inputs on left, subchips in centre, outputs on right.
	/// </summary>
	public static class VerilogImporter
	{
		// Layout constants (matches LevelManager-style placement)
		const float LeftX = -3f;
		const float CentreX = 0f;
		const float RightX = 3f;
		const float RowSpacing = 0.8f;
		const float PinSideOffset = 2.0f;

		// ---- Verilog file import (dynamic, no hard-coded circuits) ----

		/// <summary>Returns gate indices in topological order so producers are before consumers. Inputs are considered pre-driven.</summary>
		static List<int> TopoSortGates(List<VerilogGate> gates, List<string> inputs)
		{
			// Input base names and their bit nets (e.g. divisor_b0) are pre-driven by Split chips
			var inputSet = new HashSet<string>(inputs, StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < inputs.Count; i++)
			{
				string b = inputs[i];
				inputSet.Add(b);
				for (int bi = 0; bi < 32; bi++) inputSet.Add($"{b}_b{bi}");
			}
			// producerOf[net] = gate index that produces this net (output port)
			var producerOf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var inDegree = new int[gates.Count];

			for (int i = 0; i < gates.Count; i++)
			{
				var g = gates[i];
				string outputNet = (g.Ports != null && g.Ports.Count > 0) ? g.Ports[0] : null;
				if (!string.IsNullOrEmpty(outputNet))
					producerOf[outputNet] = i;
			}

			// inDegree[i] = number of producer gates that must be processed before i
			for (int i = 0; i < gates.Count; i++)
			{
				var g = gates[i];
				var deps = new HashSet<int>();
				for (int p = 1; p < (g.Ports?.Count ?? 0); p++)
				{
					string inp = g.Ports[p];
					if (string.IsNullOrEmpty(inp) || inputSet.Contains(inp)) continue;
					if (producerOf.TryGetValue(inp, out int prod) && prod != i)
						deps.Add(prod);
				}
				inDegree[i] = deps.Count;
			}

			// blocks[j] = gates i that depend on j (j must be processed before i)
			var blocks = new Dictionary<int, List<int>>();
			for (int i = 0; i < gates.Count; i++)
			{
				var g = gates[i];
				for (int p = 1; p < (g.Ports?.Count ?? 0); p++)
				{
					string inp = g.Ports[p];
					if (string.IsNullOrEmpty(inp) || inputSet.Contains(inp)) continue;
					if (producerOf.TryGetValue(inp, out int j) && j != i)
					{
						if (!blocks.TryGetValue(j, out var list)) { list = new List<int>(); blocks[j] = list; }
						list.Add(i);
					}
				}
			}

			var order = new List<int>();
			var q = new Queue<int>();
			for (int i = 0; i < gates.Count; i++)
				if (inDegree[i] == 0) q.Enqueue(i);
			while (q.Count > 0)
			{
				int j = q.Dequeue();
				order.Add(j);
				if (blocks.TryGetValue(j, out var blocked))
					foreach (int i in blocked)
					{
						inDegree[i]--;
						if (inDegree[i] == 0) q.Enqueue(i);
					}
			}
			if (order.Count != gates.Count)
			{
				// Cycle - return original order (better than crashing)
				order.Clear();
				for (int i = 0; i < gates.Count; i++) order.Add(i);
			}
			return order;
		}

		static WireDescription CreateWire(int srcOwner, int srcPin, int tgtOwner, int tgtPin)
		{
			return new WireDescription
			{
				SourcePinAddress = new PinAddress(srcOwner, srcPin),
				TargetPinAddress = new PinAddress(tgtOwner, tgtPin),
				ConnectionType = WireConnectionType.ToPins,
				ConnectedWireIndex = -1,
				ConnectedWireSegmentIndex = -1,
				Points = new Vector2[] { Vector2.zero, Vector2.zero }
			};
		}

		const int VerilogInputIdBase = 100;
		const int VerilogOutputIdBase = 200;
		const int VerilogGateIdBase = 1000;
		const int VerilogTitleLabelId = 500;
		const int VerilogSplitIdBase = 600;
		const int VerilogMergeIdBase = 700;

		static PinBitCount WidthToPinBitCount(int w)
		{
			if (w <= 1) return PinBitCount.Bit1;
			if (w <= 4) return PinBitCount.Bit4;
			if (w <= 8) return PinBitCount.Bit8;
			if (w <= 16) return new PinBitCount((ushort)16);
			return new PinBitCount((ushort)Math.Min(w, 32));
		}

		/// <summary>
		/// Parses structural Verilog and builds a GroupDescription. Returns null on error (check lastError).
		/// Pass chipLibrary to use native 3-input gates (XOR3, OR3) when available; otherwise expands to 2-input gates.
		/// </summary>
		public static GroupDescription ImportFromVerilog(string content, out string lastError) =>
			ImportFromVerilog(content, null, out lastError);

		public static GroupDescription ImportFromVerilog(string content, ChipLibrary chipLibrary, out string lastError)
		{
			lastError = null;
			VerilogParseResult parse = VerilogParser.Parse(content, chipLibrary);
			if (!string.IsNullOrEmpty(parse.ParseError))
			{
				lastError = parse.ParseError;
				return null;
			}
			if (parse.Gates.Count == 0 && parse.Inputs.Count > 0 && parse.Outputs.Count > 0)
			{
				lastError = "No logic found. For-loops are not supported. Try: 2-to-4 Decoder, 4-to-1 MUX, Full Adder, D Latch, 4-bit Counter, or ALU.";
				return null;
			}
			return BuildGroupFromParseResult(parse, chipLibrary, out lastError);
		}

		static GroupDescription BuildGroupFromParseResult(VerilogParseResult parse, ChipLibrary chipLibrary, out string error)
		{
			error = null;
			var driverOf = new Dictionary<string, (int owner, int pin)>(StringComparer.OrdinalIgnoreCase);
			var sinksOf = new Dictionary<string, List<(int owner, int pin)>>(StringComparer.OrdinalIgnoreCase);
			var aliasOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // buf out -> in

			var subChips = new List<SubChipDescription>();
			var wires = new List<WireDescription>();
			int gateId = VerilogGateIdBase;

			// Multi-bit inputs: add Split chips, wire input pin -> Split, driverOf[a_b0..] = Split outputs
			for (int i = 0; i < parse.Inputs.Count; i++)
			{
				string name = parse.Inputs[i];
				int width = parse.GetWidth(name);
				int inputPinId = VerilogInputIdBase + i;
				if (width <= 1)
				{
					driverOf[name] = (inputPinId, 0);
					continue;
				}
				// Need Split: chip "width-1BIT" e.g. "8-1BIT"
				string splitName = $"{width}-1BIT";
				if (chipLibrary != null && chipLibrary.TryGetChipDescription(splitName, out _))
				{
					int splitId = VerilogSplitIdBase + i;
					subChips.Add(new SubChipDescription(splitName, splitId, "", Vector2.zero, null, null, null, 0));
					wires.Add(CreateWire(inputPinId, 0, splitId, 0)); // input pin output -> Split input
					// Split: pin 1 = MSB, pin width = LSB. Verilog bit 0 = LSB, bit width-1 = MSB.
					for (int bi = 0; bi < width; bi++)
						driverOf[parse.BitNetName(name, bi)] = (splitId, width - bi); // bi=0 (LSB)->pin width, bi=width-1 (MSB)->pin 1
				}
				else
				{
					error = $"Multi-bit input '{name}' requires Split chip '{splitName}' in chip library. Add 8-bit pins to your project.";
					return null;
				}
			}

			// Multi-bit outputs: add Merge chips, sinksOf[op_b0..].Add(Merge inputs), wire Merge output -> output pin
			for (int i = 0; i < parse.Outputs.Count; i++)
			{
				string name = parse.Outputs[i];
				int width = parse.GetWidth(name);
				int outputPinId = VerilogOutputIdBase + i;
				if (width <= 1)
				{
					AddSink(sinksOf, name, outputPinId, 0);
					continue;
				}
				string mergeName = $"1-{width}BIT";
				if (chipLibrary != null && chipLibrary.TryGetChipDescription(mergeName, out var mergeDesc))
				{
					int mergeId = VerilogMergeIdBase + i;
					subChips.Add(new SubChipDescription(mergeName, mergeId, "", Vector2.zero, null, null, null, 0));
					// Merge: pin 0 = MSB, pin width-1 = LSB. Verilog bit 0 = LSB, bit width-1 = MSB.
					for (int bi = 0; bi < width; bi++)
						AddSink(sinksOf, parse.BitNetName(name, bi), mergeId, width - 1 - bi); // bi=0 (LSB)->pin width-1, bi=width-1 (MSB)->pin 0
					int mergeOutPin = width; // Merge output pin ID
					wires.Add(CreateWire(mergeId, mergeOutPin, outputPinId, 0));
				}
				else
				{
					error = $"Multi-bit output '{name}' requires Merge chip '{mergeName}' in chip library. Add 8-bit pins to your project.";
					return null;
				}
			}

			// Topological sort: process gates so that whenever gate B consumes a net produced by gate A, A is processed before B.
			// This keeps producer gates before consumers for cleaner layout and fewer temporary unresolved nets.
			var gateOrder = TopoSortGates(parse.Gates, parse.Inputs);
			foreach (int idx in gateOrder)
			{
				var g = parse.Gates[idx];
				// Per-gate logging is intentionally disabled to avoid huge console spam on large imports.
				if (g.Type == "dff")
				{
					if (g.Ports.Count != 3) { error = "dff gate must have 3 ports (Q, D, CLK)"; return null; }
					string qNet = g.Ports[0];
					string dNet = g.Ports[1];
					string clkNet = g.Ports[2];
					if (chipLibrary == null || !chipLibrary.TryGetChipDescription("DFF", out var dffDesc))
					{
						error = "DFF chip missing. Import DFF-master-slave.v, save as \"DFF\", then import this circuit.";
						return null;
					}
					int dPin = 0, clkPin = 1, qPin = 2;
					if (dffDesc.InputPins != null && dffDesc.OutputPins != null && dffDesc.OutputPins.Length > 0)
					{
						// Resolve D, CLK, Q by name if possible
						foreach (var p in dffDesc.InputPins)
							if (string.Equals(p.Name, "D", StringComparison.OrdinalIgnoreCase)) dPin = p.ID;
							else if (string.Equals(p.Name, "CLK", StringComparison.OrdinalIgnoreCase)) clkPin = p.ID;
						qPin = dffDesc.OutputPins[0].ID;
					}
					driverOf[qNet] = (gateId, qPin);
					AddSink(sinksOf, dNet, gateId, dPin);
					AddSink(sinksOf, clkNet, gateId, clkPin);
					subChips.Add(new SubChipDescription("DFF", gateId, "", Vector2.zero, null, null, null, 0));
					gateId++;
					continue;
				}
				if (g.Type == "not")
				{
					if (g.Ports.Count != 2) { error = "not gate must have 2 ports (output, input)"; return null; }
					driverOf[g.Ports[0]] = (gateId, 2);
					AddSink(sinksOf, g.Ports[1], gateId, 0);
					AddSink(sinksOf, g.Ports[1], gateId, 1);
					subChips.Add(new SubChipDescription("NAND", gateId, "", Vector2.zero, null, null, null, 0));
					gateId++;
					continue;
				}
				if (g.Type == "buf")
				{
					if (g.Ports.Count != 2) { error = "buf gate must have 2 ports (output, input)"; return null; }
					// Store net alias and resolve after all gates are known.
					aliasOf[g.Ports[0]] = g.Ports[1];
					continue;
				}
				if (g.Ports.Count < 3) { error = $"Gate {g.InstanceId} needs at least output and 2 inputs"; return null; }

				string outNet = g.Ports[0];
				int numInputs = g.Ports.Count - 1;

				if (g.Type == "nor")
				{
					if (!SynthesizeNor(outNet, g.Ports, numInputs, driverOf, sinksOf, subChips, ref gateId, out error)) return null;
					continue;
				}
				if (g.Type == "xnor")
				{
					if (!SynthesizeXnor(outNet, g.Ports, numInputs, driverOf, sinksOf, subChips, ref gateId, out error)) return null;
					continue;
				}

				string dlsType = g.Type switch { "and" => "AND", "or" => "OR", "nand" => "NAND", "xor" => "XOR", _ => null };
				if (dlsType == null) { error = $"Unknown gate type: {g.Type}"; return null; }

				if (numInputs == 2)
				{
					driverOf[outNet] = (gateId, 2);
					AddSink(sinksOf, g.Ports[1], gateId, 1);
					AddSink(sinksOf, g.Ports[2], gateId, 0);
					subChips.Add(new SubChipDescription(dlsType, gateId, "", Vector2.zero, null, null, null, 0));
					gateId++;
					continue;
				}

				if (!SynthesizeMultiInputGate(dlsType, outNet, g.Ports, numInputs, driverOf, sinksOf, subChips, ref gateId, chipLibrary, out error)) return null;
			}

			// Resolve transitive aliases (buf chains), then remap sink nets to their source net.
			string ResolveAliasedNet(string net, out string resolveError)
			{
				resolveError = null;
				var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				string cur = net;
				while (aliasOf.TryGetValue(cur, out var next))
				{
					if (!visited.Add(cur))
					{
						resolveError = $"Cyclic buf alias detected at net '{cur}'";
						return null;
					}
					cur = next;
				}
				// Path compression
				foreach (var v in visited)
					aliasOf[v] = cur;
				return cur;
			}

			var remappedSinks = new Dictionary<string, List<(int owner, int pin)>>(StringComparer.OrdinalIgnoreCase);
			foreach (var kv in sinksOf)
			{
				string resolvedNet = ResolveAliasedNet(kv.Key, out string resolveError);
				if (resolvedNet == null)
				{
					error = resolveError ?? "Failed to resolve buf aliases";
					return null;
				}
				if (!remappedSinks.TryGetValue(resolvedNet, out var sinkList))
				{
					sinkList = new List<(int owner, int pin)>();
					remappedSinks[resolvedNet] = sinkList;
				}
				sinkList.AddRange(kv.Value);
			}
			sinksOf = remappedSinks;

			// Prune dead gate logic: keep only gates that can reach non-gate sinks (outputs/merge/input wiring).
			// This removes dangling synthesized intermediates that don't contribute to observable behavior.
			bool IsGateOwner(int ownerId) => ownerId >= VerilogGateIdBase;
			var consumedNetsByGate = new Dictionary<int, List<string>>();
			foreach (var kv in sinksOf)
			{
				string net = kv.Key;
				foreach (var (sinkOwner, _) in kv.Value)
				{
					if (!IsGateOwner(sinkOwner)) continue;
					if (!consumedNetsByGate.TryGetValue(sinkOwner, out var nets))
					{
						nets = new List<string>();
						consumedNetsByGate[sinkOwner] = nets;
					}
					nets.Add(net);
				}
			}

			var requiredGates = new HashSet<int>();
			var gateQueue = new Queue<int>();

			// Seed with gates that directly drive non-gate sinks (observable outputs / merge inputs).
			foreach (var kv in sinksOf)
			{
				string net = kv.Key;
				if (!driverOf.TryGetValue(net, out var drv) || !IsGateOwner(drv.owner)) continue;
				bool hasExternalSink = false;
				foreach (var (sinkOwner, _) in kv.Value)
				{
					if (!IsGateOwner(sinkOwner)) { hasExternalSink = true; break; }
				}
				if (hasExternalSink && requiredGates.Add(drv.owner))
					gateQueue.Enqueue(drv.owner);
			}

			// Walk backwards through gate dependencies.
			while (gateQueue.Count > 0)
			{
				int g = gateQueue.Dequeue();
				if (!consumedNetsByGate.TryGetValue(g, out var nets)) continue;
				foreach (string inNet in nets)
				{
					if (!driverOf.TryGetValue(inNet, out var pred) || !IsGateOwner(pred.owner)) continue;
					if (requiredGates.Add(pred.owner))
						gateQueue.Enqueue(pred.owner);
				}
			}

			// Drop dead gates from subchips, drivers, and sink lists.
			subChips.RemoveAll(sc => IsGateOwner(sc.ID) && !requiredGates.Contains(sc.ID));

			var removeDriverNets = new List<string>();
			foreach (var kv in driverOf)
			{
				if (IsGateOwner(kv.Value.owner) && !requiredGates.Contains(kv.Value.owner))
					removeDriverNets.Add(kv.Key);
			}
			foreach (string net in removeDriverNets) driverOf.Remove(net);

			var sinkKeys = new List<string>(sinksOf.Keys);
			foreach (string net in sinkKeys)
			{
				var filtered = new List<(int owner, int pin)>();
				foreach (var sink in sinksOf[net])
				{
					if (!IsGateOwner(sink.owner) || requiredGates.Contains(sink.owner))
						filtered.Add(sink);
				}
				if (filtered.Count == 0) sinksOf.Remove(net);
				else sinksOf[net] = filtered;
			}

			// Build wires: from each driver to its sinks
			foreach (var kv in driverOf)
			{
				string net = kv.Key;
				(int srcO, int srcP) = kv.Value;
				if (sinksOf.TryGetValue(net, out var sinks))
				{
					foreach (var (tgtO, tgtP) in sinks)
						wires.Add(CreateWire(srcO, srcP, tgtO, tgtP));
				}
			}

			// Validate that every consumed net has a producer after alias remap.
			foreach (var net in sinksOf.Keys)
			{
				if (!driverOf.ContainsKey(net))
				{
					error = $"Signal '{net}' not driven";
					return null;
				}
			}

			// Remove duplicate wires
			var seen = new HashSet<(int, int, int, int)>();
			var uniqueWires = new List<WireDescription>();
			foreach (var w in wires)
			{
				var k = (w.SourcePinAddress.PinOwnerID, w.SourcePinAddress.PinID, w.TargetPinAddress.PinOwnerID, w.TargetPinAddress.PinID);
				if (seen.Add(k))
					uniqueWires.Add(w);
			}

			// Create pins and apply layout
			// Keep imported circuit internals near center and place I/O pins clearly at the sides.
			float lx = LeftX - PinSideOffset, rx = RightX + PinSideOffset;
			float rowSpace = RowSpacing;

			var inputPins = new List<PinDescription>();
			var inputY = new Dictionary<int, float>();
			for (int i = 0; i < parse.Inputs.Count; i++)
			{
				float y = (parse.Inputs.Count == 1 ? 0 : (i - (parse.Inputs.Count - 1) / 2f) * rowSpace);
				int id = VerilogInputIdBase + i;
				inputY[id] = y;
				PinBitCount bitCount = WidthToPinBitCount(parse.GetWidth(parse.Inputs[i]));
				inputPins.Add(new PinDescription(parse.Inputs[i], id, new Vector2(lx, y), bitCount, PinColour.Red, PinValueDisplayMode.Off));
			}

			var outputPins = new List<PinDescription>();
			var outputY = new Dictionary<int, float>();
			for (int i = 0; i < parse.Outputs.Count; i++)
			{
				float y = (parse.Outputs.Count == 1 ? 0 : (i - (parse.Outputs.Count - 1) / 2f) * rowSpace);
				int id = VerilogOutputIdBase + i;
				outputY[id] = y;
				PinBitCount bitCount = WidthToPinBitCount(parse.GetWidth(parse.Outputs[i]));
				outputPins.Add(new PinDescription(parse.Outputs[i], id, new Vector2(rx, y), bitCount, PinColour.Red, PinValueDisplayMode.Off));
			}

			// Layered layout: assign gates to levels by signal flow, then position to reduce wire crossings
			var gatePositions = ComputeLayeredLayout(driverOf, sinksOf, subChips, inputY, outputY, lx, rx, rowSpace);

			// Add title label at top center (shows module/circuit name). Append at end so it draws on top of gates.
			int numGates = subChips.Count;
			bool addTitleLabel = numGates >= 1;
			string labelChipName = ChipTypeHelper.GetName(ChipType.Label);
			float maxGateY = 0f;
			if (gatePositions.Count > 0)
			{
				foreach (var v in gatePositions.Values) if (Math.Abs(v.y) > maxGateY) maxGateY = Math.Abs(v.y);
			}
			if (addTitleLabel)
			{
				float labelY = maxGateY + rowSpace * 1.5f;
				uint[] labelData = new uint[] { 0, 120, 280 };
				var titleLabel = new SubChipDescription(labelChipName, VerilogTitleLabelId, parse.ModuleName, new Vector2(CentreX, labelY), null, labelData, null, 0);
				subChips.Add(titleLabel);
			}

			// Apply layout to all subchips
			for (int i = 0; i < subChips.Count; i++)
			{
				var sc = subChips[i];
				Vector2 pos;
				if (sc.ID == VerilogTitleLabelId)
					pos = new Vector2(CentreX, maxGateY + rowSpace * 1.5f);
				else if (gatePositions.TryGetValue(sc.ID, out var p))
					pos = p;
				else if (sc.ID >= VerilogSplitIdBase && sc.ID < VerilogSplitIdBase + 64)
					pos = new Vector2(lx + 0.3f, (sc.ID - VerilogSplitIdBase - parse.Inputs.Count / 2f) * rowSpace * 0.8f);
				else if (sc.ID >= VerilogMergeIdBase && sc.ID < VerilogMergeIdBase + 64)
					pos = new Vector2(rx - 0.3f, (sc.ID - VerilogMergeIdBase - parse.Outputs.Count / 2f) * rowSpace * 0.8f);
				else
					pos = new Vector2(CentreX, 0f);
				subChips[i] = new SubChipDescription(sc.Name, sc.ID, sc.Label, pos, sc.OutputPinColourInfo, sc.InternalData, sc.LabelOffset, sc.Rotation);
			}

			return new GroupDescription
			{
				Name = parse.ModuleName,
				DLSVersion = Main.DLSVersion.ToString(),
				InputPins = inputPins.ToArray(),
				OutputPins = outputPins.ToArray(),
				SubChips = subChips.ToArray(),
				Wires = uniqueWires.ToArray()
			};
		}

		static void AddSink(Dictionary<string, List<(int owner, int pin)>> sinksOf, string net, int owner, int pin)
		{
			if (!sinksOf.TryGetValue(net, out var list)) { list = new List<(int, int)>(); sinksOf[net] = list; }
			list.Add((owner, pin));
		}

		/// <summary>Layered layout: gates in columns by signal-flow level, y-ordered to reduce wire crossings.</summary>
		static Dictionary<int, Vector2> ComputeLayeredLayout(
			Dictionary<string, (int owner, int pin)> driverOf,
			Dictionary<string, List<(int owner, int pin)>> sinksOf,
			List<SubChipDescription> subChips,
			Dictionary<int, float> inputY, Dictionary<int, float> outputY,
			float leftX, float rightX, float rowSpace)
		{
			var result = new Dictionary<int, Vector2>();
			var gateIds = new List<int>();
			foreach (var sc in subChips)
				if (sc.ID >= VerilogGateIdBase && sc.ID != VerilogTitleLabelId)
					gateIds.Add(sc.ID);
			if (gateIds.Count == 0) return result;

			// Get predecessor owners for each gate (who drives this gate's inputs?)
			HashSet<int> GetPredecessors(int ownerId)
			{
				var pred = new HashSet<int>();
				foreach (var kv in sinksOf)
				{
					foreach (var (o, _) in kv.Value)
					{
						if (o != ownerId) continue;
						if (driverOf.TryGetValue(kv.Key, out var dr))
							pred.Add(dr.owner);
					}
				}
				return pred;
			}

			// Level = 1 + max(level of predecessors). Inputs = level 0.
			// Circuits with feedback (latches, flip-flops) have cycles; cap iterations to avoid infinite loop.
			const int MaxLevelIterations = 256;
			var levelOf = new Dictionary<int, int>();
			foreach (var id in inputY.Keys) levelOf[id] = 0;
			int iter = 0;
			bool changed;
			do
			{
				changed = false;
				foreach (int gid in gateIds)
				{
					var pred = GetPredecessors(gid);
					int maxPredLevel = -1;
					foreach (int p in pred)
					{
						if (levelOf.TryGetValue(p, out int lp) && lp > maxPredLevel) maxPredLevel = lp;
					}
					int newLevel = maxPredLevel + 1;
					if (!levelOf.TryGetValue(gid, out int old) || old != newLevel) { levelOf[gid] = newLevel; changed = true; }
				}
				if (++iter >= MaxLevelIterations)
				{
					// Cyclic circuit (latch/flip-flop): spread across multiple columns instead of one vertical stack
					int numCols = Math.Min(6, Math.Max(3, (int)Math.Ceiling(Math.Sqrt(gateIds.Count * 0.6))));
					float colWidth = Math.Max((rightX - leftX) / (numCols + 1), 1.2f);
					for (int i = 0; i < gateIds.Count; i++)
					{
						int col = i % numCols;
						int row = i / numCols;
						int rowsInCol = (gateIds.Count + numCols - 1 - col) / numCols;
						float x = leftX + colWidth * (col + 1);
						float y = rowsInCol <= 1 ? 0 : (row - (rowsInCol - 1) / 2f) * rowSpace * 1.2f;
						result[gateIds[i]] = new Vector2(x, y);
					}
					return result;
				}
			} while (changed);

			int maxLevel = 0;
			foreach (var l in levelOf.Values) if (l > maxLevel) maxLevel = l;
			int numLevels = Math.Max(1, maxLevel);

			// Group gates by level
			var byLevel = new Dictionary<int, List<int>>();
			foreach (int gid in gateIds)
			{
				int lvl = levelOf.GetValueOrDefault(gid, 0);
				if (!byLevel.TryGetValue(lvl, out var list)) { list = new List<int>(); byLevel[lvl] = list; }
				list.Add(gid);
			}

			// Position: x by level (left-to-right flow), y by barycenter of predecessors to reduce crossings
			float GetY(int ownerId)
			{
				if (inputY.TryGetValue(ownerId, out float iy)) return iy;
				if (outputY.TryGetValue(ownerId, out float oy)) return oy;
				if (result.TryGetValue(ownerId, out var r)) return r.y;
				return 0f;
			}

			for (int lvl = 0; lvl <= maxLevel; lvl++)
			{
				if (!byLevel.TryGetValue(lvl, out var gates) || gates.Count == 0) continue;
				// Sort by average y of predecessors (barycenter heuristic)
				gates.Sort((a, b) =>
				{
					var pa = GetPredecessors(a);
					var pb = GetPredecessors(b);
					float ya = 0f; int na = 0;
					foreach (int p in pa) { ya += GetY(p); na++; }
					float yb = 0f; int nb = 0;
					foreach (int p in pb) { yb += GetY(p); nb++; }
					ya = na > 0 ? ya / na : 0;
					yb = nb > 0 ? yb / nb : 0;
					return ya.CompareTo(yb);
				});
				float levelStep = Math.Max((rightX - leftX) / (numLevels + 2f), 1.1f);
				float x = leftX + levelStep * (lvl + 1f);
				float spacing = rowSpace * (gates.Count <= 1 ? 0 : 1.2f);
				for (int i = 0; i < gates.Count; i++)
				{
					float y = gates.Count == 1 ? 0 : (i - (gates.Count - 1) / 2f) * spacing;
					result[gates[i]] = new Vector2(x, y);
				}
			}
			return result;
		}

		/// <summary>NOR = NOT(OR). Supports 2+ inputs via OR tree + NAND-as-NOT.</summary>
		static bool SynthesizeNor(string outNet, List<string> ports, int numInputs, Dictionary<string, (int, int)> driverOf, Dictionary<string, List<(int, int)>> sinksOf, List<SubChipDescription> subChips, ref int gateId, out string error)
		{
			error = null;
			if (numInputs < 2) { error = "nor needs at least 2 inputs"; return false; }
			string prevNet = ports[1];
			for (int i = 2; i < ports.Count; i++)
			{
				string nextNet = $"_nort_{gateId}_{i}";
				driverOf[nextNet] = (gateId, 2);
				AddSink(sinksOf, prevNet, gateId, 1);
				AddSink(sinksOf, ports[i], gateId, 0);
				subChips.Add(new SubChipDescription("OR", gateId, "", Vector2.zero, null, null, null, 0));
				gateId++;
				prevNet = nextNet;
			}
			driverOf[outNet] = (gateId, 2);
			AddSink(sinksOf, prevNet, gateId, 0);
			AddSink(sinksOf, prevNet, gateId, 1);
			subChips.Add(new SubChipDescription("NAND", gateId, "", Vector2.zero, null, null, null, 0));
			gateId++;
			return true;
		}

		/// <summary>XNOR = NOT(XOR). Supports 2+ inputs via XOR tree + NAND-as-NOT.</summary>
		static bool SynthesizeXnor(string outNet, List<string> ports, int numInputs, Dictionary<string, (int, int)> driverOf, Dictionary<string, List<(int, int)>> sinksOf, List<SubChipDescription> subChips, ref int gateId, out string error)
		{
			error = null;
			if (numInputs < 2) { error = "xnor needs at least 2 inputs"; return false; }
			string prevNet = ports[1];
			for (int i = 2; i < ports.Count; i++)
			{
				string nextNet = $"_xnort_{gateId}_{i}";
				driverOf[nextNet] = (gateId, 2);
				AddSink(sinksOf, prevNet, gateId, 1);
				AddSink(sinksOf, ports[i], gateId, 0);
				subChips.Add(new SubChipDescription("XOR", gateId, "", Vector2.zero, null, null, null, 0));
				gateId++;
				prevNet = nextNet;
			}
			driverOf[outNet] = (gateId, 2);
			AddSink(sinksOf, prevNet, gateId, 0);
			AddSink(sinksOf, prevNet, gateId, 1);
			subChips.Add(new SubChipDescription("NAND", gateId, "", Vector2.zero, null, null, null, 0));
			gateId++;
			return true;
		}

		/// <summary>Uses native 3-input chip (XOR3/OR3) if in library; otherwise expands into tree of 2-input gates.</summary>
		static bool SynthesizeMultiInputGate(string dlsType, string outNet, List<string> ports, int numInputs, Dictionary<string, (int, int)> driverOf, Dictionary<string, List<(int, int)>> sinksOf, List<SubChipDescription> subChips, ref int gateId, ChipLibrary chipLibrary, out string error)
		{
			error = null;

			// For exactly 3 inputs, try native XOR3/OR3 if available in the chip library
			if (numInputs == 3 && (dlsType == "XOR" || dlsType == "OR") && chipLibrary != null)
			{
				string[] namesToTry = dlsType == "XOR" ? new[] { "XOR3", "3XOR" } : new[] { "OR3", "3OR" }; // XOR3/OR3 = builtin; 3XOR/3OR = user-created aliases
				foreach (string chipName in namesToTry)
				{
					if (chipLibrary.TryGetChipDescription(chipName, out var desc) &&
					    desc.InputPins != null && desc.InputPins.Length >= 3 &&
					    desc.OutputPins != null && desc.OutputPins.Length >= 1)
					{
						driverOf[outNet] = (gateId, 3);
						AddSink(sinksOf, ports[1], gateId, 0);
						AddSink(sinksOf, ports[2], gateId, 1);
						AddSink(sinksOf, ports[3], gateId, 2);
						subChips.Add(new SubChipDescription(chipName, gateId, "", Vector2.zero, null, null, null, 0));
						gateId++;
						return true;
					}
				}
			}

			bool invertOutput = dlsType == "NAND" || dlsType == "NOR";
			string baseType = dlsType == "NAND" ? "AND" : dlsType == "NOR" ? "OR" : dlsType;

			string prevNet = ports[1];
			for (int i = 2; i < ports.Count; i++)
			{
				string nextNet = i == ports.Count - 1 && !invertOutput ? outNet : $"_m{i}_{gateId}";
				driverOf[nextNet] = (gateId, 2);
				AddSink(sinksOf, prevNet, gateId, 1);
				AddSink(sinksOf, ports[i], gateId, 0);
				subChips.Add(new SubChipDescription(baseType, gateId, "", Vector2.zero, null, null, null, 0));
				gateId++;
				prevNet = nextNet;
			}

			if (invertOutput)
			{
				driverOf[outNet] = (gateId, 2);
				AddSink(sinksOf, prevNet, gateId, 0);
				AddSink(sinksOf, prevNet, gateId, 1);
				subChips.Add(new SubChipDescription("NAND", gateId, "", Vector2.zero, null, null, null, 0));
				gateId++;
			}
			return true;
		}

	}
}
