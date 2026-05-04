using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DLS.Game;
using UnityEngine;

namespace DLS.SaveSystem
{
	/// <summary>
	/// Parses structural Verilog and produces a netlist for circuit building.
	/// Supports: input/output/wire declarations, and/nand/or/nor/xor/xnor/not/buf gate primitives.
	/// </summary>
	public sealed class VerilogParseResult
	{
		public string ModuleName;
		public List<string> Inputs = new();
		public List<string> Outputs = new();
		public List<string> Wires = new();
		public List<VerilogGate> Gates = new();
		public string ParseError;
		/// <summary>Maps signal name to bit width (1 if absent). Used for multi-bit buses.</summary>
		public Dictionary<string, int> SignalWidths = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>Returns the bit-level net name for a given bit index (0 = LSB).</summary>
		public string BitNetName(string baseName, int bitIndex)
		{
			int w = SignalWidths.TryGetValue(baseName, out int w0) ? w0 : 1;
			if (w <= 1) return baseName;
			return $"{baseName}_b{bitIndex}";
		}

		/// <summary>Gets width of a signal; 1 if unknown.</summary>
		public int GetWidth(string name) => SignalWidths.TryGetValue(name, out int w) ? w : 1;
	}

	public struct VerilogGate
	{
		public string Type;       // and, nand, or, nor, xor, xnor, not, buf
		public string InstanceId;
		public List<string> Ports; // For and/or/...: [output, in1, in2,...]. For not/buf: [out1, out2,..., input]
	}

	public static class VerilogParser
	{
		// Regex to strip comments and normalize whitespace
		static readonly Regex LineComment = new(@"//[^\n]*", RegexOptions.Compiled);
		static readonly Regex BlockComment = new(@"/\*[\s\S]*?\*/", RegexOptions.Compiled);

		public static VerilogParseResult Parse(string content) => Parse(content, null);

		public static VerilogParseResult Parse(string content, ChipLibrary chipLibrary)
		{
			var result = new VerilogParseResult { ModuleName = "circuit" };
			if (string.IsNullOrWhiteSpace(content))
			{
				result.ParseError = "Empty file";
				return result;
			}

			string src = Normalize(content);
			if (string.IsNullOrEmpty(src))
			{
				result.ParseError = "No content after removing comments";
				return result;
			}

			Match modMatch = Regex.Match(src, @"module\s+(\w+)\s*\(([^)]*)\)\s*;", RegexOptions.IgnoreCase);
			if (!modMatch.Success)
			{
				result.ParseError = "Could not find module declaration (expected: module name(...);)";
				return result;
			}

			result.ModuleName = modMatch.Groups[1].Value;
			string portList = modMatch.Groups[2].Value;

			// Parse port list: input a, b; output c, d; or .a(a), .b(b) style - we support input/output/wire style
			ParsePortList(portList, result);

			// Get everything between module params and endmodule
			int bodyStart = modMatch.Index + modMatch.Length;
			int endIdx = src.IndexOf("endmodule", StringComparison.OrdinalIgnoreCase);
			string body = endIdx >= 0 ? src.Substring(bodyStart, endIdx - bodyStart) : src.Substring(bodyStart);

			ParseBody(body, result, chipLibrary);

			// Expand ~x in gate ports to explicit not gates
			ExpandInvertedPorts(result);

			return result;
		}

		/// <summary>Replaces ~x in gate ports with explicit not(_n_x, x) and substitutes _n_x.</summary>
		static void ExpandInvertedPorts(VerilogParseResult result)
		{
			var inverted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var notGates = new List<VerilogGate>();
			foreach (var g in result.Gates)
			{
				for (int i = 0; i < g.Ports.Count; i++)
				{
					string p = g.Ports[i];
					if (p.StartsWith("~") && p.Length > 1)
					{
						string baseName = p.Substring(1);
						if (!inverted.TryGetValue(baseName, out string notNet))
						{
							notNet = $"_n_{baseName}";
							inverted[baseName] = notNet;
							notGates.Add(new VerilogGate { Type = "not", InstanceId = $"g_not_{baseName}", Ports = new List<string> { notNet, baseName } });
						}
						g.Ports[i] = notNet;
					}
				}
			}
			for (int i = notGates.Count - 1; i >= 0; i--)
				result.Gates.Insert(0, notGates[i]);
		}

		static string Normalize(string content)
		{
			string s = LineComment.Replace(content, "");
			s = BlockComment.Replace(s, "");
			s = Regex.Replace(s, @"\s+", " ");
			s = Regex.Replace(s, @"\s*;\s*", "; ");
			return s.Trim();
		}

		/// <summary>Parse [msb:lsb] or [n] to width. Returns 1 if no bracket.</summary>
		static int ParseWidth(string match)
		{
			if (string.IsNullOrEmpty(match)) return 1;
			Match m = Regex.Match(match.Trim(), @"\[\s*(\d+)\s*:\s*(\d+)\s*\]");
			if (m.Success)
			{
				int hi = int.Parse(m.Groups[1].Value);
				int lo = int.Parse(m.Groups[2].Value);
				return Math.Abs(hi - lo) + 1;
			}
			m = Regex.Match(match.Trim(), @"\[\s*(\d+)\s*\]");
			if (m.Success) return 1; // single bit select
			return 1;
		}

		static void ParsePortList(string portList, VerilogParseResult result)
		{
			// Match: input [7:0] a, b or input a, b
			foreach (Match m in Regex.Matches(portList, @"(input|output)\s+(\[[^\]]*\])?\s*([^;]+?)(?=\s*(?:input|output|wire|$|\s*;))", RegexOptions.IgnoreCase))
			{
				string kind = m.Groups[1].Value.ToLowerInvariant();
				int width = ParseWidth(m.Groups[2].Value);
				foreach (string name in TokenizeNames(m.Groups[3].Value))
				{
					if (string.IsNullOrEmpty(name)) continue;
					if (kind == "input" && !result.Inputs.Contains(name)) { result.Inputs.Add(name); result.SignalWidths[name] = width; }
					else if (kind == "output" && !result.Outputs.Contains(name)) { result.Outputs.Add(name); result.SignalWidths[name] = width; }
				}
			}
		}

		static void ParseBody(string body, VerilogParseResult result, ChipLibrary chipLibrary = null)
		{
			var allWires = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// Match input/output/wire/reg declarations: (input|output|wire|reg) (reg)? [7:0]? a, b;
			foreach (Match m in Regex.Matches(body, @"(input|output|wire|reg)\s+(?:reg\s+)?(\[[^\]]*\])?\s*([^;]+);", RegexOptions.IgnoreCase))
			{
				string kind = m.Groups[1].Value.ToLowerInvariant();
				int width = ParseWidth(m.Groups[2].Value);
				string names = m.Groups[3].Value;
				foreach (string name in TokenizeNames(names))
				{
					string n = name.Trim();
					if (string.IsNullOrEmpty(n)) continue;
					if (kind == "input" && !result.Inputs.Contains(n))
					{ result.Inputs.Add(n); result.SignalWidths[n] = width; }
					else if (kind == "output" && !result.Outputs.Contains(n))
					{ result.Outputs.Add(n); result.SignalWidths[n] = width; }
					else if (kind == "wire" || kind == "reg")
					{
						if (!result.Wires.Contains(n)) { result.Wires.Add(n); allWires.Add(n); }
						result.SignalWidths[n] = width;
					}
				}
			}

			// Also get input/output from module port list if not already found (e.g. "input a, b, output sum, cout")
			Match portDecl = Regex.Match(body, @"(input|output)\s+([^;]+);", RegexOptions.IgnoreCase);
			// Already handled above

			// assign statements (behavioral) -> synthesize to gates
			int gatesBeforeAssign = result.Gates.Count;
			ParseAssignStatements(body, result);

			// always @(*) or always @(sensitivity) combinational/sequential -> synthesize to gates
			int gatesBeforeAlways = result.Gates.Count;
			ParseCombinationalAlways(body, result, chipLibrary);

			// Gate primitives: and, nand, or, nor, xor, xnor (output first), not, buf (input last)
			// Mask always blocks so we don't falsely match "or"/"and"/"not" in sensitivity lists (e.g. "A or CLK")
			string bodyForGates = Regex.Replace(body, @"always\s*@\s*\([^)]*\)\s*[\s\S]*?(?=\s*(?:always\s*@|endmodule|$))", " ");
			int gatesBeforeStructural = result.Gates.Count;
			var gateMatches = Regex.Matches(bodyForGates, @"(and|nand|or|nor|xor|xnor|not|buf)\s*(?:#\s*\([^)]*\)\s*)?([^;]+);", RegexOptions.IgnoreCase);
			foreach (Match stmt in gateMatches)
			{
				string type = stmt.Groups[1].Value.ToLowerInvariant();
				string rest = stmt.Groups[2].Value;
				// Match each instance: "g1(c,a,b)" or "(c,a,b)"
				var gmMatches = Regex.Matches(rest, @"(?:^|,)\s*(?:\w+\s*)?\(([^)]+)\)");
				foreach (Match gm in gmMatches)
				{
					string portStr = gm.Groups[1].Value;
					var ports = TokenizeNames(portStr);

					if (ports.Count < 2)
					{
						continue;
					}

					string instId = $"g{result.Gates.Count}";
					result.Gates.Add(new VerilogGate
					{
						Type = type,
						InstanceId = instId,
						Ports = ports
					});
				}
			}

			// If no inputs/outputs found from declarations, infer from gates
			// drivenBy = produced by gates; drives = consumed by gates
			// Inputs = consumed but not produced; Outputs = produced but not consumed
			if (result.Inputs.Count == 0 && result.Outputs.Count == 0 && result.Gates.Count > 0)
			{
				var drivenBy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var drives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var g in result.Gates)
				{
					if (g.Type == "not" || g.Type == "buf")
					{
						drives.Add(g.Ports[g.Ports.Count - 1]);
						for (int i = 0; i < g.Ports.Count - 1; i++)
							drivenBy.Add(g.Ports[i]);
					}
					else
					{
						drivenBy.Add(g.Ports[0]);
						for (int i = 1; i < g.Ports.Count; i++)
							drives.Add(g.Ports[i]);
					}
				}
				foreach (var d in drives)
				{
					if (!drivenBy.Contains(d))
						result.Inputs.Add(d);
				}
				foreach (var d in drivenBy)
				{
					if (!drives.Contains(d))
						result.Outputs.Add(d);
				}
			}
		}

