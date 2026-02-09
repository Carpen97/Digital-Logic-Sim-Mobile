using System;
using DLS.Simulation;

public class AudioState
{
	public enum WaveType
	{
		Sin = 0,
		Square = 1,
		Saw = 2,
		Triangle = 3,
		Pulse = 4,           // Variable duty cycle square wave
		InverseSaw = 5,      // Falling sawtooth
		Noise = 6,           // White noise
		Reserved = 7         // Future expansion
	}

	const WaveType defaultWaveType = WaveType.Square;
	const int waveIterations = 20;

	// RMS normalization factors to make all waveforms sound equally loud
	// More conservative values to avoid clipping/distortion
	static readonly float[] waveNormalizationFactors = new float[]
	{
		1.0f,    // Sin - keep at 1.0x (pure tone reference)
		0.85f,   // Square - reduce slightly (harsh overtones)
		0.95f,   // Saw - slight reduction (many harmonics)
		1.0f,    // Triangle - keep at 1.0x (soft waveform)
		0.85f,   // Pulse - same as square (varies with duty)
		0.95f,   // InverseSaw - same as saw
		0.4f,    // Noise - reduce significantly (very harsh)
		1.0f     // Reserved
	};

	public readonly SimAudio simAudio = new();
	
	// Phase accumulator for SpeakerV2 to maintain continuity
	private double speakerV2PhaseAccumulator = 0;
	private double lastSampleTime = 0;

	public float Sample(double time)
	{
		float sum = 0;

		// Sample SpeakerV2 (sine wave, direct frequency with phase continuity)
		if (simAudio.speakerV2CurrentAmplitude > 0.001)
		{
			// Calculate time delta since last sample
			double deltaTime = time - lastSampleTime;
			if (deltaTime < 0) deltaTime = 0; // Handle wrap-around
			
			// Accumulate phase to maintain continuity
			speakerV2PhaseAccumulator += 2 * Math.PI * simAudio.speakerV2CurrentFrequency * deltaTime;
			
			// Wrap phase to avoid overflow (keep it in reasonable range)
			if (speakerV2PhaseAccumulator > 1000000)
			{
				speakerV2PhaseAccumulator = speakerV2PhaseAccumulator % (2 * Math.PI);
			}
			
			sum += SinWave(speakerV2PhaseAccumulator) * (float)simAudio.speakerV2CurrentAmplitude;
		}
		else
		{
			// Reset phase when silent to avoid accumulation
			speakerV2PhaseAccumulator = 0;
		}
		
		lastSampleTime = time;

		// Sample other speakers (Buzzer, Speaker V1)
		for (int i = 0; i < simAudio.freqsAll.Length; i++)
		{
			float amplitude = (float)simAudio.targetAmplitudesPerFreq[i];
			if (amplitude < 0.001f) continue;

			double phase = time * 2 * Math.PI * simAudio.freqsAll[i];
			
		// Use per-frequency wave type if specified, otherwise use default
		int waveTypeIndex = simAudio.waveTypePerFreq[i];
		WaveType waveTypeToUse = waveTypeIndex >= 0 ? (WaveType)waveTypeIndex : defaultWaveType;
		
		// Get pulse width for this frequency (used for pulse wave)
		float pulseWidth = simAudio.pulseWidthPerFreq[i];
		
		// Apply waveform and normalization
		float normFactor = waveNormalizationFactors[(int)waveTypeToUse];
		sum += Wave(phase, waveTypeToUse, pulseWidth) * amplitude * normFactor;
		}

		/*
		if (UnityMain.instance.useRef)
		{
			sum += Wave(time * 2 * Math.PI * UnityMain.instance.refNoteFreq) * 1;
		}
		else
		{
			float nt = UnityMain.instance.noteIndex / 255f;
			float omnt = 1 - nt;
			//UnityMain.instance.perceptualGain = Maths.EaseCubeInOut(Mathf.Pow(omnt, 1.1f)) + 1;
			UnityMain.instance.perceptualGain = Mathf.Lerp(2,0.5f, Maths.EaseQuadInOut(nt));
			sum += Wave(time * 2 * Math.PI * UnityMain.instance.noteFreq) * UnityMain.instance.perceptualGain;
		}
		*/

		return sum;
	}

	static float Wave(double phase, WaveType waveType)
	{
		return Wave(phase, waveType, 0.5f);
	}

	static float Wave(double phase, WaveType waveType, float pulseWidth)
	{
		return waveType switch
		{
			WaveType.Sin => SinWave(phase),
			WaveType.Square => SquareWave(phase, waveIterations),
			WaveType.Saw => SawtoothWave(phase, waveIterations),
			WaveType.Triangle => TriangleWave(phase),
			WaveType.Pulse => PulseWave(phase, pulseWidth),
			WaveType.InverseSaw => InverseSawtoothWave(phase, waveIterations),
			WaveType.Noise => NoiseWave(phase),
			WaveType.Reserved => 0f,
			_ => throw new NotImplementedException()
		};
	}

	static float SinWave(double phase)
	{
		return (float)Math.Sin(phase);
	}

	static float SawtoothWave(double t, int numIterations = 20)
	{
		double sum = 0;
		for (int i = 1; i <= numIterations; i++)
		{
			double numerator = Math.Sin(2 * i * t);
			double denominator = i;
			sum += numerator / denominator;
		}

		return (float)(sum * 4 / MathF.PI);
	}

	static float SquareWave(double t, int numIterations = 20)
	{
		double sum = 0;
		for (int i = 1; i <= numIterations; i++)
		{
			double numerator = Math.Sin((2 * i - 1) * t);
			double denominator = 2 * i - 1;
			sum += numerator / denominator;
		}

		return (float)(sum * 4 / MathF.PI);
	}

	static float TriangleWave(double phase)
	{
		// Normalize phase to 0-1 range
		double t = (phase / (2 * Math.PI)) % 1.0;
		if (t < 0) t += 1.0;
		
		// Triangle wave: rises from -1 to 1 in first half, falls from 1 to -1 in second half
		if (t < 0.5)
			return (float)(4 * t - 1); // -1 to 1
		else
			return (float)(3 - 4 * t); // 1 to -1
	}

	static float PulseWave(double phase, float dutyCycle)
	{
		// Normalize phase to 0-1 range
		double t = (phase / (2 * Math.PI)) % 1.0;
		if (t < 0) t += 1.0;
		
		// Clamp duty cycle to valid range
		dutyCycle = Math.Clamp(dutyCycle, 0.05f, 0.95f);
		
		// Use band-limited pulse wave (Fourier series) to reduce aliasing
		// For now, simple square wave when duty ≈ 0.5, harder edges otherwise
		if (Math.Abs(dutyCycle - 0.5) < 0.1)
		{
			// Near 50% duty cycle, use smooth square wave
			return SquareWave(phase, 15);
		}
		else
		{
			// Other duty cycles: hard pulse (may alias, but characteristic)
			return t < dutyCycle ? 1f : -1f;
		}
	}

	static float InverseSawtoothWave(double t, int numIterations = 20)
	{
		// Inverse sawtooth (falling) is just negative of regular sawtooth
		return -SawtoothWave(t, numIterations);
	}

	// Simple pseudo-random noise generator
	static float NoiseWave(double phase)
	{
		// Use phase as seed for deterministic noise (otherwise it would pop)
		// This creates a repeating noise pattern, but at audio rates it sounds continuous
		uint seed = (uint)(phase * 12345.6789);
		seed = seed * 1103515245 + 12345;
		float noise = ((seed / 65536) % 32768) / 16384.0f - 1.0f;
		return noise;
	}
}