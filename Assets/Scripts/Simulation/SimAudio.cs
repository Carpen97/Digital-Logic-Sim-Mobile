using System;
using Seb.Helpers;
using UnityEngine;

namespace DLS.Simulation
{
	public class SimAudio
	{
		public const int freqCount = 256;

		public readonly float[] freqsAll = new float[freqCount];
		readonly double[] targetAmplitudesPerFreq_temp = new double[freqCount];
		public readonly double[] targetAmplitudesPerFreq = new double[freqCount];
		// Very crude correction factors to make different frequencies sound more equal in volume
		// (boosts amplitude of low frequencies)
		readonly float[] perceptualGainCorrection = new float[freqCount];
		
	// Wave type per frequency (defaults to -1 for no override, uses AudioState default)
	readonly int[] waveTypePerFreq_temp = new int[freqCount];
	public readonly int[] waveTypePerFreq = new int[freqCount];
	
	// Pulse width per frequency (0.0 to 1.0, default 0.5 for 50% duty cycle)
	readonly float[] pulseWidthPerFreq_temp = new float[freqCount];
	public readonly float[] pulseWidthPerFreq = new float[freqCount];
	
	// ---- State ----
	bool hasInputSinceLastInit;
	bool isSmoothing;

		public SimAudio()
		{
			for (int i = 0; i < freqsAll.Length; i++)
			{
				freqsAll[i] = CalculateFrequency(i / 3.0);
				float freqT = i / 255f;
				perceptualGainCorrection[i] = Maths.Lerp(2, 0.35f, Maths.EaseQuadInOut(freqT));
			}
		}

	public void InitFrame()
	{
		// Store whether we had input last frame before resetting
		bool hadSpeakerV2InputLastFrame = hasSpeakerV2Input;
		
		if (!hasInputSinceLastInit && !hadSpeakerV2InputLastFrame) return;
		
		hasInputSinceLastInit = false;
		hasSpeakerV2Input = false;  // Reset for this frame

		for (int i = 0; i < targetAmplitudesPerFreq_temp.Length; i++)
		{
			targetAmplitudesPerFreq_temp[i] = 0;
			waveTypePerFreq_temp[i] = -1; // Reset to no override
			pulseWidthPerFreq_temp[i] = 0.5f; // Reset to 50% duty cycle
		}
		
		// Note: speakerV2TargetAmplitude is NOT reset here
		// It will be set by RegisterTone() if input comes this frame
		// If no input comes, it stays at previous value and will fade if needed
	}

		public void RegisterNote(int index, uint volume)
		{
			if (volume == 0) return;

			hasInputSinceLastInit = true;
			float amplitudeT = MathF.Min(volume / 15f, 1);
			targetAmplitudesPerFreq_temp[index] += amplitudeT * perceptualGainCorrection[index];
		}

	public void RegisterNote(int index, uint volume, int waveType)
	{
		if (volume == 0) return;

		hasInputSinceLastInit = true;
		float amplitudeT = MathF.Min(volume / 255f, 1);
		targetAmplitudesPerFreq_temp[index] += amplitudeT * perceptualGainCorrection[index];
		waveTypePerFreq_temp[index] = waveType; // Store wave type for this frequency
	}

	public void RegisterNote(int index, uint volume, int waveType, float pulseWidth)
	{
		if (volume == 0) return;

		hasInputSinceLastInit = true;
		float amplitudeT = MathF.Min(volume / 255f, 1);
		targetAmplitudesPerFreq_temp[index] += amplitudeT * perceptualGainCorrection[index];
		waveTypePerFreq_temp[index] = waveType; // Store wave type for this frequency
		pulseWidthPerFreq_temp[index] = pulseWidth; // Store pulse width for this frequency
	}

	// For SpeakerV2: Register tone by direct frequency
	public float speakerV2TargetFrequency = 0;
	public double speakerV2TargetAmplitude = 0;
	public float speakerV2CurrentFrequency = 0;
	public double speakerV2CurrentAmplitude = 0;
	public bool hasSpeakerV2Input = false;

	public void RegisterTone(float frequency, uint volume)
	{
		hasSpeakerV2Input = true;
		
		if (volume == 0)
		{
			speakerV2TargetAmplitude = 0;
			// Keep previous frequency to avoid glitches
		}
		else
		{
			speakerV2TargetFrequency = frequency;
			speakerV2TargetAmplitude = volume / 255.0;
		}
	}
	
	public void SmoothSpeakerV2(double deltaTime)
	{
		// Smooth frequency and amplitude changes to avoid clicks and pops
		const float smoothSpeed = 20f;
		double step = Math.Min(1, deltaTime * smoothSpeed);
		
		// Smooth frequency
		double freqDelta = speakerV2TargetFrequency - speakerV2CurrentFrequency;
		speakerV2CurrentFrequency += (float)(freqDelta * step);
		
		// Smooth amplitude
		double ampDelta = speakerV2TargetAmplitude - speakerV2CurrentAmplitude;
		speakerV2CurrentAmplitude += ampDelta * step;
		
		// If very close to target, snap to it
		if (Math.Abs(freqDelta) < 0.1) speakerV2CurrentFrequency = speakerV2TargetFrequency;
		if (Math.Abs(ampDelta) < 0.001) speakerV2CurrentAmplitude = speakerV2TargetAmplitude;
	}

	public void NotifyAllNotesRegistered(double deltaTime)
	{
		if (!hasInputSinceLastInit && !isSmoothing && !hasSpeakerV2Input && speakerV2CurrentAmplitude < 0.001) return;

		const float smoothSpeed = 30f;
		double step = Math.Min(1, deltaTime * smoothSpeed);
		isSmoothing = false;

		// Smooth SpeakerV2 parameters
		SmoothSpeakerV2(deltaTime);
		isSmoothing |= speakerV2CurrentAmplitude > 0.001;

		for (int i = 0; i < targetAmplitudesPerFreq.Length; i++)
		{
			// Crude smoothing to avoid jarring frequency jumps
			double curr = targetAmplitudesPerFreq[i];
			double target = targetAmplitudesPerFreq_temp[i];
			double delta = target - curr;
			double valNew = curr + delta * step;
			double error = Math.Abs(valNew - target);

			if (error <= 0.0001) valNew = target;
			targetAmplitudesPerFreq[i] = valNew;

			// Update wave type and pulse width immediately (no smoothing needed for discrete values)
			waveTypePerFreq[i] = waveTypePerFreq_temp[i];
			pulseWidthPerFreq[i] = pulseWidthPerFreq_temp[i];

			isSmoothing |= valNew > 0;
		}
	}


		public static float CalculateFrequency(double numAboveA0)
		{
			const double A0Frequency = 27.5;
			return (float)(A0Frequency * Math.Pow(1.059463094359, numAboveA0));
		}
	}
}