		static List<string> TokenizeNames(string s)
		{
			var list = new List<string>();
			foreach (string part in s.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string t = part.Trim();
				if (!string.IsNullOrEmpty(t))
					list.Add(t);
			}
			return list;
		}

		/// <summary>Parses assign statements and synthesizes them to gate primitives. Supports: ~, &amp;, |, ^, ( ). Multi-bit: expands per bit.</summary>
		static void ParseAssignStatements(string body, VerilogParseResult result)
		{
			foreach (Match m in Regex.Matches(body, @"assign\s+(\w+)\s*=\s*([^;]+);", RegexOptions.IgnoreCase))
			{
				string lhs = m.Groups[1].Value.Trim();
				string rhs = m.Groups[2].Value.Trim();
				if (string.IsNullOrEmpty(lhs) || string.IsNullOrEmpty(rhs)) continue;

				int width = result.GetWidth(lhs);
				int gateCounter = result.Gates.Count;
				bool ok = width > 1
					? SynthesizeAssignExpressionMultiBit(rhs, lhs, result, ref gateCounter)
					: SynthesizeAssignExpression(rhs, lhs, result, ref gateCounter);
				if (ok)
				{
					if (!result.Outputs.Contains(lhs) && !result.Wires.Contains(lhs) && !result.Inputs.Contains(lhs))
						result.Wires.Add(lhs);
				}
			}
		}

