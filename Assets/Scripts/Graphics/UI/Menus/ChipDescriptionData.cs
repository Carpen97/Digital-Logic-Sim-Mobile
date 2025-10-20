using System.Collections.Generic;
using DLS.Description;

namespace DLS.Graphics
{
	/// <summary>
	/// Contains educational descriptions for all built-in chip types
	/// </summary>
	public static class ChipDescriptionData
	{
		private static readonly Dictionary<ChipType, string> chipDescriptions = new()
		{
			// ---- Basic Chips ----
			[ChipType.Nand] = @"NAND Gate

What it is: A NAND gate outputs 0 only when ALL inputs are 1. It's the inverse of an AND gate.

How it works: NAND stands for 'NOT AND'. It performs the AND operation on all inputs, then inverts the result. If any input is 0, the output is 1. Only when all inputs are 1 does it output 0.

When to use: NAND gates are universal - you can build any other logic gate using only NAND gates. They're commonly used for:
• Creating AND gates (connect NAND output to NOT gate)
• Building complex logic circuits
• Memory circuits and flip-flops
• Error detection and correction

Example: A 2-input NAND gate with inputs A=1, B=0 outputs 1. With A=1, B=1 it outputs 0.

Tips: NAND gates are often preferred in digital design because they're faster and use fewer transistors than other gates.",

			[ChipType.TriStateBuffer] = @"Tri-State Buffer

What it is: A tri-state buffer can output 0, 1, or a high-impedance (disconnected) state based on an enable signal.

How it works: When the enable input is 1, the buffer acts like a normal buffer - output equals input. When enable is 0, the output becomes high-impedance (floating), effectively disconnecting the output from the circuit.

When to use: Tri-state buffers are essential for:
• Bus systems where multiple devices share the same wires
• Data multiplexing and routing
• Creating bidirectional communication
• Preventing signal conflicts on shared lines

Example: In a computer's data bus, multiple chips can share the same wires by using tri-state buffers. Only the enabled chip drives the bus while others remain disconnected.

Tips: Never connect multiple tri-state outputs together without proper enable control - this can cause short circuits and damage components.",

			[ChipType.Clock] = @"Clock Generator

What it is: A clock generator produces a regular, periodic signal that alternates between 0 and 1 at a fixed frequency.

How it works: The clock continuously toggles its output at regular intervals. The frequency determines how fast the clock oscillates - higher frequency means faster switching between 0 and 1.

When to use: Clock signals are fundamental for:
• Synchronizing digital circuits
• Creating timers and counters
• Sequential logic circuits (flip-flops, registers)
• CPU operations and data processing
• Creating delays and timing sequences

Example: A 1Hz clock toggles once per second, while a 1MHz clock toggles one million times per second.

Tips: Clock signals should be clean and stable. Avoid connecting multiple clock sources to the same circuit unless you're creating a clock multiplexer.",

			[ChipType.Pulse] = @"Pulse Generator

What it is: A pulse generator creates a brief pulse (short burst) of 1 followed by 0, triggered by an input signal.

How it works: When the trigger input changes from 0 to 1, the pulse generator outputs a short pulse of 1, then returns to 0. The pulse duration is typically very brief compared to the trigger signal.

When to use: Pulse generators are useful for:
• Creating edge-triggered events
• Resetting counters and flip-flops
• Generating control signals
• Debouncing noisy input signals
• Creating one-shot timers

Example: Connecting a pulse generator to a counter's reset input will clear the counter each time the trigger goes high.

Tips: Pulse generators are great for creating clean, brief control signals from longer input signals.",

			[ChipType.Detector] = @"Edge Detector

What it is: An edge detector outputs a brief pulse when it detects a change (edge) in the input signal from 0 to 1 or 1 to 0.

How it works: The detector monitors the input signal and compares it to the previous state. When it detects a transition (rising or falling edge), it outputs a short pulse to indicate the change occurred.

When to use: Edge detectors are essential for:
• Synchronizing circuits to signal changes
• Creating event-driven logic
• Detecting button presses and switches
• Building state machines
• Creating interrupt signals

Example: An edge detector can convert a button press (which stays high while pressed) into a brief pulse that triggers a counter increment.

Tips: Edge detectors help convert level-sensitive signals into edge-sensitive events, which is crucial for many digital circuits.",

			// ---- Memory ----
			[ChipType.dev_Ram_8Bit] = @"8-Bit RAM

What it is: Random Access Memory (RAM) that can store and retrieve 8-bit data values at specific addresses.

How it works: RAM has address inputs to select which memory location to access, data inputs/outputs for reading and writing, and control signals (read/write enable). You can store 8-bit values at different addresses and retrieve them later.

When to use: RAM is essential for:
• Storing temporary data in processors
• Creating lookup tables
• Building caches and buffers
• Implementing data structures
• Storing program variables

Example: Store the value 42 at address 5, then later read address 5 to get back 42.

Tips: RAM loses its data when power is removed. Use EEPROM or ROM for permanent storage.",

			[ChipType.Rom_256x16] = @"256×16 ROM

What it is: Read-Only Memory that stores 256 different 16-bit values. The data is permanent and cannot be changed during operation.

How it works: ROM has address inputs (8 bits to select from 256 locations) and outputs the stored 16-bit value at that address. The data is programmed into the ROM and remains fixed.

When to use: ROM is perfect for:
• Storing program instructions
• Creating lookup tables for functions
• Storing constants and data tables
• Implementing microcode
• Storing character fonts and graphics

Example: A ROM could store a sine wave lookup table where address 0 outputs 0, address 64 outputs the maximum value, etc.

Tips: ROM data is permanent and reliable. Use it for any data that doesn't need to change during operation.",

			[ChipType.EEPROM_256x16] = @"256×16 EEPROM

What it is: Electrically Erasable Programmable Read-Only Memory that can be reprogrammed while in the circuit.

How it works: Like ROM, EEPROM stores 256 different 16-bit values. Unlike ROM, EEPROM can be erased and reprogrammed using special write operations, but retains data when power is removed.

When to use: EEPROM is ideal for:
• Storing user settings and preferences
• Creating configurable lookup tables
• Storing calibration data
• Implementing non-volatile counters
• Storing small amounts of persistent data

Example: Store user preferences that persist between power cycles, or create a configurable function generator.

Tips: EEPROM has limited write cycles (typically 100,000+), so avoid frequent writes to the same location.",

			// ---- Displays ----
			[ChipType.SevenSegmentDisplay] = @"7-Segment Display

What it is: A display that shows decimal digits (0-9) using 7 individual segments that can be lit or unlit.

How it works: The display has 7 inputs (one for each segment: a, b, c, d, e, f, g). Each input controls whether that segment is lit (1) or dark (0). Different combinations create different digits.

When to use: 7-segment displays are perfect for:
• Showing numbers and some letters
• Digital clocks and timers
• Score displays and counters
• Calculator outputs
• Status indicators

Example: To display '8', all segments (a through g) should be lit. To display '1', only segments b and c should be lit.

Tips: You can also display some letters like 'A', 'b', 'C', 'd', 'E', 'F' by lighting the appropriate segments.",

			[ChipType.DisplayRGB] = @"RGB Display

What it is: A color display that shows a 16×16 grid of colored pixels, where each pixel can display different colors.

How it works: The display takes color data (typically red, green, blue values) and displays them as colored pixels in a grid pattern. Each pixel can show different colors based on the input data.

When to use: RGB displays are great for:
• Creating graphics and images
• Building games and visual effects
• Displaying color-coded information
• Creating animated patterns
• Visual data representation

Example: Connect a pattern generator to create colorful animations, or use it to display simple graphics and sprites.

Tips: RGB displays can create millions of colors by combining different intensities of red, green, and blue.",

			[ChipType.DisplayDot] = @"Dot Matrix Display

What it is: A monochrome display that shows a 16×16 grid of dots that can be on (white) or off (black).

How it works: The display takes binary data representing which dots should be lit. Each bit in the input data corresponds to one dot in the grid - 1 for lit, 0 for dark.

When to use: Dot matrix displays are useful for:
• Showing simple graphics and patterns
• Creating text displays
• Building games (like Tetris)
• Displaying binary data visually
• Creating custom symbols and icons

Example: Connect a pattern generator to create moving animations, or use it to display simple text characters.

Tips: You can create letters, numbers, and simple graphics by carefully arranging which dots are lit.",

			[ChipType.DisplayLED] = @"LED Display

What it is: A simple light-emitting diode that can be turned on (bright) or off (dark) based on an input signal.

How it works: The LED takes a single input signal. When the input is 1 (high), the LED lights up brightly. When the input is 0 (low), the LED is dark.

When to use: LEDs are perfect for:
• Status indicators (on/off, active/inactive)
• Simple visual feedback
• Debugging circuits
• Creating warning lights
• Showing binary states

Example: Connect an LED to a button to show when the button is pressed, or use it to indicate when a circuit is active.

Tips: LEDs are simple but effective for providing immediate visual feedback about circuit states.",

			[ChipType.DisplayRGBTouch] = @"RGB Touch Display

What it is: A color display with touch input capability that can show colored pixels and detect when you touch specific areas.

How it works: Like an RGB display, it shows a grid of colored pixels. Additionally, it can detect touch input and output information about where and when the display was touched.

When to use: RGB touch displays are ideal for:
• Creating interactive interfaces
• Building touch-based games
• Implementing user input systems
• Creating interactive tutorials
• Building custom control panels

Example: Create an interactive calculator where users can touch buttons to input numbers and see results on the colored display.

Tips: Touch displays combine visual output with user input, making them perfect for interactive applications.",

			// ---- Input/Output ----
			[ChipType.Button] = @"Button

What it is: A push button that outputs 1 when pressed and 0 when released.

How it works: The button has a single output that goes high (1) when the button is physically pressed down, and goes low (0) when released. The output follows the physical state of the button.

When to use: Buttons are essential for:
• User input and control
• Triggering actions and events
• Manual control of circuits
• Creating interactive interfaces
• Testing and debugging

Example: Connect a button to a counter to manually increment it, or use it to start/stop a process.

Tips: Buttons can be 'bouncy' - they may rapidly switch between states when pressed. Use a pulse generator or debouncing circuit for clean signals.",

			[ChipType.Toggle] = @"Toggle Switch

What it is: A switch that maintains its state - once turned on, it stays on until manually turned off.

How it works: Unlike a button, a toggle switch has two stable states. When you flip it on, the output goes to 1 and stays there. When you flip it off, the output goes to 0 and stays there until you flip it again.

When to use: Toggle switches are perfect for:
• Setting permanent states (on/off)
• Enabling/disabling features
• Creating mode selectors
• Storing simple user preferences
• Creating power switches

Example: Use a toggle to enable/disable a feature, or to switch between different operating modes of a circuit.

Tips: Toggle switches provide stable, persistent control - ideal for settings that should remain until manually changed.",

			[ChipType.Key] = @"Keyboard Input

What it is: A component that outputs different values based on which key is pressed on a connected keyboard.

How it works: The key component monitors keyboard input and outputs a value corresponding to the pressed key. Different keys produce different output values, allowing you to create keyboard-controlled circuits.

When to use: Keyboard input is great for:
• Creating keyboard-controlled games
• Building text input systems
• Implementing keyboard shortcuts
• Creating interactive tutorials
• Building custom input systems

Example: Create a game where arrow keys control movement, or build a calculator that responds to number key presses.

Tips: Keyboard input allows for complex user interaction and is perfect for creating interactive applications.",

			[ChipType.Constant_8Bit] = @"8-Bit Constant

What it is: A component that always outputs the same 8-bit value, regardless of any inputs.

How it works: The constant component is configured with a specific 8-bit value (0-255) and continuously outputs that value. It's like a battery that always provides the same voltage.

When to use: Constants are useful for:
• Providing fixed values to circuits
• Setting thresholds and limits
• Creating reference values
• Initializing counters and registers
• Testing and debugging circuits

Example: Use a constant value of 100 as a threshold for a comparator, or use it to initialize a counter to a specific starting value.

Tips: Constants are simple but essential - they provide the fixed values that many circuits need to function properly.",

			// ---- Special Components ----
			[ChipType.Buzzer] = @"Buzzer

What it is: An audio output device that produces sound when activated by an input signal.

How it works: The buzzer takes an input signal and produces an audible tone when the input is high (1). The frequency and duration of the sound depend on the input signal pattern.

When to use: Buzzers are perfect for:
• Creating audio alerts and notifications
• Building alarm systems
• Providing audio feedback
• Creating simple music and sound effects
• Implementing audio warnings

Example: Connect a buzzer to a timer to create an alarm, or use it to provide audio feedback when a button is pressed.

Tips: Buzzers add an important audio dimension to your circuits, making them more interactive and user-friendly.",

			[ChipType.RTC] = @"Real-Time Clock

What it is: A clock that keeps track of real-world time, providing hours, minutes, and seconds.

How it works: The RTC continuously tracks time and outputs the current time values (hours, minutes, seconds) as separate outputs. It maintains accurate time even when the circuit is running.

When to use: Real-time clocks are essential for:
• Creating time-based applications
• Building clocks and timers
• Scheduling events
• Creating time-stamped data
• Building time-sensitive systems

Example: Create a digital clock display, or build a system that performs actions at specific times of day.

Tips: RTCs provide real-world time reference, making your circuits aware of actual time passage.",

			[ChipType.SPS] = @"Steps Per Second

What it is: A clock generator that produces a specific number of pulses per second, useful for controlling the speed of operations.

How it works: The SPS component generates clock pulses at a precise rate (e.g., 1 pulse per second, 10 pulses per second). This allows you to control how fast your circuit operates.

When to use: SPS clocks are perfect for:
• Controlling simulation speed
• Creating timed sequences
• Building slow-motion circuits
• Creating educational demonstrations
• Controlling animation speed

Example: Use a 1 SPS clock to create a slow-motion demonstration, or use a 10 SPS clock for faster but still visible operations.

Tips: SPS clocks let you control the pace of your circuit, making complex operations easier to observe and understand."
		};

