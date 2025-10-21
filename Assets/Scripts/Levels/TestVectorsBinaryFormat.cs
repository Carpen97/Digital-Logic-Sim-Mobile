using System;
using System.IO;
using UnityEngine;

namespace DLS.Levels
{
	/// <summary>
	/// Binary format for test vectors storage.
	/// 
	/// File format (.tvec):
	/// Header (16 bytes):
	///   - Magic: "TVEC" (4 bytes)
	///   - Version: 1 (4 bytes)
	///   - InputBits: N (2 bytes)
	///   - OutputBits: M (2 bytes)
	///   - VectorCount: C (4 bytes)
	/// 
	/// Body (variable):
	///   For each test vector:
	///     - Input bits packed into bytes (⌈N/8⌉ bytes)
	///     - Output bits packed into bytes (⌈M/8⌉ bytes)
	/// </summary>
	public static class TestVectorsBinaryFormat
	{
		private const uint MAGIC = 0x43455654; // "TVEC" in little-endian
		private const int VERSION = 1;
		private const int HEADER_SIZE = 16;

		/// <summary>
		/// Read test vectors from a Resource.
		/// </summary>
		public static LevelDefinition.TestVector[] ReadFromResource(string resourcePath)
		{
			try
			{
				var asset = Resources.Load<TextAsset>(resourcePath);
				if (asset == null)
				{
					Debug.LogError($"[TestVectorsBinary] Resource not found: {resourcePath}");
					return Array.Empty<LevelDefinition.TestVector>();
				}

				using (var stream = new MemoryStream(asset.bytes))
				using (var reader = new BinaryReader(stream))
				{
					return ReadFromStream(reader);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"[TestVectorsBinary] Failed to read resource {resourcePath}: {ex.Message}");
				return Array.Empty<LevelDefinition.TestVector>();
			}
		}

		/// <summary>
		/// Read test vectors from a file.
		/// </summary>
		public static LevelDefinition.TestVector[] ReadFromFile(string filePath)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					Debug.LogError($"[TestVectorsBinary] File not found: {filePath}");
					return Array.Empty<LevelDefinition.TestVector>();
				}

				using (var stream = File.OpenRead(filePath))
				using (var reader = new BinaryReader(stream))
				{
					return ReadFromStream(reader);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"[TestVectorsBinary] Failed to read {filePath}: {ex.Message}");
				return Array.Empty<LevelDefinition.TestVector>();
			}
		}

		private static LevelDefinition.TestVector[] ReadFromStream(BinaryReader reader)
		{
			// Read header
			uint magic = reader.ReadUInt32();
			if (magic != MAGIC)
			{
				throw new Exception($"Invalid magic number. Expected {MAGIC:X8}, got {magic:X8}");
			}

			int version = reader.ReadInt32();
			if (version != VERSION)
			{
				throw new Exception($"Unsupported version {version}. Expected {VERSION}");
			}

			ushort inputBits = reader.ReadUInt16();
			ushort outputBits = reader.ReadUInt16();
			int vectorCount = reader.ReadInt32();

			Debug.Log($"[TestVectorsBinary] Reading {vectorCount} vectors ({inputBits} input bits, {outputBits} output bits)");

			// Calculate bytes needed per vector
			int inputBytes = (inputBits + 7) / 8;
			int outputBytes = (outputBits + 7) / 8;

			// Read test vectors
			var vectors = new LevelDefinition.TestVector[vectorCount];
			
			for (int i = 0; i < vectorCount; i++)
			{
				byte[] inputData = reader.ReadBytes(inputBytes);
				string inputs = UnpackBits(inputData, inputBits);

				byte[] outputData = reader.ReadBytes(outputBytes);
				string expected = UnpackBits(outputData, outputBits);

				vectors[i] = new LevelDefinition.TestVector
				{
					inputs = inputs,
					expected = expected,
					settleSteps = 0,
					isClockEdge = false
				};
			}

			return vectors;
		}

		/// <summary>
		/// Write test vectors to a file.
		/// </summary>
		public static void WriteToFile(string filePath, LevelDefinition.TestVector[] vectors, int inputBits, int outputBits)
		{
			try
			{
				string directory = Path.GetDirectoryName(filePath);
				if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				using (var stream = File.Create(filePath))
				using (var writer = new BinaryWriter(stream))
				{
					WriteToStream(writer, vectors, inputBits, outputBits);
				}

				Debug.Log($"[TestVectorsBinary] Wrote {vectors.Length} vectors to {filePath} ({new FileInfo(filePath).Length} bytes)");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[TestVectorsBinary] Failed to write {filePath}: {ex.Message}");
				throw;
			}
		}

		private static void WriteToStream(BinaryWriter writer, LevelDefinition.TestVector[] vectors, int inputBits, int outputBits)
		{
			// Write header
			writer.Write(MAGIC);
			writer.Write(VERSION);
			writer.Write((ushort)inputBits);
			writer.Write((ushort)outputBits);
			writer.Write(vectors.Length);

			// Calculate bytes needed per vector
			int inputBytes = (inputBits + 7) / 8;
			int outputBytes = (outputBits + 7) / 8;

			// Write test vectors
			foreach (var vector in vectors)
			{
				byte[] inputData = PackBits(vector.inputs, inputBytes);
				writer.Write(inputData);

				byte[] outputData = PackBits(vector.expected, outputBytes);
				writer.Write(outputData);
			}
		}

		private static byte[] PackBits(string bits, int byteCount)
		{
			byte[] data = new byte[byteCount];
			
			for (int i = 0; i < bits.Length; i++)
			{
				if (bits[i] == '1')
				{
					int byteIndex = i / 8;
					int bitIndex = 7 - (i % 8);
					data[byteIndex] |= (byte)(1 << bitIndex);
				}
			}
			
			return data;
		}

		private static string UnpackBits(byte[] data, int bitCount)
		{
			char[] bits = new char[bitCount];
			
			for (int i = 0; i < bitCount; i++)
			{
				int byteIndex = i / 8;
				int bitIndex = 7 - (i % 8);
				bits[i] = ((data[byteIndex] >> bitIndex) & 1) != 0 ? '1' : '0';
			}
			
			return new string(bits);
		}

		public static long EstimateFileSize(int vectorCount, int inputBits, int outputBits)
		{
			int inputBytes = (inputBits + 7) / 8;
			int outputBytes = (outputBits + 7) / 8;
			return HEADER_SIZE + (long)vectorCount * (inputBytes + outputBytes);
		}
	}
}