		/// <summary>Expands multi-bit assign to per-bit gates. LHS and RHS must have same width. +,-,<<,>> are full-width and handled directly.</summary>
		static bool SynthesizeAssignExpressionMultiBit(string expr, string outputBase, VerilogParseResult result, ref int gateCounter)
		{
			int width = result.GetWidth(outputBase);
			if (width <= 1) return SynthesizeAssignExpression(expr, outputBase, result, ref gateCounter);

			expr = StripOuterParentheses(expr.Trim());
			// Multi-bit constant e.g. 4'b0000, 4'b0001
			var (constWidth, constVal) = ParseVerilogConstant(expr);
			if (constWidth >= 0)
			{
				// Use LHS width when constant has no explicit width (e.g. plain 0 -> extend to all bits)
				int w = (constWidth > 1) ? Math.Min(constWidth, width) : width;
				for (int bi = 0; bi < w; bi++)
				{
					string outBit = result.BitNetName(outputBase, bi);
					int bitVal = (constVal >> bi) & 1;
					if (bitVal == 0)
						SynthesizeAssignExpression("1'b0", outBit, result, ref gateCounter);
					else
						SynthesizeAssignExpression("1'b1", outBit, result, ref gateCounter);
				}
				return true;
			}
			int pmIdx = FindTopLevelAddOrSub(expr);
			if (pmIdx >= 0)
			{
				char op = expr[pmIdx];
				string left = expr.Substring(0, pmIdx).Trim();
				string right = expr.Substring(pmIdx + 1).Trim();
				return op == '-' ? SynthesizeSub(left, right, outputBase, result, ref gateCounter) : SynthesizeAdd(left, right, outputBase, result, ref gateCounter);
			}
			int shiftIdx; string shiftOp;
			if (FindTopLevelShift(expr, out shiftIdx, out shiftOp) >= 0)
			{
				string left = expr.Substring(0, shiftIdx).Trim();
				string right = expr.Substring(shiftIdx + shiftOp.Length).Trim();
				return SynthesizeShift(left, right, shiftOp, outputBase, result, ref gateCounter);
			}
			// Ternary cond ? a : b - SynthesizeAssignExpressionBit doesn't handle +/-, so do full-width mux here
			int tQ = FindTopLevelTernary(expr);
			if (tQ >= 0)
			{
				int colon = FindMatchingColon(expr, tQ);
				if (colon < 0) return false;
				string cond = expr.Substring(0, tQ).Trim();
				string a = expr.Substring(tQ + 1, colon - tQ - 1).Trim();
				string b = expr.Substring(colon + 1).Trim();
				return SynthesizeMuxMultiBit(cond, a, b, outputBase, width, result, ref gateCounter);
			}
			// Concatenation: { part1, part2, ... } - rightmost part is LSB
			if (expr.StartsWith("{") && expr.EndsWith("}"))
			{
				string inner = expr.Substring(1, expr.Length - 2).Trim();
				var parts = new List<string>();
				int d = 0, start = 0;
				for (int i = 0; i <= inner.Length; i++)
				{
					if (i < inner.Length)
					{
						if (inner[i] == '[') d++;
						else if (inner[i] == ']') d--;
						else if (d == 0 && inner[i] == ',')
						{
							parts.Add(inner.Substring(start, i - start).Trim());
							start = i + 1;
						}
					}
					else if (start < inner.Length)
						parts.Add(inner.Substring(start).Trim());
				}
				if (parts.Count == 0) return false;
				int outBit = 0;
				for (int pi = parts.Count - 1; pi >= 0; pi--)
				{
					var part = parts[pi];
					int partWidth = ParseConcatenationPartWidth(part, result);
					if (partWidth <= 0) return false;
					for (int bi = 0; bi < partWidth; bi++)
					{
						string srcNet = ResolveConcatenationPartBit(part, bi, result, ref gateCounter);
						if (srcNet == null) return false;
						string dstNet = result.BitNetName(outputBase, outBit + bi);
						result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { dstNet, srcNet } });
						gateCounter++;
					}
					outBit += partWidth;
				}
				return outBit == width;
			}

			for (int bi = 0; bi < width; bi++)
			{
				string outNet = result.BitNetName(outputBase, bi);
				if (!SynthesizeAssignExpressionBit(expr, outNet, result, ref gateCounter, bi))
					return false;
			}
			return true;
		}

		/// <summary>For concatenation part (ident, ident[n], ident[hi:lo]), returns width. -1 if unknown.</summary>
		static int ParseConcatenationPartWidth(string part, VerilogParseResult result)
		{
			part = part.Trim();
			var rangeMatch = Regex.Match(part, @"^\w+\s*\[\s*(\d+)\s*:\s*(\d+)\s*\]\s*$");
			if (rangeMatch.Success)
			{
				int hi = int.Parse(rangeMatch.Groups[1].Value);
				int lo = int.Parse(rangeMatch.Groups[2].Value);
				return Math.Abs(hi - lo) + 1;
			}
			var bitMatch = Regex.Match(part, @"^\w+\s*\[\s*(\d+)\s*\]\s*$");
			if (bitMatch.Success) return 1;
			var (cw, _) = ParseVerilogConstant(part);
			if (cw >= 0) return cw > 0 ? cw : 1;
			if (part.IndexOf('?') >= 0) return 1; // ternary expression -> 1 bit
			return result.GetWidth(part);
		}

		/// <summary>For concatenation part, returns the net for bit bi (0=LSB of that part). For constants/ternary, synthesizes to temp and returns that net.</summary>
		static string ResolveConcatenationPartBit(string part, int bi, VerilogParseResult result, ref int gateCounter)
		{
			part = part.Trim();
			var rangeMatch = Regex.Match(part, @"^(\w+)\s*\[\s*(\d+)\s*:\s*(\d+)\s*\]\s*$");
			if (rangeMatch.Success)
			{
				string name = rangeMatch.Groups[1].Value;
				int hi = int.Parse(rangeMatch.Groups[2].Value);
				int lo = int.Parse(rangeMatch.Groups[3].Value);
				int srcBit = hi >= lo ? lo + bi : hi + bi;
				return result.BitNetName(name, srcBit);
			}
			var bitMatch = Regex.Match(part, @"^(\w+)\s*\[\s*(\d+)\s*\]\s*$");
			if (bitMatch.Success)
			{
				string name = bitMatch.Groups[1].Value;
				int bit = int.Parse(bitMatch.Groups[2].Value);
				if (bi != 0) return null;
				return result.BitNetName(name, bit);
			}
			var (cw, cv) = ParseVerilogConstant(part);
			if (cw >= 0)
			{
				int val = (cv >> bi) & 1;
				string tmp = AllocTempNet(ref gateCounter);
				SynthesizeAssignExpression(val != 0 ? "1'b1" : "1'b0", tmp, result, ref gateCounter);
				return tmp;
			}
			if (part.IndexOf('?') >= 0 && bi == 0)
			{
				string tmp = AllocTempNet(ref gateCounter);
				if (SynthesizeAssignExpression(part, tmp, result, ref gateCounter))
					return tmp;
			}
			return result.BitNetName(part, bi);
		}

		static string ResolveIdent(string ident, int bitIndex, VerilogParseResult result)
		{
			ident = ident.Trim();
			if (result.GetWidth(ident) <= 1) return ident;
			return result.BitNetName(ident, bitIndex);
		}

		/// <summary>Like SynthesizeAssignExpression but resolves identifiers to bit-level nets.</summary>
		static bool SynthesizeAssignExpressionBit(string expr, string outputNet, VerilogParseResult result, ref int gateCounter, int bitIndex)
		{
			expr = expr.Trim();
			if (string.IsNullOrEmpty(expr)) return false;

			int q = FindTopLevelTernary(expr);
			if (q >= 0)
			{
				string cond = expr.Substring(0, q).Trim();
				int colon = FindMatchingColon(expr, q);
				if (colon < 0) return false;
				string a = expr.Substring(q + 1, colon - q - 1).Trim();
				string b = expr.Substring(colon + 1).Trim();
				return SynthesizeMuxBit(cond, a, b, outputNet, result, ref gateCounter, bitIndex);
			}

			int orIdx = FindTopLevelBinaryOp(expr, '|');
			if (orIdx >= 0)
			{
				string left = expr.Substring(0, orIdx).Trim();
				string right = expr.Substring(orIdx + 1).Trim();
				string xorA = null, xorB = null;
				if (TryMatchXorExpansion(left, right, out xorA, out xorB))
				{
					string xa = ResolveIdentOrBitSelect(xorA, bitIndex, result);
					string xb = ResolveIdentOrBitSelect(xorB, bitIndex, result);
					result.Gates.Add(new VerilogGate { Type = "xor", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, xa, xb } });
					gateCounter++;
					return true;
				}
				string leftNet = AllocTempNet(ref gateCounter);
				string rightNet = AllocTempNet(ref gateCounter);
				if (!SynthesizeAssignExpressionBit(left, leftNet, result, ref gateCounter, bitIndex)) return false;
				if (!SynthesizeAssignExpressionBit(right, rightNet, result, ref gateCounter, bitIndex)) return false;
				result.Gates.Add(new VerilogGate { Type = "or", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, leftNet, rightNet } });
				gateCounter++;
				return true;
			}

			int xorIdx = FindTopLevelBinaryOp(expr, '^');
			if (xorIdx >= 0)
			{
				string left = expr.Substring(0, xorIdx).Trim();
				string right = expr.Substring(xorIdx + 1).Trim();
				string leftNet = AllocTempNet(ref gateCounter);
				string rightNet = AllocTempNet(ref gateCounter);
				if (!SynthesizeAssignExpressionBit(left, leftNet, result, ref gateCounter, bitIndex)) return false;
				if (!SynthesizeAssignExpressionBit(right, rightNet, result, ref gateCounter, bitIndex)) return false;
				result.Gates.Add(new VerilogGate { Type = "xor", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, leftNet, rightNet } });
				gateCounter++;
				return true;
			}

			int andIdx = FindTopLevelBinaryOp(expr, '&');
			if (andIdx >= 0)
			{
				string left = expr.Substring(0, andIdx).Trim();
				string right = expr.Substring(andIdx + 1).Trim();
				string leftNet = AllocTempNet(ref gateCounter);
				string rightNet = AllocTempNet(ref gateCounter);
				if (!SynthesizeAssignExpressionBit(left, leftNet, result, ref gateCounter, bitIndex)) return false;
				if (!SynthesizeAssignExpressionBit(right, rightNet, result, ref gateCounter, bitIndex)) return false;
				result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, leftNet, rightNet } });
				gateCounter++;
				return true;
			}

			if (expr.StartsWith("~") && expr.Length > 1)
			{
				string inner = expr.Substring(1).Trim();
				if (inner.StartsWith("(") && inner.EndsWith(")"))
					inner = inner.Substring(1, inner.Length - 2).Trim();
				string innerNet = IsSimpleIdent(inner) ? ResolveIdentOrBitSelect(inner, bitIndex, result) : AllocTempNet(ref gateCounter);
				if (!IsSimpleIdent(inner) && !SynthesizeAssignExpressionBit(inner, innerNet, result, ref gateCounter, bitIndex))
					return false;
				result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, innerNet } });
				gateCounter++;
				return true;
			}

			if (expr.StartsWith("(") && expr.EndsWith(")"))
				return SynthesizeAssignExpressionBit(expr.Substring(1, expr.Length - 2).Trim(), outputNet, result, ref gateCounter, bitIndex);

			if (expr.Trim() == "0" || Regex.IsMatch(expr, @"1\s*'\s*[bB]\s*0"))
				return SynthesizeAssignExpression("0", outputNet, result, ref gateCounter);

			if (expr.Trim() == "1" || Regex.IsMatch(expr, @"1\s*'\s*[bB]\s*1"))
				return SynthesizeAssignExpression("1", outputNet, result, ref gateCounter);

			if (IsSimpleIdent(expr))
			{
				string src = ResolveIdentOrBitSelect(expr, bitIndex, result);
				result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, src } });
				gateCounter++;
				return true;
			}

			return false;
		}

		static bool SynthesizeMuxBit(string cond, string a, string b, string outputNet, VerilogParseResult result, ref int gateCounter, int bitIndex)
		{
			string condNet = AllocTempNet(ref gateCounter);
			string notCond = AllocTempNet(ref gateCounter);
			// Cond (selector) is typically 1-bit; use non-bit synthesis
			if (!SynthesizeAssignExpression(cond, condNet, result, ref gateCounter)) return false;
			result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notCond, condNet } });
			gateCounter++;
			string t1 = AllocTempNet(ref gateCounter);
			string t2 = AllocTempNet(ref gateCounter);
			if (!SynthesizeAssignExpressionBit(a, t1, result, ref gateCounter, bitIndex)) return false;
			if (!SynthesizeAssignExpressionBit(b, t2, result, ref gateCounter, bitIndex)) return false;
			string aAndNotSel = AllocTempNet(ref gateCounter);
			string bAndSel = AllocTempNet(ref gateCounter);
			result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { aAndNotSel, t1, notCond } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { bAndSel, t2, condNet } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "or", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, aAndNotSel, bAndSel } });
			gateCounter++;
			return true;
		}

		/// <summary>Synthesizes RHS expression to gates. Returns the net name that holds the result. Precedence: | &lt; ^ &lt; &amp; &lt; ~ .</summary>
		static bool SynthesizeAssignExpression(string expr, string outputNet, VerilogParseResult result, ref int gateCounter)
		{
			expr = expr.Trim();
			if (string.IsNullOrEmpty(expr)) return false;

			// Parse == (equality with constant) - produces 1-bit result
			var eqMatch = Regex.Match(expr, @"^(.+?)\s*==\s*(\d+'[bd][\w]+)\s*$", RegexOptions.IgnoreCase);
			if (eqMatch.Success)
			{
				string sel = eqMatch.Groups[1].Value.Trim();
				var (cw, cv) = ParseVerilogConstant(eqMatch.Groups[2].Value.Trim());
				if (cw >= 0 && SynthesizeEqualsConst(sel, cv, cw > 0 ? cw : 1, result, ref gateCounter, out string condNet))
				{
					if (condNet != outputNet)
					{
						result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, condNet } });
						gateCounter++;
					}
					return true;
				}
			}

			// Parse ternary: cond ? a : b  (lowest precedence)
			int q = FindTopLevelTernary(expr);
			if (q >= 0)
			{
				string cond = expr.Substring(0, q).Trim();
				int colon = FindMatchingColon(expr, q);
				if (colon < 0) return false;
				string a = expr.Substring(q + 1, colon - q - 1).Trim();
				string b = expr.Substring(colon + 1).Trim();
				return SynthesizeMux(cond, a, b, outputNet, result, ref gateCounter);
			}

			// Parse + and - (addition, subtraction) - before |
			int pmIdx = FindTopLevelAddOrSub(expr);
			if (pmIdx >= 0)
			{
				char op = expr[pmIdx];
				string left = expr.Substring(0, pmIdx).Trim();
				string right = expr.Substring(pmIdx + 1).Trim();
				return op == '-' ? SynthesizeSub(left, right, outputNet, result, ref gateCounter) : SynthesizeAdd(left, right, outputNet, result, ref gateCounter);
			}

			// Parse << and >> (shifts)
			int shiftIdx; string shiftOp;
			if (FindTopLevelShift(expr, out shiftIdx, out shiftOp) >= 0)
			{
				string left = expr.Substring(0, shiftIdx).Trim();
				string right = expr.Substring(shiftIdx + shiftOp.Length).Trim();
				return SynthesizeShift(left, right, shiftOp, outputNet, result, ref gateCounter);
			}

			// Parse | (OR) - lowest precedence
			int orIdx = FindTopLevelBinaryOp(expr, '|');
			if (orIdx >= 0)
			{
				string left = expr.Substring(0, orIdx).Trim();
				string right = expr.Substring(orIdx + 1).Trim();
				// Optimize (a & ~b) | (~a & b) and (~a & b) | (a & ~b) -> single XOR
				string xorA = null, xorB = null;
				if (TryMatchXorExpansion(left, right, out xorA, out xorB))
				{
					result.Gates.Add(new VerilogGate { Type = "xor", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, xorA, xorB } });
					gateCounter++;
					return true;
				}
				string leftNet = AllocTempNet(ref gateCounter);
				string rightNet = AllocTempNet(ref gateCounter);
				if (!SynthesizeAssignExpression(left, leftNet, result, ref gateCounter)) return false;
				if (!SynthesizeAssignExpression(right, rightNet, result, ref gateCounter)) return false;
				result.Gates.Add(new VerilogGate { Type = "or", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, leftNet, rightNet } });
				gateCounter++;
				return true;
			}

			// Parse ^ (XOR)
			int xorIdx = FindTopLevelBinaryOp(expr, '^');
			if (xorIdx >= 0)
			{
				string left = expr.Substring(0, xorIdx).Trim();
				string right = expr.Substring(xorIdx + 1).Trim();
				string leftNet = AllocTempNet(ref gateCounter);
				string rightNet = AllocTempNet(ref gateCounter);
				if (!SynthesizeAssignExpression(left, leftNet, result, ref gateCounter)) return false;
				if (!SynthesizeAssignExpression(right, rightNet, result, ref gateCounter)) return false;
				result.Gates.Add(new VerilogGate { Type = "xor", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, leftNet, rightNet } });
				gateCounter++;
				return true;
			}

			// Parse & (AND)
			int andIdx = FindTopLevelBinaryOp(expr, '&');
			if (andIdx >= 0)
			{
				string left = expr.Substring(0, andIdx).Trim();
				string right = expr.Substring(andIdx + 1).Trim();
				string leftNet = AllocTempNet(ref gateCounter);
				string rightNet = AllocTempNet(ref gateCounter);
				if (!SynthesizeAssignExpression(left, leftNet, result, ref gateCounter)) return false;
				if (!SynthesizeAssignExpression(right, rightNet, result, ref gateCounter)) return false;
				result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, leftNet, rightNet } });
				gateCounter++;
				return true;
			}

			// Parse ~ (NOT) - unary
			if (expr.StartsWith("~") && expr.Length > 1)
			{
				string inner = expr.Substring(1).Trim();
				// Could be ~(expr) or ~ident
				if (inner.StartsWith("(") && inner.EndsWith(")"))
					inner = inner.Substring(1, inner.Length - 2).Trim();
				string innerNet = IsSimpleIdent(inner) ? ResolveIdentOrBitSelect(inner, 0, result) : AllocTempNet(ref gateCounter);
				if (!IsSimpleIdent(inner) && !SynthesizeAssignExpression(inner, innerNet, result, ref gateCounter))
					return false;
				result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, innerNet } });
				gateCounter++;
				return true;
			}

			// Parentheses: ( expr )
			if (expr.StartsWith("(") && expr.EndsWith(")"))
				return SynthesizeAssignExpression(expr.Substring(1, expr.Length - 2).Trim(), outputNet, result, ref gateCounter);

			// Constant 0: use (x & ~x) to produce 0; x from first input in result
			if (expr.Trim() == "0" || Regex.IsMatch(expr, @"1\s*'\s*[bB]\s*0"))
			{
				string anyNet = result.Inputs.Count > 0 ? result.BitNetName(result.Inputs[0], 0) : (result.Gates.Count > 0 ? result.Gates[result.Gates.Count - 1].Ports[0] : null);
				if (anyNet == null) return false;
				string notX = AllocTempNet(ref gateCounter);
				result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notX, anyNet } });
				gateCounter++;
				result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, anyNet, notX } });
				gateCounter++;
				return true;
			}
			// Constant 1: use ~(x & ~x); x from first input
			if (expr.Trim() == "1" || Regex.IsMatch(expr, @"1\s*'\s*[bB]\s*1"))
			{
				string anyNet = result.Inputs.Count > 0 ? result.BitNetName(result.Inputs[0], 0) : (result.Gates.Count > 0 ? result.Gates[result.Gates.Count - 1].Ports[0] : null);
				if (anyNet == null) return false;
				string andZero = AllocTempNet(ref gateCounter);
				string notX = AllocTempNet(ref gateCounter);
				result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notX, anyNet } });
				gateCounter++;
				result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { andZero, anyNet, notX } });
				gateCounter++;
				result.Gates.Add(new VerilogGate { Type = "nand", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, anyNet, notX } });
				gateCounter++;
				return true;
			}

			// Bit-select identifier (e.g. foo[7]) - connect via buf.
			if (Regex.IsMatch(expr, @"^\w+\s*\[\s*\d+\s*\]\s*$"))
			{
				string src = ResolveIdentOrBitSelect(expr, 0, result);
				result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, src } });
				gateCounter++;
				return true;
			}

			// Simple identifier - connect via buf. Resolve multi-bit identifiers to bit 0 in 1-bit contexts.
			if (IsSimpleIdent(expr))
			{
				string src = ResolveIdentOrBitSelect(expr, 0, result);
				result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, src } });
				gateCounter++;
				return true;
			}

			return false;
		}

		/// <summary>Emits a positive-edge D flip-flop from two D latches (master-slave). Built from D latches (NANDs).</summary>
		static bool SynthesizeDFF(string clk, string data, string outputQ, VerilogParseResult result, ref int gateCounter)
		{
			string notClk = AllocTempNet(ref gateCounter);
			result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notClk, clk } });
			gateCounter++;
			string qMaster = AllocTempNet(ref gateCounter);
			// Master: transparent when CLK=0 (enable=~CLK). Slave: transparent when CLK=1 (enable=CLK).
			if (!SynthesizeDLatch(notClk, data, qMaster, result, ref gateCounter)) return false;
			if (!SynthesizeDLatch(clk, qMaster, outputQ, result, ref gateCounter)) return false;
			return true;
		}

		/// <summary>Emits a D latch (level-sensitive): when Enable=1, Q follows D; when Enable=0, Q holds. Built from NANDs.</summary>
		static bool SynthesizeDLatch(string enable, string data, string outputQ, VerilogParseResult result, ref int gateCounter)
		{
			string notD = AllocTempNet(ref gateCounter);
			string a = AllocTempNet(ref gateCounter);
			string b = AllocTempNet(ref gateCounter);
			string qBar = AllocTempNet(ref gateCounter);
			result.Gates.Add(new VerilogGate { Type = "nand", InstanceId = $"g{gateCounter}", Ports = new List<string> { notD, data, data } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "nand", InstanceId = $"g{gateCounter}", Ports = new List<string> { a, data, enable } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "nand", InstanceId = $"g{gateCounter}", Ports = new List<string> { b, notD, enable } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "nand", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputQ, a, qBar } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "nand", InstanceId = $"g{gateCounter}", Ports = new List<string> { qBar, b, outputQ } });
			gateCounter++;
			return true;
		}

		/// <summary>Multi-bit MUX: for each bit i, next_i = cond ? a_i : b_i. Pre-synthesizes a,b to temp nets so +,- in operands work.</summary>
		static bool SynthesizeMuxMultiBit(string cond, string a, string b, string outputBase, int width, VerilogParseResult result, ref int gateCounter)
		{
			string r1Net = AllocTempNet(ref gateCounter);
			string r2Net = AllocTempNet(ref gateCounter);
			result.SignalWidths[r1Net] = width;
			result.SignalWidths[r2Net] = width;
			if (!SynthesizeAssignExpressionMultiBit(a, r1Net, result, ref gateCounter)) return false;
			if (!SynthesizeAssignExpressionMultiBit(b, r2Net, result, ref gateCounter)) return false;
			for (int bi = 0; bi < width; bi++)
			{
				string aBit = result.BitNetName(r1Net, bi);
				string bBit = result.BitNetName(r2Net, bi);
				string outBit = width > 1 ? result.BitNetName(outputBase, bi) : outputBase;
				// Ternary cond?trueBranch:falseBranch: cond=1 -> a (r1Net), cond=0 -> b (r2Net). SynthesizeMuxBit gives cond=0->first, cond=1->second, so pass (cond, b, a).
				if (!SynthesizeMuxBit(cond, bBit, aBit, outBit, result, ref gateCounter, bi))
					return false;
			}
			return true;
		}

		static bool SynthesizeMux(string cond, string a, string b, string outputNet, VerilogParseResult result, ref int gateCounter)
		{
			string condNet = AllocTempNet(ref gateCounter);
			string notCond = AllocTempNet(ref gateCounter);
			if (!SynthesizeAssignExpression(cond, condNet, result, ref gateCounter)) return false;
			result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notCond, condNet } });
			gateCounter++;
			string t1 = AllocTempNet(ref gateCounter);
			string t2 = AllocTempNet(ref gateCounter);
			if (!SynthesizeAssignExpression(a, t1, result, ref gateCounter)) return false;
			if (!SynthesizeAssignExpression(b, t2, result, ref gateCounter)) return false;
			string aAndNotSel = AllocTempNet(ref gateCounter);
			string bAndSel = AllocTempNet(ref gateCounter);
			result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { aAndNotSel, t1, notCond } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { bAndSel, t2, condNet } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "or", InstanceId = $"g{gateCounter}", Ports = new List<string> { outputNet, aAndNotSel, bAndSel } });
			gateCounter++;
			return true;
		}

		static bool SynthesizeAdd(string left, string right, string outputNet, VerilogParseResult result, ref int gateCounter)
		{
			int w = result.GetWidth(outputNet);
			if (w <= 0) w = Math.Max(result.GetWidth(left), result.GetWidth(right));
			if (w <= 0) w = 8;
			left = SynthesizeSubExprToNet(left, w, result, ref gateCounter);
			right = SynthesizeSubExprToNet(right, w, result, ref gateCounter);
			if (left == null || right == null) return false;
			result.SignalWidths[outputNet] = w;
			return SynthesizeAddSub(left, right, outputNet, result, ref gateCounter, w, subtract: false);
		}

		static bool SynthesizeSub(string left, string right, string outputNet, VerilogParseResult result, ref int gateCounter)
		{
			int w = result.GetWidth(outputNet);
			if (w <= 0) w = Math.Max(result.GetWidth(left), result.GetWidth(right));
			if (w <= 0) w = 8;
			left = SynthesizeSubExprToNet(left, w, result, ref gateCounter);
			right = SynthesizeSubExprToNet(right, w, result, ref gateCounter);
			if (left == null || right == null) return false;
			result.SignalWidths[outputNet] = w;
			return SynthesizeAddSub(left, right, outputNet, result, ref gateCounter, w, subtract: true);
		}

		static string SynthesizeSubExprToNet(string expr, int width, VerilogParseResult result, ref int gateCounter)
		{
			if (IsSimpleIdent(expr) && result.GetWidth(expr) == width) return expr;
			string net = AllocTempNet(ref gateCounter);
			result.SignalWidths[net] = width;
			bool ok = width > 1 ? SynthesizeAssignExpressionMultiBit(expr, net, result, ref gateCounter) : SynthesizeAssignExpression(expr, net, result, ref gateCounter);
			return ok ? net : null;
		}

		static bool SynthesizeAddSub(string left, string right, string outputBase, VerilogParseResult result, ref int gateCounter, int width, bool subtract)
		{
			string carryIn = null;
			for (int bi = 0; bi < width; bi++)
			{
				string aBit = width > 1 ? result.BitNetName(left, bi) : left;
				string bBit = width > 1 ? result.BitNetName(right, bi) : right;
				string outBit = width > 1 ? result.BitNetName(outputBase, bi) : outputBase;
				if (subtract)
				{
					string notB = AllocTempNet(ref gateCounter);
					result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notB, bBit } });
					gateCounter++;
					bBit = notB;
				}
				string cout = bi < width - 1 ? AllocTempNet(ref gateCounter) : null;
				bool needCin1 = (bi == 0 && subtract);
				if (!SynthesizeFullAdder(aBit, bBit, carryIn, outBit, cout, needCin1, result, ref gateCounter))
					return false;
				carryIn = cout;
			}
			return true;
		}

		static bool SynthesizeFullAdder(string a, string b, string carryIn, string sumOut, string coutOut, bool needCin1, VerilogParseResult result, ref int gateCounter)
		{
			string cinNet;
			if (carryIn != null) cinNet = carryIn;
			else if (needCin1)
			{
				string any = result.Inputs.Count > 0 ? result.BitNetName(result.Inputs[0], 0) : null;
				if (any == null) return false;
				cinNet = AllocTempNet(ref gateCounter);
				string notA = AllocTempNet(ref gateCounter);
				result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notA, any } });
				gateCounter++;
				result.Gates.Add(new VerilogGate { Type = "or", InstanceId = $"g{gateCounter}", Ports = new List<string> { cinNet, any, notA } });
				gateCounter++;
			}
			else
			{
				string any = result.Inputs.Count > 0 ? result.BitNetName(result.Inputs[0], 0) : null;
				if (any == null) return false;
				cinNet = AllocTempNet(ref gateCounter);
				string notA = AllocTempNet(ref gateCounter);
				result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notA, any } });
				gateCounter++;
				result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { cinNet, any, notA } });
				gateCounter++;
			}
			string xor1 = AllocTempNet(ref gateCounter);
			result.Gates.Add(new VerilogGate { Type = "xor", InstanceId = $"g{gateCounter}", Ports = new List<string> { xor1, a, b } });
			gateCounter++;
			result.Gates.Add(new VerilogGate { Type = "xor", InstanceId = $"g{gateCounter}", Ports = new List<string> { sumOut, xor1, cinNet } });
			gateCounter++;
			if (coutOut != null)
			{
				string and1 = AllocTempNet(ref gateCounter), and2 = AllocTempNet(ref gateCounter), and3 = AllocTempNet(ref gateCounter);
				result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { and1, a, b } });
				gateCounter++;
				result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { and2, a, cinNet } });
				gateCounter++;
				result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { and3, b, cinNet } });
				gateCounter++;
				string or1 = AllocTempNet(ref gateCounter);
				result.Gates.Add(new VerilogGate { Type = "or", InstanceId = $"g{gateCounter}", Ports = new List<string> { or1, and1, and2 } });
				gateCounter++;
				result.Gates.Add(new VerilogGate { Type = "or", InstanceId = $"g{gateCounter}", Ports = new List<string> { coutOut, or1, and3 } });
				gateCounter++;
			}
			return true;
		}

		static bool SynthesizeShift(string left, string right, string op, string outputBase, VerilogParseResult result, ref int gateCounter)
		{
			right = right.Trim();
			var (_, val) = ParseVerilogConstant(right);
			int amt = val >= 0 ? val : 1;
			int width = result.GetWidth(outputBase);
			if (width <= 0) width = result.GetWidth(left);
			if (width <= 0) width = 8;
			left = SynthesizeSubExprToNet(left, width, result, ref gateCounter);
			if (left == null) return false;
			result.SignalWidths[outputBase] = width;
			string anyForZero = result.Inputs.Count > 0 ? result.BitNetName(result.Inputs[0], 0) : null;
			for (int bi = 0; bi < width; bi++)
			{
				string outBit = result.BitNetName(outputBase, bi);
				int srcBit = op == "<<" ? bi - amt : bi + amt;
				if (srcBit < 0 || srcBit >= width)
				{
					if (anyForZero == null) return false;
					string zero = AllocTempNet(ref gateCounter);
					string notA = AllocTempNet(ref gateCounter);
					result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { notA, anyForZero } });
					gateCounter++;
					result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { zero, anyForZero, notA } });
					gateCounter++;
					result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { outBit, zero } });
					gateCounter++;
				}
				else
				{
					string src = result.BitNetName(left, srcBit);
					result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { outBit, src } });
					gateCounter++;
				}
			}
			return true;
		}

		static int FindTopLevelAddOrSub(string s)
		{
			int depth = 0;
			for (int i = s.Length - 1; i >= 0; i--)
			{
				if (s[i] == ')') depth++;
				else if (s[i] == '(') depth--;
				else if (depth == 0 && (s[i] == '+' || s[i] == '-'))
				{
					if (i > 0 && (s[i - 1] == '+' || s[i - 1] == '-' || s[i - 1] == '=')) continue;
					if (i < s.Length - 1 && s[i + 1] == '>') continue;
					return i;
				}
			}
			return -1;
		}

		static string StripOuterParentheses(string s)
		{
			s = s.Trim();
			while (s.Length >= 2 && s[0] == '(' && s[s.Length - 1] == ')')
			{
				int depth = 0;
				bool wrapsWhole = false;
				for (int i = 0; i < s.Length; i++)
				{
					if (s[i] == '(') depth++;
					else if (s[i] == ')')
					{
						depth--;
						if (depth == 0)
						{
							wrapsWhole = (i == s.Length - 1);
							break;
						}
					}
				}
				if (!wrapsWhole) break;
				s = s.Substring(1, s.Length - 2).Trim();
			}
			return s;
		}

		static int FindTopLevelShift(string expr, out int idx, out string op)
		{
			idx = -1; op = null;
			int depth = 0;
			for (int i = 0; i < expr.Length - 1; i++)
			{
				if (expr[i] == '(') depth++;
				else if (expr[i] == ')') depth--;
				else if (depth == 0 && (expr.Substring(i).StartsWith(">>") || expr.Substring(i).StartsWith("<<")))
				{
					op = expr.Substring(i, 2);
					idx = i;
					return i;
				}
			}
			return -1;
		}

		/// <summary>Parse Verilog constant e.g. 4'b0000, 4'd5, 1'b1, 1. Returns (width, numeric value) or (-1,-1) on fail.</summary>
		static (int width, int value) ParseVerilogConstant(string s)
		{
			s = s.Trim();
			if (int.TryParse(s, out int plainVal) && plainVal >= 0) return (1, plainVal);
			Match m = Regex.Match(s, @"(\d+)\s*'\s*[bB]\s*([01]+)");
			if (m.Success)
			{
				int w = int.Parse(m.Groups[1].Value);
				string bits = m.Groups[2].Value;
				int val = 0;
				foreach (char c in bits) { val = (val << 1) | (c == '1' ? 1 : 0); }
				return (w, val);
			}
			m = Regex.Match(s, @"(\d+)\s*'\s*[dD]\s*(\d+)");
			if (m.Success)
			{
				int w = int.Parse(m.Groups[1].Value);
				int val = int.Parse(m.Groups[2].Value);
				return (w, val);
			}
			return (-1, -1);
		}

		/// <summary>Synthesizes (selector == constant) to a single-bit net. Uses AND of (bit==expected) per bit. Handles ident[n] bit select.</summary>
		static bool SynthesizeEqualsConst(string selector, int constVal, int constWidth, VerilogParseResult result, ref int gateCounter, out string condNet)
		{
			condNet = null;
			selector = selector.Trim();
			// Bit select: ident[n] -> single bit
			var bitSel = Regex.Match(selector, @"^(\w+)\s*\[\s*(\d+)\s*\]\s*$");
			if (bitSel.Success)
			{
				string name = bitSel.Groups[1].Value;
				int bit = int.Parse(bitSel.Groups[2].Value);
				string bitNet = result.BitNetName(name, bit);
				int bitVal = (constVal >> 0) & 1;
				condNet = AllocTempNet(ref gateCounter);
				if (bitVal == 0)
					result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { condNet, bitNet } });
				else
					result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { condNet, bitNet } });
				gateCounter++;
				return condNet != null;
			}
			int width = result.GetWidth(selector);
			if (width <= 0) width = constWidth;
			string andChain = null;
			for (int bi = 0; bi < width; bi++)
			{
				int bitVal = (constVal >> bi) & 1;
				string bitNet = result.BitNetName(selector, bi);
				string bitMatch = AllocTempNet(ref gateCounter);
				if (bitVal == 0)
				{
					result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gateCounter}", Ports = new List<string> { bitMatch, bitNet } });
					gateCounter++;
				}
				else
				{
					// buf to temp so we can AND
					result.Gates.Add(new VerilogGate { Type = "buf", InstanceId = $"g{gateCounter}", Ports = new List<string> { bitMatch, bitNet } });
					gateCounter++;
				}
				if (andChain == null)
					andChain = bitMatch;
				else
				{
					string next = AllocTempNet(ref gateCounter);
					result.Gates.Add(new VerilogGate { Type = "and", InstanceId = $"g{gateCounter}", Ports = new List<string> { next, andChain, bitMatch } });
					gateCounter++;
					andChain = next;
				}
			}
			condNet = andChain;
			return condNet != null;
		}

		/// <summary>Parses case body and synthesizes to MUX tree. Supports: 4'b0000: lhs=rhs; default: lhs=rhs; endcase</summary>
		static bool SynthesizeCaseStatement(string selector, string caseBody, VerilogParseResult result)
		{
			var branches = new List<(string condConst, string lhs, string rhs)>();
			// Match each branch: 4'b0000: or default: followed by assignment(s)
			var branchMatches = Regex.Matches(caseBody, @"(\d+'[bd][\w]+|default)\s*:\s*([\s\S]*?)(?=\d+'[bd][\w]+\s*:|default\s*:|$)", RegexOptions.IgnoreCase);
			foreach (Match bm in branchMatches)
			{
				string label = bm.Groups[1].Value.Trim();
				string content = bm.Groups[2].Value.Trim();
				// Extract first lhs = rhs from content (may have begin...end)
				content = Regex.Replace(content, @"^\s*begin\s*", "", RegexOptions.IgnoreCase);
				content = Regex.Replace(content, @"\s*end\s*$", "", RegexOptions.IgnoreCase);
				// Strip $display(...);
				content = Regex.Replace(content, @"\$display\s*\([^)]*\)\s*;?", " ", RegexOptions.IgnoreCase);
				Match assign = Regex.Match(content, @"(\w+)\s*=\s*([^;]+)\s*;");
				if (!assign.Success) continue;
				string lhs = assign.Groups[1].Value.Trim();
				string rhs = assign.Groups[2].Value.Trim();
				// Skip 8'bXXXXXXXX and similar (default / invalid)
				if (Regex.IsMatch(rhs, @"\d+'[bdxz]\s*[XxZz]+", RegexOptions.IgnoreCase))
					rhs = "0"; // synthesize as 0 for now
				branches.Add((label, lhs, rhs));
			}
			if (branches.Count == 0) return false;

			int gc = result.Gates.Count;
			string lhsFinal = branches[0].lhs;
			int width = result.GetWidth(lhsFinal);

			int defaultIdx = -1;
			for (int i = 0; i < branches.Count; i++)
				if (branches[i].condConst.Equals("default", StringComparison.OrdinalIgnoreCase))
				{ defaultIdx = i; break; }

			string defaultResult = $"_case_def_{gc}";
			result.SignalWidths[defaultResult] = width;
			if (defaultIdx >= 0)
			{
				var (_, _, drhs) = branches[defaultIdx];
				bool ok = width > 1 ? SynthesizeAssignExpressionMultiBit(drhs, defaultResult, result, ref gc) : SynthesizeAssignExpression(drhs, defaultResult, result, ref gc);
				if (!ok) { for (int bi = 0; bi < width; bi++) SynthesizeAssignExpression("0", result.BitNetName(defaultResult, bi), result, ref gc); }
			}
			else
			{
				if (width > 1)
					for (int bi = 0; bi < width; bi++) SynthesizeAssignExpression("0", result.BitNetName(defaultResult, bi), result, ref gc);
				else
					SynthesizeAssignExpression("0", defaultResult, result, ref gc);
			}

			string currentResult = defaultResult;
			for (int i = branches.Count - 1; i >= 0; i--)
			{
				if (i == defaultIdx) continue;
				var (condConst, lhs, rhs) = branches[i];
				if (condConst.Equals("default", StringComparison.OrdinalIgnoreCase)) continue;

				var (cw, cv) = ParseVerilogConstant(condConst);
				if (cw < 0) continue;

				if (!SynthesizeEqualsConst(selector, cv, cw, result, ref gc, out string condNet))
					continue;

				string branchResult = $"_case_br_{gc}_{i}";
				result.SignalWidths[branchResult] = width;
				bool ok = width > 1 ? SynthesizeAssignExpressionMultiBit(rhs, branchResult, result, ref gc) : SynthesizeAssignExpression(rhs, branchResult, result, ref gc);
				if (!ok) continue;

				string muxOut = (i == 0) ? lhs : $"_case_mux_{gc}_{i}";
				if (i != 0) result.SignalWidths[muxOut] = width;

				if (width > 1)
				{
					for (int bi = 0; bi < width; bi++)
					{
						string muxBitOut = (i == 0) ? result.BitNetName(lhs, bi) : result.BitNetName(muxOut, bi);
						string brBit = result.BitNetName(branchResult, bi);
						string curBit = result.BitNetName(currentResult, bi);
						SynthesizeMux(condNet, brBit, curBit, muxBitOut, result, ref gc);
					}
				}
				else
				{
					SynthesizeMux(condNet, branchResult, currentResult, muxOut, result, ref gc);
				}
				currentResult = muxOut;
			}

			if (!result.Outputs.Contains(branches[0].lhs) && !result.Wires.Contains(branches[0].lhs) && !result.Inputs.Contains(branches[0].lhs))
				result.Wires.Add(branches[0].lhs);
			return true;
		}

		static int FindTopLevelBinaryOp(string s, char op)
		{
			int depth = 0;
			for (int i = s.Length - 1; i >= 0; i--)
			{
				if (s[i] == ')') depth++;
				else if (s[i] == '(') depth--;
				else if (depth == 0 && s[i] == op) return i;
			}
			return -1;
		}

		static int FindTopLevelTernary(string s)
		{
			int depth = 0;
			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] == '(') depth++;
				else if (s[i] == ')') depth--;
				else if (depth == 0 && s[i] == '?') return i;
			}
			return -1;
		}

		static int FindMatchingColon(string s, int afterQ)
		{
			int depth = 0;
			for (int i = afterQ + 1; i < s.Length; i++)
			{
				if (s[i] == '(') depth++;
				else if (s[i] == ')') depth--;
				else if (depth == 0 && s[i] == ':') return i;
			}
			return -1;
		}

		static bool IsSimpleIdent(string s)
		{
			if (string.IsNullOrEmpty(s)) return false;
			s = s.Trim();
			if (Regex.IsMatch(s, @"^\w+$")) return true;
			// Bit select: ident[n]
			if (Regex.IsMatch(s, @"^\w+\s*\[\s*\d+\s*\]\s*$")) return true;
			return false;
		}

		/// <summary>Resolve expr to a net. For ident[n] returns that bit; for ident uses bitIndex for multi-bit.</summary>
		static string ResolveIdentOrBitSelect(string expr, int bitIndex, VerilogParseResult result)
		{
			expr = expr.Trim();
			var bitSel = Regex.Match(expr, @"^(\w+)\s*\[\s*(\d+)\s*\]\s*$");
			if (bitSel.Success)
			{
				string name = bitSel.Groups[1].Value;
				int bit = int.Parse(bitSel.Groups[2].Value);
				if (result.GetWidth(name) <= 1) return name;
				return result.BitNetName(name, bit);
			}
			return ResolveIdent(expr, bitIndex, result);
		}

		/// <summary>Matches (a & ~b) | (~a & b) or (~a & b) | (a & ~b) -> XOR(a,b). Returns true and sets a,b.</summary>
		static bool TryMatchXorExpansion(string left, string right, out string a, out string b)
		{
			a = null; b = null;
			left = left.Trim(); right = right.Trim();
			if (!left.StartsWith("(") || !left.EndsWith(")") || !right.StartsWith("(") || !right.EndsWith(")"))
				return false;
			left = left.Substring(1, left.Length - 2).Trim();
			right = right.Substring(1, right.Length - 2).Trim();
			// Match "x & ~y" or "~y & x"
			bool MatchAndNot(string s, out string x, out string notY)
			{
				x = null; notY = null;
				int idx = s.IndexOf('&');
				if (idx < 0) return false;
				string p1 = s.Substring(0, idx).Trim();
				string p2 = s.Substring(idx + 1).Trim();
				if (p1.StartsWith("~") && IsSimpleIdent(p1.Substring(1)))
				{
					notY = p1.Substring(1);
					if (IsSimpleIdent(p2)) { x = p2; return true; }
				}
				else if (p2.StartsWith("~") && IsSimpleIdent(p2.Substring(1)))
				{
					notY = p2.Substring(1);
					if (IsSimpleIdent(p1)) { x = p1; return true; }
				}
				return false;
			}
			// Match "~x & y" or "y & ~x"
			bool MatchNotAnd(string s, out string notX, out string y)
			{
				notX = null; y = null;
				int idx = s.IndexOf('&');
				if (idx < 0) return false;
				string p1 = s.Substring(0, idx).Trim();
				string p2 = s.Substring(idx + 1).Trim();
				if (p1.StartsWith("~") && IsSimpleIdent(p1.Substring(1)))
				{
					notX = p1.Substring(1);
					if (IsSimpleIdent(p2)) { y = p2; return true; }
				}
				else if (p2.StartsWith("~") && IsSimpleIdent(p2.Substring(1)))
				{
					notX = p2.Substring(1);
					if (IsSimpleIdent(p1)) { y = p1; return true; }
				}
				return false;
			}
			string x1, notY1, notX2, y2;
			if (MatchAndNot(left, out x1, out notY1) && MatchNotAnd(right, out notX2, out y2) &&
			    string.Equals(x1, notX2, StringComparison.OrdinalIgnoreCase) &&
			    string.Equals(notY1, y2, StringComparison.OrdinalIgnoreCase))
			{
				a = x1; b = notY1;
				return true;
			}
			if (MatchNotAnd(left, out notX2, out y2) && MatchAndNot(right, out x1, out notY1) &&
			    string.Equals(x1, notX2, StringComparison.OrdinalIgnoreCase) &&
			    string.Equals(notY1, y2, StringComparison.OrdinalIgnoreCase))
			{
				a = x1; b = notY1;
				return true;
			}
			return false;
		}

		/// <summary>Splits block into statements by semicolons (at depth 0, not inside parens/brackets).</summary>
		static List<string> SplitStatements(string block)
		{
			var list = new List<string>();
			int start = 0, depth = 0;
			for (int i = 0; i <= block.Length; i++)
			{
				if (i == block.Length || (depth == 0 && block[i] == ';'))
				{
					string stmt = block.Substring(start, (i == block.Length ? block.Length : i) - start).Trim();
					if (!string.IsNullOrEmpty(stmt)) list.Add(stmt);
					start = i + 1;
				}
				else if (i < block.Length)
				{
					if (block[i] == '(' || block[i] == '[' || block[i] == '{') depth++;
					else if (block[i] == ')' || block[i] == ']' || block[i] == '}') depth--;
				}
			}
			return list;
		}

		/// <summary>Allocates a unique temp net name and increments the counter so each call returns a distinct net.</summary>
		static string AllocTempNet(ref int gateCounter)
		{
			return $"_a{gateCounter++}";
		}

		/// <summary>Unrolls for(i=0;i&lt;N;i=i+1) begin ... end for division-style loops. Returns block with loop replaced by unrolled statements.</summary>
		static string TryUnrollForLoop(string block, VerilogParseResult result)
		{
			var headMatch = Regex.Match(block, @"for\s*\(\s*(\w+)\s*=\s*0\s*;\s*\1\s*<\s*(\d+)\s*;\s*\1\s*=\s*\1\s*\+\s*1\s*\)\s*begin\s*", RegexOptions.IgnoreCase);
			if (!headMatch.Success) return block;
			int bodyStart = headMatch.Index + headMatch.Length;
			int depth = 1;
			int i = bodyStart;
			while (i < block.Length && depth > 0)
			{
				if (block.Substring(i).StartsWith("begin", StringComparison.OrdinalIgnoreCase) && (i == 0 || !char.IsLetterOrDigit(block[i - 1])))
					depth++;
				else if (i + 3 <= block.Length && block.Substring(i, 3).Equals("end", StringComparison.OrdinalIgnoreCase) && (i == 0 || !char.IsLetterOrDigit(block[i - 1])) && (i + 3 >= block.Length || !char.IsLetterOrDigit(block[i + 3])))
				{
					depth--;
					if (depth == 0)
					{
						i += 3; // skip past "end" so "after" starts after the keyword, not "nd result..."
						break;
					}
				}
				i++;
			}
			if (depth != 0) return block;
			string body = block.Substring(bodyStart, i - bodyStart - 3).Trim(); // exclude the "end" from body
			body = Regex.Replace(body, @"/\*[\s\S]*?\*/", " "); // strip comments
			int n = int.Parse(headMatch.Groups[2].Value);
			if (n <= 0 || n > 32) return block;

			int loopEnd = i;
			string before = block.Substring(0, headMatch.Index).Trim();
			string after = block.Substring(loopEnd).Trim();

			// Register versioned names
			// r-stages are 0..n-1; final remainder writes directly to temp.
			for (int k = 0; k < n; k++)
			{
				string rk = $"_r{k}";
				string qk = $"_q{k}";
				if (!result.Wires.Contains(rk)) { result.Wires.Add(rk); result.SignalWidths[rk] = 8; }
				if (!result.Wires.Contains(qk)) { result.Wires.Add(qk); result.SignalWidths[qk] = 8; }
			}
			string qn = $"_q{n}";
			if (!result.Wires.Contains(qn)) { result.Wires.Add(qn); result.SignalWidths[qn] = 8; }
			for (int k = 0; k < n; k++)
			{
				string rs = $"_r_shift{k}";
				string rsub = $"_r_sub{k}";
				if (!result.Wires.Contains(rs)) { result.Wires.Add(rs); result.SignalWidths[rs] = 8; }
				if (!result.Wires.Contains(rsub)) { result.Wires.Add(rsub); result.SignalWidths[rsub] = 8; }
			}

			var sb = new System.Text.StringBuilder();
			sb.Append(before);
			if (before.Length > 0 && !before.EndsWith(";")) sb.Append("; ");
			sb.Append("_r0 = 0; _q0 = dividend; ");
			for (int k = 0; k < n; k++)
			{
				sb.Append($"_r_shift{k} = {{_r{k}[6:0], _q{k}[7]}}; ");
				sb.Append($"_r_sub{k} = _r_shift{k} - divisor_copy; ");
				if (k < n - 1)
					sb.Append($"_r{k + 1} = (_r_sub{k}[7]) ? (_r_sub{k} + divisor_copy) : _r_sub{k}; ");
				else
					sb.Append($"temp = (_r_sub{k}[7]) ? (_r_sub{k} + divisor_copy) : _r_sub{k}; ");
				sb.Append($"_q{k + 1} = {{_q{k}[6:0], (_r_sub{k}[7]) ? 1'b0 : 1'b1}}; ");
			}
			sb.Append("dividend_copy = _q").Append(n).Append("; ");
			sb.Append(after);
			return sb.ToString();
		}

		/// <summary>Parses posedge/negedge always blocks. Supports: if (cond) reg=a; else reg=b; Synthesizes next-state logic + DFFs. Uses library DFF if available, else synthesizes from latches.</summary>
		static bool ParseSequentialAlways(string block, string sens, VerilogParseResult result, ChipLibrary chipLibrary = null)
		{
			bool useNegedge = sens.IndexOf("negedge", StringComparison.OrdinalIgnoreCase) >= 0;
			var clkMatch = Regex.Match(sens, @"(?:posedge|negedge)\s*\(?\s*(\w+)\s*\)?", RegexOptions.IgnoreCase);
			if (!clkMatch.Success) return false;
			string clock = clkMatch.Groups[1].Value.Trim();

			// Match: if ( cond ) lhs = rhs1 ; else lhs = rhs2 ;
			// Cond may be "reset" or "reset == 1'b1"
			Match ifElse = Regex.Match(block, @"if\s*\(\s*([^)]+)\s*\)\s*(\w+)\s*=\s*([^;]+)\s*;\s*else\s*(\w+)\s*=\s*([^;]+)\s*;", RegexOptions.IgnoreCase);
			if (!ifElse.Success) return false;
			string condExpr = ifElse.Groups[1].Value.Trim();
			string lhs = ifElse.Groups[2].Value.Trim();
			string rhs1 = ifElse.Groups[3].Value.Trim();
			string lhs2 = ifElse.Groups[4].Value.Trim();
			string rhs2 = ifElse.Groups[5].Value.Trim();
			if (lhs != lhs2) return false;

			int width = result.GetWidth(lhs);
			if (width <= 0) width = 4;
			result.SignalWidths[lhs] = width;

			int gc = result.Gates.Count;
			string condNet;
			// Parse condition: "reset == 1'b1" or "reset"
			Match condEq = Regex.Match(condExpr, @"(\w+)\s*==\s*(\d+'[bd][\w]+)", RegexOptions.IgnoreCase);
			if (condEq.Success)
			{
				string sel = condEq.Groups[1].Value.Trim();
				var (cw, cv) = ParseVerilogConstant(condEq.Groups[2].Value);
				if (cw < 0) return false;
				if (!SynthesizeEqualsConst(sel, cv, cw > 0 ? cw : 1, result, ref gc, out condNet))
					return false;
			}
			else if (Regex.IsMatch(condExpr, @"^\w+$"))
			{
				condNet = condExpr;
			}
			else
				return false;

			// Synthesize rhs1 and rhs2 to nets first (they may be expressions like "count + 1")
			string r1Net = AllocTempNet(ref gc);
			string r2Net = AllocTempNet(ref gc);
			result.SignalWidths[r1Net] = width;
			result.SignalWidths[r2Net] = width;
			bool ok1 = width > 1 ? SynthesizeAssignExpressionMultiBit(rhs1, r1Net, result, ref gc) : SynthesizeAssignExpression(rhs1, r1Net, result, ref gc);
			bool ok2 = width > 1 ? SynthesizeAssignExpressionMultiBit(rhs2, r2Net, result, ref gc) : SynthesizeAssignExpression(rhs2, r2Net, result, ref gc);
			if (!ok1 || !ok2) return false;
			// Synthesize next_state = cond ? rhs1 : rhs2. Mux uses (a & ~cond)|(b & cond) -> cond=0 picks a, cond=1 picks b.
			// So pass (r2, r1) to get cond=1->rhs1, cond=0->rhs2.
			string nextBase = $"_next_{lhs}_{gc}";
			result.SignalWidths[nextBase] = width;
			if (!SynthesizeMuxMultiBit(condNet, r2Net, r1Net, nextBase, width, result, ref gc))
				return false;

			// Get effective clock (invert for negedge)
			string effClk = clock;
			if (useNegedge)
			{
				string notClk = AllocTempNet(ref gc);
				result.Gates.Add(new VerilogGate { Type = "not", InstanceId = $"g{gc}", Ports = new List<string> { notClk, clock } });
				gc++;
				effClk = notClk;
			}

			// Require DFF chip in library – no built-in, no fallback to gate synthesis
			if (chipLibrary == null || !chipLibrary.TryGetChipDescription("DFF", out _))
			{
				result.ParseError = "DFF chip missing. Import DFF-master-slave.v, save as \"DFF\", then import this circuit.";
				return false;
			}
			for (int bi = 0; bi < width; bi++)
			{
				string qBit = width > 1 ? result.BitNetName(lhs, bi) : lhs;
				string dBit = width > 1 ? result.BitNetName(nextBase, bi) : nextBase;
				result.Gates.Add(new VerilogGate { Type = "dff", InstanceId = $"g{gc}", Ports = new List<string> { qBit, dBit, effClk } });
				gc++;
			}

			if (!result.Outputs.Contains(lhs) && !result.Wires.Contains(lhs) && !result.Inputs.Contains(lhs))
				result.Wires.Add(lhs);
			return true;
		}

		/// <summary>Parses combinational always @(*) or always @(list) blocks. Supports: x=expr; and if(cond)x=a;else x=b;</summary>
		static void ParseCombinationalAlways(string body, VerilogParseResult result, ChipLibrary chipLibrary = null)
		{
			// Do NOT use (\w+)? - it wrongly captures "if" from "if (CLK) C = A;" as a block name, stripping the D-latch pattern
			var alwaysMatches = Regex.Matches(body, @"always\s*@\s*\(([^)]*)\)\s*([\s\S]*?)(?=always|endmodule|$)", RegexOptions.IgnoreCase);
			foreach (Match am in alwaysMatches)
			{
				string sens = am.Groups[1].Value.Trim();
				string block = am.Groups[2].Value.Trim();
				block = Regex.Replace(block, @"^\s*begin\s*", "", RegexOptions.IgnoreCase);
				block = Regex.Replace(block, @"\s*end\s*$", "", RegexOptions.IgnoreCase);
				block = block.Trim();
				// Try to unroll for (i=0; i<N; i=i+1) loops
				block = TryUnrollForLoop(block, result);
				// Sequential: posedge/negedge -> synthesize DFFs + next-state logic
				if (sens.IndexOf("posedge", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    sens.IndexOf("negedge", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					// Sequential synthesis sets ParseError on failure.
					ParseSequentialAlways(block, sens, result, chipLibrary);
					continue;
				}

				// Process statements in order (multiple lhs=rhs; or if/else)
				var statements = SplitStatements(block);
				foreach (string stmt in statements)
				{
					if (string.IsNullOrWhiteSpace(stmt)) continue;
					string s = stmt.Trim();
					if (s.Equals("end", StringComparison.OrdinalIgnoreCase)) continue; // block structure

					// Match: case (selector) 4'b0000: lhs=rhs; 4'b0001: ... default: lhs=rhs; endcase
					Match caseMatch = Regex.Match(s, @"^case\s*\(\s*(\w+)\s*\)\s*([\s\S]*?)\s*endcase\s*$", RegexOptions.IgnoreCase);
					if (caseMatch.Success)
					{
						string selector = caseMatch.Groups[1].Value.Trim();
						string caseBody = caseMatch.Groups[2].Value;
						if (SynthesizeCaseStatement(selector, caseBody, result))
							// Case statement synthesized successfully.
						continue;
					}

					// Match: if ( cond ) lhs = rhs ; else lhs = rhs2 ;
					Match ifElse = Regex.Match(s, @"^if\s*\(\s*([^)]+)\s*\)\s*(\w+)\s*=\s*([^;]+)\s*;\s*else\s*(\w+)\s*=\s*([^;]+)\s*;\s*$", RegexOptions.IgnoreCase);
					if (ifElse.Success)
					{
						string cond = ifElse.Groups[1].Value.Trim();
						string lhs = ifElse.Groups[2].Value.Trim();
						string rhs1 = ifElse.Groups[3].Value.Trim();
						string lhs2 = ifElse.Groups[4].Value.Trim();
						string rhs2 = ifElse.Groups[5].Value.Trim();
						if (lhs == lhs2)
						{
							int gc = result.Gates.Count;
							bool ok = SynthesizeMux(cond, rhs1, rhs2, lhs, result, ref gc);
							if (!ok)
							{
								result.ParseError = $"Failed to synthesize if/else assignment: {lhs} = ({cond}) ? ({rhs1}) : ({rhs2})";
								return;
							}
							if (!result.Outputs.Contains(lhs) && !result.Wires.Contains(lhs) && !result.Inputs.Contains(lhs))
								result.Wires.Add(lhs);
						}
						continue;
					}

					// Match: if ( cond ) lhs = rhs ; (no else) -> D latch (level-sensitive)
					Match ifOnly = Regex.Match(s, @"^if\s*\(\s*(\w+)\s*\)\s*(\w+)\s*=\s*(\w+)\s*;\s*$", RegexOptions.IgnoreCase);
					if (ifOnly.Success)
					{
						string enable = ifOnly.Groups[1].Value.Trim();
						string lhs = ifOnly.Groups[2].Value.Trim();
						string data = ifOnly.Groups[3].Value.Trim();
						int gc = result.Gates.Count;
						if (SynthesizeDLatch(enable, data, lhs, result, ref gc))
						{
							if (!result.Outputs.Contains(lhs) && !result.Wires.Contains(lhs) && !result.Inputs.Contains(lhs))
								result.Wires.Add(lhs);
						}
						continue;
					}

					// Match: lhs = expr ;  (semicolon optional, removed by split)
					Match singleAssign = Regex.Match(s, @"^(\w+)\s*=\s*([^;]+)\s*;?\s*$", RegexOptions.IgnoreCase);
					if (singleAssign.Success)
					{
						string lhs = singleAssign.Groups[1].Value.Trim();
						string rhs = singleAssign.Groups[2].Value.Trim();
						int gc = result.Gates.Count;
						int width = result.GetWidth(lhs);
						if (width <= 0) width = 8;
						result.SignalWidths[lhs] = width;
						bool ok = width > 1 ? SynthesizeAssignExpressionMultiBit(rhs, lhs, result, ref gc) : SynthesizeAssignExpression(rhs, lhs, result, ref gc);
						if (ok)
						{
							if (!result.Outputs.Contains(lhs) && !result.Wires.Contains(lhs) && !result.Inputs.Contains(lhs))
								result.Wires.Add(lhs);
						}
						else
						{
							result.ParseError = $"Failed to synthesize assignment: {lhs} = {rhs}";
							return;
						}
					}
				}
			}
		}
	}
}