		/// <summary>
		/// Gets the description for a specific chip type
		/// </summary>
		/// <param name="chipType">The chip type to get description for</param>
		/// <returns>The description text, or a default message if not found</returns>
		public static string GetDescription(ChipType chipType)
		{
			if (chipDescriptions.TryGetValue(chipType, out string description))
			{
				return description;
			}

			// Default description for unknown chip types
			return $@"{chipType} Chip

What it is: A {chipType} component with specific functionality.

How it works: This chip performs operations based on its input signals and produces corresponding outputs.

When to use: Use this chip when you need its specific functionality in your circuit design.

Example: Connect appropriate inputs and observe the outputs to understand how this chip behaves.

Tips: Experiment with different input combinations to discover all the capabilities of this chip.";
		}

		/// <summary>
		/// Gets the description for a chip by name (for custom chips or chips not in the enum)
		/// </summary>
		/// <param name="chipName">The name of the chip</param>
		/// <returns>The description text, or a default message if not found</returns>
		public static string GetDescriptionByName(string chipName)
		{
			// Try to find a matching chip type by name
			foreach (var kvp in chipDescriptions)
			{
				if (ChipTypeHelper.GetName(kvp.Key).Equals(chipName, System.StringComparison.OrdinalIgnoreCase))
				{
					return kvp.Value;
				}
			}

			// Default description for unknown chip names
			return $@"{chipName} Chip

What it is: A {chipName} component with specific functionality.

How it works: This chip performs operations based on its input signals and produces corresponding outputs.

When to use: Use this chip when you need its specific functionality in your circuit design.

Example: Connect appropriate inputs and observe the outputs to understand how this chip behaves.

Tips: Experiment with different input combinations to discover all the capabilities of this chip.";
		}
	}
}
