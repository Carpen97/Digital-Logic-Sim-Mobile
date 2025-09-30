using System;
using UnityEngine;
using DLS.Game;
using DLS.Levels;
using DLS.Levels.Host;

namespace DLS.Game.LevelsIntegration
{
    /// <summary>
    /// Provides hint colors for pin handles based on expected vs actual output.
    /// Uses level validation logic to determine correct outputs.
    /// </summary>
    public static class HintSystem
    {
        /// <summary>
        /// Global toggle for the hint tool. When false, all handles use default colors.
        /// </summary>
        public static bool EnableHintTool => MobileUIController.Instance != null && MobileUIController.Instance.isHintToolActive;

        // Performance optimization: cache the last calculated input state
        private static ulong _cachedInputState = 0;
        private static int _cachedInputCount = 0;
        private static bool _cachedResult = false;
        private static Color _cachedColor = Color.grey;

        /// <summary>
        /// Gets the appropriate color for an output pin handle based on expected output.
        /// Returns green if current output matches expected, grey otherwise.
        /// </summary>
        public static Color GetPinHandleColor(DevPinInstance outputPin, LevelDefinition level)
        {
            // Early exit if hints disabled or no active level
            if (!EnableHintTool || !LevelManager.Instance.IsActive || level == null)
                return GetDefaultHandleColor();

            // Only process output pins
            if (outputPin.IsInputPin)
                return GetDefaultHandleColor();

            // Performance optimization: check if we can use cached result
            var currentInputState = GetCurrentInputState();
            if (currentInputState == _cachedInputState && _cachedInputCount == GetInputCount())
            {
                return _cachedColor;
            }

            // Calculate expected output for current input state
            var expectedOutput = CalculateExpectedOutput(currentInputState, level);

            // Update cache
            _cachedInputState = currentInputState;
            _cachedInputCount = GetInputCount();
            _cachedResult = expectedOutput;
            _cachedColor = expectedOutput ? Color.green : GetDefaultHandleColor();

            return _cachedColor;
        }

        /// <summary>
        /// Gets the current input state as a bit vector for comparison with test vectors.
        /// </summary>
        private static ulong GetCurrentInputState()
        {
            var project = Project.ActiveProject;
            if (project?.ViewedChip == null) return 0;

            ulong inputState = 0;
            int bitIndex = 0;
            
            foreach (var inputPin in project.ViewedChip.GetInputPins())
            {
                if (inputPin is DevPinInstance devPin)
                {
                    // Use PlayerInputState to get what user has set
                    bool isHigh = devPin.Pin.PlayerInputState.FirstBitHigh();
                    if (isHigh)
                        inputState |= (1UL << bitIndex);
                    bitIndex++;
                }
            }

            return inputState;
        }

        /// <summary>
        /// Gets the count of input pins for cache validation.
        /// </summary>
        private static int GetInputCount()
        {
            var project = Project.ActiveProject;
            if (project?.ViewedChip == null) return 0;

            int count = 0;
            foreach (var inputPin in project.ViewedChip.GetInputPins())
            {
                if (inputPin is DevPinInstance) count++;
            }
            return count;
        }

        /// <summary>
        /// Gets the current output state of the given output pin.
        /// </summary>
        private static bool GetCurrentOutput(DevPinInstance outputPin)
        {
            return outputPin.Pin.State.FirstBitHigh();
        }

        /// <summary>
        /// Calculates the expected output for the given input state using level test vectors.
        /// </summary>
        private static bool CalculateExpectedOutput(ulong inputState, LevelDefinition level)
        {
            if (level.testVectors == null || level.testVectors.Length == 0)
                return false; // No test vectors, assume should be low

            // Convert input state to string format for comparison
            string inputString = ConvertInputStateToString(inputState, GetInputCount());

            // Find matching test vector
            foreach (var testVector in level.testVectors)
            {
                if (testVector.inputs == inputString)
                {
                    // Parse expected output - assume single bit for now
                    return testVector.expected == "1";
                }
            }

            // No matching test vector found, assume should be low
            return false;
        }

        /// <summary>
        /// Converts input state to string format used in test vectors.
        /// </summary>
        private static string ConvertInputStateToString(ulong inputState, int inputCount)
        {
            char[] chars = new char[inputCount];
            for (int i = 0; i < inputCount; i++)
            {
                chars[i] = ((inputState & (1UL << i)) != 0) ? '1' : '0';
            }
            
            // Reverse to match test vector format (MSB first)
            Array.Reverse(chars);
            return new string(chars);
        }

        /// <summary>
        /// Gets the default handle color from the current theme.
        /// </summary>
        private static Color GetDefaultHandleColor()
        {
            return Graphics.DrawSettings.ActiveTheme.DevPinHandle;
        }

        /// <summary>
        /// Clears the cache when level changes or inputs are modified externally.
        /// </summary>
        public static void ClearCache()
        {
            _cachedInputState = 0;
            _cachedInputCount = 0;
            _cachedResult = false;
            _cachedColor = Color.grey;
        }
    }
}
