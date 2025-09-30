using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DLS.Description
{
	/// <summary>
	/// Represents a single row in a truth table.
	/// </summary>
	public class TruthTableRow
	{
		public bool[] Inputs { get; set; }
		public bool[] Outputs { get; set; }
		
		public TruthTableRow(bool[] inputs, bool[] outputs)
		{
			Inputs = inputs;
			Outputs = outputs;
		}
		
		public override string ToString()
		{
			var inputStr = string.Join(",", Inputs.Select(b => b ? "1" : "0"));
			var outputStr = string.Join(",", Outputs.Select(b => b ? "1" : "0"));
			return $"[{inputStr}]→[{outputStr}]";
		}
	}
	
	/// <summary>
	/// Represents a complete truth table for a circuit.
	/// </summary>
	public class TruthTable
	{
		public int InputCount { get; set; }
		public int OutputCount { get; set; }
		public List<TruthTableRow> Rows { get; set; }
		
		public TruthTable(int inputCount, int outputCount)
		{
			InputCount = inputCount;
			OutputCount = outputCount;
			Rows = new List<TruthTableRow>();
		}
		
		public void AddRow(bool[] inputs, bool[] outputs)
		{
			if (inputs.Length != InputCount)
				throw new ArgumentException($"Expected {InputCount} inputs, got {inputs.Length}");
			if (outputs.Length != OutputCount)
				throw new ArgumentException($"Expected {OutputCount} outputs, got {outputs.Length}");
				
			Rows.Add(new TruthTableRow(inputs, outputs));
		}
		
		public override string ToString()
		{
			return string.Join(" | ", Rows.Select(r => r.ToString()));
		}
		
		public override bool Equals(object obj)
		{
			if (obj is TruthTable other)
			{
				if (InputCount != other.InputCount || OutputCount != other.OutputCount || Rows.Count != other.Rows.Count)
					return false;
					
				for (int i = 0; i < Rows.Count; i++)
				{
					var row1 = Rows[i];
					var row2 = other.Rows[i];
					
					if (!row1.Inputs.SequenceEqual(row2.Inputs) || !row1.Outputs.SequenceEqual(row2.Outputs))
						return false;
				}
				return true;
			}
			return false;
		}
		
		public override int GetHashCode()
		{
			int hash = InputCount ^ OutputCount ^ Rows.Count;
			foreach (var row in Rows)
			{
				hash ^= row.Inputs.Aggregate(0, (acc, b) => acc ^ (b ? 1 : 0));
				hash ^= row.Outputs.Aggregate(0, (acc, b) => acc ^ (b ? 1 : 0));
			}
			return hash;
		}
	}

	/// <summary>
	/// Detects logical chip types by analyzing truth tables for combinational circuits.
	/// Supports chips with 1-3 inputs and 1-2 outputs for optimal performance.
	/// </summary>
	public static class ChipTypeDetector
	{
		// Detection limits for performance optimization
		const int MAX_INPUT_BITS = 3;
		const int MAX_OUTPUT_BITS = 2;
		
		// Structured truth table lookup dictionary for common gate patterns
		static readonly Dictionary<TruthTable, ChipTypeId> TruthTableLookup = new Dictionary<TruthTable, ChipTypeId>
		{
			// Single input, single output (2 possible input combinations)
			{ CreateTruthTable(1, 1, new[] {
				new TruthTableRow(new[] { false }, new[] { true }),   // 0→1
				new TruthTableRow(new[] { true }, new[] { false })    // 1→0
			}), ChipTypeId.NOT },
			
			{ CreateTruthTable(1, 1, new[] {
				new TruthTableRow(new[] { false }, new[] { false }),  // 0→0
				new TruthTableRow(new[] { true }, new[] { true })     // 1→1
			}), ChipTypeId.Buffer },
			
			// Two inputs, single output (4 possible input combinations)
			{ CreateTruthTable(2, 1, new[] {
				new TruthTableRow(new[] { false, false }, new[] { false }), // 00→0
				new TruthTableRow(new[] { false, true }, new[] { false }),  // 01→0
				new TruthTableRow(new[] { true, false }, new[] { false }),  // 10→0
				new TruthTableRow(new[] { true, true }, new[] { true })     // 11→1
			}), ChipTypeId.AND },
			
			{ CreateTruthTable(2, 1, new[] {
				new TruthTableRow(new[] { false, false }, new[] { false }), // 00→0
				new TruthTableRow(new[] { false, true }, new[] { true }),   // 01→1
				new TruthTableRow(new[] { true, false }, new[] { true }),   // 10→1
				new TruthTableRow(new[] { true, true }, new[] { true })     // 11→1
			}), ChipTypeId.OR },
			
			{ CreateTruthTable(2, 1, new[] {
				new TruthTableRow(new[] { false, false }, new[] { false }), // 00→0
				new TruthTableRow(new[] { false, true }, new[] { true }),   // 01→1
				new TruthTableRow(new[] { true, false }, new[] { true }),   // 10→1
				new TruthTableRow(new[] { true, true }, new[] { false })    // 11→0
			}), ChipTypeId.XOR },
			
			{ CreateTruthTable(2, 1, new[] {
				new TruthTableRow(new[] { false, false }, new[] { true }),  // 00→1
				new TruthTableRow(new[] { false, true }, new[] { true }),   // 01→1
				new TruthTableRow(new[] { true, false }, new[] { true }),   // 10→1
				new TruthTableRow(new[] { true, true }, new[] { false })    // 11→0
			}), ChipTypeId.NAND },
			
			{ CreateTruthTable(2, 1, new[] {
				new TruthTableRow(new[] { false, false }, new[] { true }),  // 00→1
				new TruthTableRow(new[] { false, true }, new[] { false }),  // 01→0
				new TruthTableRow(new[] { true, false }, new[] { false }),  // 10→0
				new TruthTableRow(new[] { true, true }, new[] { false })    // 11→0
			}), ChipTypeId.NOR },
			
			{ CreateTruthTable(2, 1, new[] {
				new TruthTableRow(new[] { false, false }, new[] { true }),  // 00→1
				new TruthTableRow(new[] { false, true }, new[] { false }),  // 01→0
				new TruthTableRow(new[] { true, false }, new[] { false }),  // 10→0
				new TruthTableRow(new[] { true, true }, new[] { true })     // 11→1
			}), ChipTypeId.XNOR },
			
			// Three inputs, single output (8 possible input combinations)
			{ CreateTruthTable(3, 1, new[] {
				new TruthTableRow(new[] { false, false, false }, new[] { false }), // 000→0
				new TruthTableRow(new[] { false, false, true }, new[] { false }),  // 001→0
				new TruthTableRow(new[] { false, true, false }, new[] { false }),  // 010→0
				new TruthTableRow(new[] { false, true, true }, new[] { false }),   // 011→0
				new TruthTableRow(new[] { true, false, false }, new[] { false }),  // 100→0
				new TruthTableRow(new[] { true, false, true }, new[] { false }),   // 101→0
				new TruthTableRow(new[] { true, true, false }, new[] { false }),   // 110→0
				new TruthTableRow(new[] { true, true, true }, new[] { true })      // 111→1
			}), ChipTypeId.AND3 },
			
			{ CreateTruthTable(3, 1, new[] {
				new TruthTableRow(new[] { false, false, false }, new[] { false }), // 000→0
				new TruthTableRow(new[] { false, false, true }, new[] { true }),   // 001→1
				new TruthTableRow(new[] { false, true, false }, new[] { true }),   // 010→1
				new TruthTableRow(new[] { false, true, true }, new[] { true }),    // 011→1
				new TruthTableRow(new[] { true, false, false }, new[] { true }),   // 100→1
				new TruthTableRow(new[] { true, false, true }, new[] { true }),    // 101→1
				new TruthTableRow(new[] { true, true, false }, new[] { true }),    // 110→1
				new TruthTableRow(new[] { true, true, true }, new[] { true })      // 111→1
			}), ChipTypeId.OR3 }
		};
		
		// Name mapping for auto-complete suggestions
		static readonly Dictionary<ChipTypeId, string> TypeNames = new()
		{
			{ ChipTypeId.Unknown, "Unknown" },
			{ ChipTypeId.NOT, "NOT" },
			{ ChipTypeId.Buffer, "Buffer" },
			{ ChipTypeId.AND, "AND" },
			{ ChipTypeId.OR, "OR" },
			{ ChipTypeId.XOR, "XOR" },
			{ ChipTypeId.NAND, "NAND" },
			{ ChipTypeId.NOR, "NOR" },
			{ ChipTypeId.XNOR, "XNOR" },
			{ ChipTypeId.AND3, "AND3" },
			{ ChipTypeId.OR3, "OR3" },
			{ ChipTypeId.NAND3, "NAND3" },
			{ ChipTypeId.NOR3, "NOR3" },
		};
		
		/// <summary>
		/// Helper method to create a truth table from TruthTableRow array.
		/// </summary>
		static TruthTable CreateTruthTable(int inputCount, int outputCount, TruthTableRow[] rows)
		{
			var truthTable = new TruthTable(inputCount, outputCount);
			foreach (var row in rows)
			{
				truthTable.AddRow(row.Inputs, row.Outputs);
			}
			return truthTable;
		}
		
		/// <summary>
		/// Detects chip type and suggests a name for the given chip description.
		/// Only runs detection for chips within the supported input/output range.
		/// </summary>
		public static (ChipTypeId detectedType, string suggestedName) DetectAndSuggestName(ChipDescription chip)
		{
			if (chip == null) return (ChipTypeId.Unknown, "Unknown");
			
			// Skip detection for chips outside supported range
			int inputBits = chip.InputPins?.Sum(pin => pin.BitCount.BitCount) ?? 0;
			int outputBits = chip.OutputPins?.Sum(pin => pin.BitCount.BitCount) ?? 0;
			
			Debug.Log($"ChipTypeDetector: Analyzing chip '{chip.Name}' - {inputBits} inputs, {outputBits} outputs");
			
			if (inputBits == 0 || inputBits > MAX_INPUT_BITS || outputBits == 0 || outputBits > MAX_OUTPUT_BITS)
			{
				Debug.Log($"ChipTypeDetector: Skipping detection - outside supported range ({inputBits} inputs, {outputBits} outputs)");
				return (ChipTypeId.Unknown, "Unknown");
			}
			
			// Check if chip is combinational (reuse existing logic)
			if (!IsChipCombinational(chip))
			{
				Debug.Log("ChipTypeDetector: Skipping detection - not combinational");
				return (ChipTypeId.Unknown, "Unknown");
			}
			
			// Simple approach: Generate structured truth table and match against known patterns
			var truthTable = GenerateTruthTable(chip);
			Debug.Log($"ChipTypeDetector: Generated truth table: {truthTable}");
			
			// Direct truth table lookup - this is all we need!
			var detectedType = LookupChipType(truthTable);
			var suggestedName = TypeNames[detectedType];
			
			Debug.Log($"ChipTypeDetector: Detected type: {detectedType} ({suggestedName})");
			
			return (detectedType, suggestedName);
		}
		
		/// <summary>
		/// Checks if a chip is purely combinational using simplified logic.
		/// This is a lightweight version that doesn't require full simulation setup.
		/// </summary>
		static bool IsChipCombinational(ChipDescription chip)
		{
			// Basic checks for combinational circuits
			
			// If no sub-chips, check if it's a simple buffer (direct input to output connection)
			if (chip.SubChips == null || chip.SubChips.Length == 0)
			{
				// Simple buffer: 1 input, 1 output, direct connection
				if (chip.InputPins?.Length == 1 && chip.OutputPins?.Length == 1 && 
				    chip.Wires?.Length == 1)
				{
					Debug.Log("Detected simple buffer pattern - treating as combinational");
					return true;
				}
				
				Debug.Log("No sub-chips and not a simple buffer - treating as non-combinational");
				return false;
			}
			
			// Check that all sub-chips are known combinational types
			foreach (var subChip in chip.SubChips)
			{
				string chipName = subChip.Name?.ToUpper();
				
				// Known combinational built-in chips
				if (chipName == "NAND" || chipName == "AND" || chipName == "OR" || 
				    chipName == "XOR" || chipName == "NOT" || chipName == "BUFFER")
				{
					continue; // These are combinational
				}
				
				// For custom chips, we assume they could be combinational
				// In a full implementation, we would recursively check them
				Debug.Log($"Assuming custom chip '{subChip.Name}' is combinational");
			}
			
			// Check for basic structural issues that would make it non-combinational
			if (chip.Wires == null || chip.Wires.Length == 0)
			{
				Debug.Log("No wires found - treating as non-combinational");
				return false;
			}
			
			Debug.Log("Chip appears to be combinational based on structure");
			return true;
		}
		
		/// <summary>
		/// Generates a structured truth table for the given chip description.
		/// Simple approach: evaluate all input combinations and record outputs.
		/// </summary>
		static TruthTable GenerateTruthTable(ChipDescription chip)
		{
			if (chip.SubChips == null || chip.SubChips.Length == 0)
			{
				return null;
			}
			
			int inputBits = chip.InputPins?.Sum(pin => pin.BitCount.BitCount) ?? 0;
			int outputBits = chip.OutputPins?.Sum(pin => pin.BitCount.BitCount) ?? 0;
			
			// Support both single and multi-output circuits
			if (inputBits > 0 && outputBits > 0)
			{
				return EvaluateCircuitTruthTable(chip, inputBits, outputBits);
			}
			
			return null;
		}
		
		// Removed complex circuit structure analysis - we now use simple truth table approach only
		
		/// <summary>
		/// Simple truth table evaluation - just evaluate the circuit for all input combinations.
		/// Now supports multiple outputs!
		/// </summary>
		static TruthTable EvaluateCircuitTruthTable(ChipDescription chip, int inputBits, int outputBits)
		{
			try
			{
				Debug.Log($"Evaluating circuit truth table for {inputBits} input bits and {outputBits} output bits");
				
				// Build circuit graph
				var circuitGraph = BuildCircuitGraph(chip);
				if (circuitGraph == null) 
				{
					Debug.Log("Failed to build circuit graph");
					return null;
				}
				
				Debug.Log($"Built circuit graph with {circuitGraph.Count} nodes");
				
				// Generate structured truth table by evaluating all input combinations
				var truthTable = EvaluateStructuredTruthTable(circuitGraph, inputBits, outputBits, chip);
				Debug.Log($"Generated truth table: {truthTable}");
				
				return truthTable;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Truth table evaluation failed: {ex.Message}");
				return null;
			}
		}
		
		/// <summary>
		/// Represents a simplified circuit graph for analysis.
		/// </summary>
		class CircuitNode
		{
			public int ID;
			public string Name;
			public ChipType BuiltinType;
			public bool IsInput;
			public bool IsOutput;
			public List<CircuitNode> Inputs = new();
			public List<CircuitNode> Outputs = new();
		}
		
		/// <summary>
		/// Builds a circuit graph from the chip description.
		/// </summary>
		static Dictionary<int, CircuitNode> BuildCircuitGraph(ChipDescription chip)
		{
			var nodes = new Dictionary<int, CircuitNode>();
			
			Debug.Log($"Building circuit graph for chip '{chip.Name}'");
			
			// Create input nodes
			if (chip.InputPins != null)
			{
				foreach (var inputPin in chip.InputPins)
				{
					nodes[inputPin.ID] = new CircuitNode
					{
						Name = "INPUT",
						IsInput = true,
						BuiltinType = ChipType.Custom // Inputs are treated as custom
					};
				}
			}
			
			// Create sub-chip nodes
			if (chip.SubChips != null)
			{
				foreach (var subChip in chip.SubChips)
				{
					nodes[subChip.ID] = new CircuitNode
					{
						Name = subChip.Name,
						BuiltinType = GetBuiltinChipType(subChip.Name)
					};
				}
			}
			
			// Create output nodes
			if (chip.OutputPins != null)
			{
				foreach (var outputPin in chip.OutputPins)
				{
					nodes[outputPin.ID] = new CircuitNode
					{
						Name = "OUTPUT",
						IsOutput = true,
						BuiltinType = ChipType.Custom // Outputs are treated as custom
					};
				}
			}
			
			// Connect nodes based on wires
			if (chip.Wires != null)
			{
				foreach (var wire in chip.Wires)
				{
					var sourceNode = GetNodeFromPinAddress(nodes, wire.SourcePinAddress, chip);
					var targetNode = GetNodeFromPinAddress(nodes, wire.TargetPinAddress, chip);
					
					if (sourceNode != null && targetNode != null)
					{
						sourceNode.Outputs.Add(targetNode);
						targetNode.Inputs.Add(sourceNode);
					}
				}
			}
			
			// Log circuit structure
			var inputCount = nodes.Values.Count(n => n.IsInput);
			var outputCount = nodes.Values.Count(n => n.IsOutput);
			var nandCount = nodes.Values.Count(n => n.Name == "NAND");
			
			// Debug: Log all nodes
			Debug.Log($"Circuit graph: {inputCount} inputs, {outputCount} outputs, {nandCount} NAND gates");
			Debug.Log($"Total nodes: {nodes.Count}");
			foreach (var node in nodes.Values)
			{
				Debug.Log($"  Node: ID={node.ID}, Name='{node.Name}', IsInput={node.IsInput}, IsOutput={node.IsOutput}, BuiltinType={node.BuiltinType}");
			}
			
			return nodes;
		}
		
		/// <summary>
		/// Gets a circuit node from a pin address.
		/// </summary>
		static CircuitNode GetNodeFromPinAddress(Dictionary<int, CircuitNode> nodes, PinAddress address, ChipDescription chip)
		{
			// Check if it's an input pin
			if (chip.InputPins != null)
			{
				foreach (var inputPin in chip.InputPins)
				{
					if (inputPin.ID == address.PinOwnerID)
					{
						return nodes.TryGetValue(inputPin.ID, out var node) ? node : null;
					}
				}
			}
			
			// Check if it's an output pin
			if (chip.OutputPins != null)
			{
				foreach (var outputPin in chip.OutputPins)
				{
					if (outputPin.ID == address.PinOwnerID)
					{
						return nodes.TryGetValue(outputPin.ID, out var node) ? node : null;
					}
				}
			}
			
			// Check if it's a sub-chip
			return nodes.TryGetValue(address.PinOwnerID, out var subChipNode) ? subChipNode : null;
		}
		
		/// <summary>
		/// Maps chip names to built-in chip types.
		/// </summary>
		static ChipType GetBuiltinChipType(string chipName)
		{
			return chipName.ToUpper() switch
			{
				"NAND" => ChipType.Nand,
				"AND" => ChipType.Custom, // Custom AND implementation
				"OR" => ChipType.Custom,  // Custom OR implementation
				"XOR" => ChipType.Custom, // Custom XOR implementation
				"NOT" => ChipType.Custom, // Custom NOT implementation
				_ => ChipType.Custom
			};
		}
		
		/// <summary>
		/// Evaluates a structured truth table for the circuit, supporting multiple outputs.
		/// </summary>
		static TruthTable EvaluateStructuredTruthTable(Dictionary<int, CircuitNode> circuitGraph, int inputBits, int outputBits, ChipDescription chip)
		{
			Debug.Log($"Evaluating structured truth table: {inputBits} input bits, {outputBits} output bits");
			
			// Find output nodes
			var outputNodes = circuitGraph.Values.Where(n => n.IsOutput).ToList();
			if (outputNodes.Count == 0) 
			{
				Debug.Log("No output nodes found in circuit graph");
				return null;
			}
			
			// Find input nodes
			var inputNodes = circuitGraph.Values.Where(n => n.IsInput).ToList();
			if (inputNodes.Count == 0)
			{
				Debug.Log("No input nodes found in circuit graph");
				return null;
			}
			
			Debug.Log($"Found {inputNodes.Count} input nodes and {outputNodes.Count} output nodes");
			
			var truthTable = new TruthTable(inputBits, outputBits);
			int numInputCombinations = 1 << inputBits;
			Debug.Log($"Generating truth table with {numInputCombinations} combinations");
			
			// Evaluate each input combination
			for (int input = 0; input < numInputCombinations; input++)
			{
				// Convert input number to boolean array (MSB first to match dictionary order)
				var inputValues = new bool[inputBits];
				for (int i = 0; i < inputBits; i++)
				{
					inputValues[i] = (input & (1 << (inputBits - 1 - i))) != 0;
				}
				
				// Evaluate all outputs for this input combination
				var outputValues = new bool[outputBits];
				for (int outputIndex = 0; outputIndex < outputBits; outputIndex++)
				{
					if (outputIndex < outputNodes.Count)
					{
						outputValues[outputIndex] = EvaluateCircuitForInput(circuitGraph, inputNodes, outputNodes[outputIndex], input);
					}
				}
				
				truthTable.AddRow(inputValues, outputValues);
				Debug.Log($"Input {input} ({string.Join(",", inputValues.Select(b => b ? "1" : "0"))}): Output = {string.Join(",", outputValues.Select(b => b ? "1" : "0"))}");
			}
			
			Debug.Log($"Generated structured truth table: {truthTable}");
			return truthTable;
		}
		
		/// <summary>
		/// Evaluates the circuit for a specific input combination.
		/// </summary>
		static bool EvaluateCircuitForInput(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode, int inputValue)
		{
			Debug.Log($"EvaluateCircuitForInput: inputValue={inputValue}, inputNodes={inputNodes.Count}, outputNode={outputNode.Name}");
			
			// Use the simulation callback if available, otherwise fall back to pattern matching
			if (_simulationCallback != null)
			{
				bool result = _simulationCallback(inputValue);
				Debug.Log($"Simulation callback result: {result}");
				return result;
			}
			
			// Fall back to pattern matching if no simulation callback
			bool patternResult = AnalyzeGatePattern(circuitGraph, inputNodes, outputNode, inputValue);
			Debug.Log($"Pattern analysis result: {patternResult}");
			return patternResult;
		}
		
		/// <summary>
		/// Callback function for circuit evaluation using the actual simulation engine.
		/// This is set by the calling code that has access to the simulation assemblies.
		/// </summary>
		static Func<int, bool> _simulationCallback;
		
		/// <summary>
		/// Sets the simulation callback function for circuit evaluation.
		/// </summary>
		public static void SetSimulationCallback(Func<int, bool> callback)
		{
			_simulationCallback = callback;
		}
		
		/// <summary>
		/// Simple circuit evaluation that can handle basic NAND gate circuits.
		/// </summary>
		static bool EvaluateSimpleCircuit(Dictionary<int, CircuitNode> circuitGraph, List<bool> inputValues, CircuitNode outputNode)
		{
			Debug.Log($"EvaluateSimpleCircuit: {inputValues.Count} inputs, output={outputNode.Name}");
			
			// Create a cache for evaluated nodes
			var evaluatedNodes = new Dictionary<CircuitNode, bool>();
			
			// Set input values
			var inputNodes = circuitGraph.Values.Where(n => n.IsInput).OrderBy(n => n.Name).ToList();
			for (int i = 0; i < inputNodes.Count && i < inputValues.Count; i++)
			{
				evaluatedNodes[inputNodes[i]] = inputValues[i];
				Debug.Log($"Set input {inputNodes[i].Name} = {inputValues[i]}");
			}
			
			// Evaluate the output node
			return EvaluateNode(outputNode, evaluatedNodes);
		}
		
		/// <summary>
		/// Recursively evaluates a node in the circuit.
		/// </summary>
		static bool EvaluateNode(CircuitNode node, Dictionary<CircuitNode, bool> evaluatedNodes)
		{
			Debug.Log($"Evaluating node: {node.Name}");
			
			// If already evaluated, return cached result
			if (evaluatedNodes.ContainsKey(node))
			{
				Debug.Log($"Node {node.Name} already evaluated: {evaluatedNodes[node]}");
				return evaluatedNodes[node];
			}
			
			// Input nodes should already be set
			if (node.IsInput)
			{
				Debug.Log($"Node {node.Name} is input but not in cache");
				return false; // Default to false if not found
			}
			
			// Evaluate NAND gates
			if (node.Name == "NAND")
			{
				Debug.Log($"Evaluating NAND gate with {node.Inputs.Count} inputs");
				
				if (node.Inputs.Count < 2)
				{
					Debug.Log($"NAND gate has insufficient inputs: {node.Inputs.Count}");
					return false;
				}
				
				// Evaluate both inputs
				bool input1 = EvaluateNode(node.Inputs[0], evaluatedNodes);
				bool input2 = EvaluateNode(node.Inputs[1], evaluatedNodes);
				
				bool result = !(input1 && input2); // NAND logic
				evaluatedNodes[node] = result;
				Debug.Log($"NAND({input1}, {input2}) = {result}");
				return result;
			}
			
			// For other gate types, use the built-in logic
			if (node.BuiltinType != ChipType.Custom)
			{
				var gateType = node.Name.ToUpper() switch
				{
					"AND" => ChipTypeId.AND,
					"OR" => ChipTypeId.OR,
					"XOR" => ChipTypeId.XOR,
					"NAND" => ChipTypeId.NAND,
					"NOR" => ChipTypeId.NOR,
					"XNOR" => ChipTypeId.XNOR,
					"NOT" => ChipTypeId.NOT,
					_ => ChipTypeId.Unknown
				};
				
				if (gateType != ChipTypeId.Unknown && node.Inputs.Count > 0)
				{
					var inputValues = new List<bool>();
					foreach (var input in node.Inputs)
					{
						inputValues.Add(EvaluateNode(input, evaluatedNodes));
					}
					
					bool result = CalculateOutputForType(gateType, inputValues);
					evaluatedNodes[node] = result;
					Debug.Log($"Built-in gate {node.Name}({string.Join(",", inputValues)}) = {result}");
					return result;
				}
			}
			
			Debug.Log($"Cannot evaluate node {node.Name}");
			return false;
		}
		
		/// <summary>
		/// Analyzes common gate patterns to determine output.
		/// </summary>
		static bool AnalyzeGatePattern(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode, int inputValue)
		{
			// Extract individual input values
			var inputValues = new List<bool>();
			int tempInput = inputValue;
			
			foreach (var inputNode in inputNodes.OrderBy(n => n.Name))
			{
				inputValues.Add((tempInput & 1) != 0);
				tempInput >>= 1;
			}
			
			Debug.Log($"Analyzing gate pattern: input values = [{string.Join(", ", inputValues)}]");
			
			// Check if this is a simple circuit with a single connected built-in gate
			var connectedSubChips = GetConnectedSubChips(circuitGraph, inputNodes, outputNode);
			
			if (connectedSubChips.Count == 1)
			{
				var gate = connectedSubChips[0];
				Debug.Log($"Found single connected built-in gate: {gate.Name}");
				
				// Special case: Single NAND gate with both inputs tied together = NOT gate
				if (gate.Name.ToUpper() == "NAND" && inputNodes.Count == 1)
				{
					Debug.Log("Detected NAND gate functioning as NOT gate (both inputs tied together)");
					bool result = !inputValues[0]; // NOT gate logic
					Debug.Log($"NAND-as-NOT gate result: {result}");
					return result;
				}
				
				// Map built-in gate names to chip types
				var gateType = gate.Name.ToUpper() switch
				{
					"AND" => ChipTypeId.AND,
					"OR" => ChipTypeId.OR,
					"XOR" => ChipTypeId.XOR,
					"NAND" => ChipTypeId.NAND,
					"NOR" => ChipTypeId.NOR,
					"XNOR" => ChipTypeId.XNOR,
					"NOT" => ChipTypeId.NOT,
					_ => ChipTypeId.Unknown
				};
				
				if (gateType != ChipTypeId.Unknown)
				{
					bool result = CalculateOutputForType(gateType, inputValues);
					Debug.Log($"Connected built-in gate {gate.Name} result: {result}");
					return result;
				}
			}
			
			// Fall back to NAND-based analysis for custom implementations
			var detectedType = AnalyzeNandBasedGateType(circuitGraph, inputNodes, outputNode);
			
			// Calculate output based on detected type and input values
			bool fallbackResult = CalculateOutputForType(detectedType, inputValues);
			Debug.Log($"Fallback analysis result: {fallbackResult}");
			return fallbackResult;
		}
		
		/// <summary>
		/// Gets sub-chips that are actually connected in the circuit (ignores floating/unconnected components).
		/// </summary>
		static List<CircuitNode> GetConnectedSubChips(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode)
		{
			var allSubChips = circuitGraph.Values.Where(n => !n.IsInput && !n.IsOutput).ToList();
			var connectedSubChips = new List<CircuitNode>();
			
			Debug.Log($"Analyzing {allSubChips.Count} sub-chips for connectivity");
			
			foreach (var subChip in allSubChips)
			{
				bool isConnected = false;
				
				// Check if the sub-chip is connected to inputs or outputs
				// A sub-chip is considered connected if:
				// 1. It receives input from input nodes or other sub-chips
				// 2. It provides output to output nodes or other sub-chips
				
				// Check if it has inputs from connected sources
				if (subChip.Inputs.Any(input => input.IsInput || connectedSubChips.Contains(input)))
				{
					isConnected = true;
				}
				
				// Check if it provides output to connected destinations
				if (subChip.Outputs.Any(output => output.IsOutput || output.Outputs.Any() || output.Inputs.Any()))
				{
					isConnected = true;
				}
				
				// Special case: if it's the only sub-chip and has any connections, it's connected
				if (allSubChips.Count == 1 && (subChip.Inputs.Any() || subChip.Outputs.Any()))
				{
					isConnected = true;
				}
				
				if (isConnected)
				{
					connectedSubChips.Add(subChip);
					Debug.Log($"Sub-chip '{subChip.Name}' is connected");
				}
				else
				{
					Debug.Log($"Sub-chip '{subChip.Name}' is unconnected (floating)");
				}
			}
			
			Debug.Log($"Found {connectedSubChips.Count} connected sub-chips out of {allSubChips.Count} total");
			return connectedSubChips;
		}
		
		/// <summary>
		/// Calculates the output value for a given gate type and input values.
		/// </summary>
		static bool CalculateOutputForType(ChipTypeId gateType, List<bool> inputValues)
		{
			switch (gateType)
			{
				case ChipTypeId.NOT:
					return inputValues.Count == 1 ? !inputValues[0] : false;
					
				case ChipTypeId.AND:
					return inputValues.Count == 2 ? (inputValues[0] && inputValues[1]) : false;
					
				case ChipTypeId.OR:
					return inputValues.Count == 2 ? (inputValues[0] || inputValues[1]) : false;
					
				case ChipTypeId.XOR:
					return inputValues.Count == 2 ? (inputValues[0] != inputValues[1]) : false;
					
				case ChipTypeId.NAND:
					return inputValues.Count == 2 ? !(inputValues[0] && inputValues[1]) : false;
					
				case ChipTypeId.NOR:
					return inputValues.Count == 2 ? !(inputValues[0] || inputValues[1]) : false;
					
				case ChipTypeId.XNOR:
					return inputValues.Count == 2 ? (inputValues[0] == inputValues[1]) : false;
					
				case ChipTypeId.Buffer:
					return inputValues.Count == 1 ? inputValues[0] : false;
					
				default:
					return false;
			}
		}
		
		/// <summary>
		/// Checks if the circuit is a NOT gate pattern (1 input, 1 NAND with both inputs connected to same source).
		/// </summary>
		static bool IsNotGatePattern(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode)
		{
			// NOT gate pattern: 1 input, 1 NAND gate, 1 output
			if (inputNodes.Count != 1) return false;
			
			var nandChips = circuitGraph.Values.Where(n => n.Name == "NAND").ToList();
			if (nandChips.Count != 1) return false;
			
			var nandChip = nandChips[0];
			var inputNode = inputNodes[0];
			
			// Check if both inputs of the NAND gate are connected to the same input node
			// This creates a NOT gate: NAND(A, A) = !A
			var nandInputs = nandChip.Inputs.Where(inp => inp == inputNode).ToList();
			
			return nandInputs.Count == 2; // Both NAND inputs connected to same input
		}
		
		/// <summary>
		/// Checks if the circuit is a NAND-based gate implementation.
		/// </summary>
		static bool IsNandBasedGate(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode)
		{
			// Look for patterns where all sub-chips are NAND gates
			var nandChips = circuitGraph.Values.Where(n => n.Name == "NAND").ToList();
			return nandChips.Count > 0;
		}
		
		/// <summary>
		/// Evaluates common NAND-based gate patterns by analyzing the actual circuit structure.
		/// </summary>
		static bool EvaluateNandBasedGate(List<bool> inputValues)
		{
			// This method is called from AnalyzeGatePattern, but we need the circuit structure
			// to properly determine the gate type. For now, return false to indicate
			// that we need more sophisticated analysis.
			return false;
		}
		
		/// <summary>
		/// Analyzes NAND-based gate patterns by examining the circuit structure.
		/// </summary>
		static ChipTypeId AnalyzeNandBasedGateType(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode)
		{
			var nandChips = circuitGraph.Values.Where(n => n.Name == "NAND").ToList();
			
			Debug.Log($"NAND-based gate analysis: {inputNodes.Count} inputs, {nandChips.Count} NAND gates");
			
			if (inputNodes.Count == 1 && nandChips.Count == 1)
			{
				// Single input, single NAND - check for NOT pattern
				if (IsNotGatePattern(circuitGraph, inputNodes, outputNode))
				{
					Debug.Log("Detected NOT gate pattern");
					return ChipTypeId.NOT;
				}
			}
			else if (inputNodes.Count == 2)
			{
				// Two inputs - check for specific structural patterns first
				
				// Check for classic 2-NAND AND pattern
				if (nandChips.Count == 2 && IsAndGatePattern(circuitGraph, inputNodes, outputNode))
				{
					Debug.Log("Detected classic 2-NAND AND gate pattern");
					return ChipTypeId.AND;
				}
				
				// For other AND implementations, we'll let the truth table analysis handle it
				// This allows detection of AND gates built with different numbers of gates
				Debug.Log($"AND pattern check: {nandChips.Count} NAND gates found, will fall back to truth table analysis");
				
				// Check for other gate types with specific patterns
				if (nandChips.Count >= 3)
				{
					if (IsOrGatePattern(circuitGraph, inputNodes, outputNode))
					{
						Debug.Log("Detected OR gate pattern");
						return ChipTypeId.OR;
					}
					else if (IsXorGatePattern(circuitGraph, inputNodes, outputNode))
					{
						Debug.Log("Detected XOR gate pattern");
						return ChipTypeId.XOR;
					}
				}
			}
			
			Debug.Log("No NAND-based structural pattern matched");
			return ChipTypeId.Unknown;
		}
		
		/// <summary>
		/// Checks if the circuit is an AND gate pattern: NAND(NAND(A, B), NAND(A, B)).
		/// </summary>
		static bool IsAndGatePattern(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode)
		{
			Debug.Log("Checking for AND gate pattern");
			
			if (inputNodes.Count != 2) 
			{
				Debug.Log($"AND pattern check failed: expected 2 inputs, got {inputNodes.Count}");
				return false;
			}
			
			var nandChips = circuitGraph.Values.Where(n => n.Name == "NAND").ToList();
			if (nandChips.Count != 2) 
			{
				Debug.Log($"AND pattern check failed: expected 2 NAND gates, got {nandChips.Count}");
				return false;
			}
			
			var inputA = inputNodes[0];
			var inputB = inputNodes[1];
			var firstNand = nandChips[0];
			var secondNand = nandChips[1];
			
			// Check if first NAND gets both inputs A and B
			bool firstNandGetsA = firstNand.Inputs.Contains(inputA);
			bool firstNandGetsB = firstNand.Inputs.Contains(inputB);
			
			// Check if second NAND gets the output of first NAND on both inputs
			bool secondNandGetsFirstOutput = secondNand.Inputs.Contains(firstNand);
			int secondNandInputCount = secondNand.Inputs.Count(inp => inp == firstNand);
			
			bool isAndPattern = firstNandGetsA && firstNandGetsB && secondNandGetsFirstOutput && secondNandInputCount == 2;
			
			Debug.Log($"AND pattern check: firstNandGetsA={firstNandGetsA}, firstNandGetsB={firstNandGetsB}, secondNandGetsFirstOutput={secondNandGetsFirstOutput}, secondNandInputCount={secondNandInputCount}, result={isAndPattern}");
			
			return isAndPattern;
		}
		
		/// <summary>
		/// Checks if the circuit is an OR gate pattern: NAND(NAND(A, A), NAND(B, B)).
		/// </summary>
		static bool IsOrGatePattern(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode)
		{
			// OR gate implementation varies, this is a simplified check
			// For now, return false to avoid false positives
			return false;
		}
		
		/// <summary>
		/// Checks if the circuit is an XOR gate pattern.
		/// </summary>
		static bool IsXorGatePattern(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode)
		{
			// XOR gate implementation varies, this is a simplified check
			// For now, return false to avoid false positives
			return false;
		}
		
		/// <summary>
		/// Checks if the circuit is a simple buffer pattern (direct connection from input to output).
		/// </summary>
		static bool IsBufferPattern(Dictionary<int, CircuitNode> circuitGraph, List<CircuitNode> inputNodes, CircuitNode outputNode)
		{
			Debug.Log("Checking for Buffer pattern");
			
			// Buffer pattern: 1 input, 1 output, no sub-chips (direct connection)
			if (inputNodes.Count != 1) 
			{
				Debug.Log($"Buffer pattern check failed: expected 1 input, got {inputNodes.Count}");
				return false;
			}
			
			var subChips = circuitGraph.Values.Where(n => !n.IsInput && !n.IsOutput).ToList();
			if (subChips.Count > 0) 
			{
				Debug.Log($"Buffer pattern check failed: expected 0 sub-chips, got {subChips.Count}");
				return false; // Has sub-chips, not a simple buffer
			}
			
			// Check if input is directly connected to output
			var inputNode = inputNodes[0];
			bool isDirectlyConnected = outputNode.Inputs.Contains(inputNode);
			
			Debug.Log($"Buffer pattern check: input directly connected to output = {isDirectlyConnected}");
			
			return isDirectlyConnected;
		}
		
		/// <summary>
		/// Looks up the chip type from a structured truth table.
		/// </summary>
		static ChipTypeId LookupChipType(TruthTable truthTable)
		{
			if (truthTable == null) return ChipTypeId.Unknown;
			
			return TruthTableLookup.TryGetValue(truthTable, out var type) ? type : ChipTypeId.Unknown;
		}
		
		/// <summary>
		/// Gets the display name for a chip type ID.
		/// </summary>
		public static string GetTypeName(ChipTypeId typeId)
		{
			return TypeNames.TryGetValue(typeId, out var name) ? name : "Unknown";
		}
	}
}
