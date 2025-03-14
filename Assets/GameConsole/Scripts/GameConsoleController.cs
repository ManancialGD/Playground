using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.IO;

namespace GameConsole
{
    /// <summary>
    /// A simple in-game console system that handles user inputs, processes commands, 
    /// and displays results in a log. This class supports command parsing, execution,
    /// and the registration of custom commands that can be invoked via a text input.
    /// The default commands, such as 'help', 'move', and 'spawn', can be replaced
    /// with custom commands for specific game actions.
    ///
    /// The console operates by listening for user input from an <see cref="InputField"/>
    /// and logs messages in a <see cref="TMP_Text"/> UI element. It supports dynamic
    /// command execution with arguments and provides helpful logs for feedback.
    /// </summary>
    public class GameConsoleController : MonoBehaviour
    {
        // Serialized fields
        [Header("Player Settings")]
        [SerializeField] private PlayerSettings playerSettings;

        [Header("UI Elements")]
        [SerializeField] private InputField commandInputField;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private GameObject canvas;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference openConsoleAction;

        [Header("Events")]
        public UnityEvent ConsoleOpened;
        public UnityEvent ConsoleClosed;

        // Private variables
        private bool open;
        private Dictionary<string, CommandDefinition> commands;

        // Public properties
        public Dictionary<string, CommandDefinition> Commands => new(commands);

        private void Awake()
        {
            // Initialize the commands dictionary with placeholder commands.
            commands = new Dictionary<string, CommandDefinition>
            {
                // Example 'help' command which lists all available commands
                {
                    "help",
                    new CommandDefinition(
                        ShowHelp,
                        "Displays a list of all available commands.\n" +
                        "Provide a command name as an argument to see details " +
                        "about what it does.",
                        new[]
                        {
                            new CommandArgument("Command name", typeof(string), new string[]{"home"})
                        }
                    )
                },
                {
                    "snd_effects", new CommandDefinition(SoundEffectVolume,
                        help: "Change sound effects volume.\n" +
                            " 0 = mute, 1 = default volume.\n" +
                            " To make it lower then default use decimals."+
                            " To make it higher then default volume, give it a higher number.",
                        arguments:
                        new CommandArgument[]
                        {
                            new("volume", typeof(float), defaultValue: 1.0f)
                        })
                },
                {
                    "snd_music", new CommandDefinition(MusicVolume,
                        help: "Change music volume.\n" +
                            " 0 = mute, 1 = default volume.\n" +
                            " To make it lower then default use decimals."+
                            " To make it higher then default volume, give it a higher number.",
                        arguments:
                        new CommandArgument[]
                        {
                            new("volume", typeof(float), defaultValue: 1.0f)
                        })
                },
                {
                    "snd_main", new CommandDefinition(MainVolume,
                        help: "Change main sound volume. This includes all sounds in the game.\n" +
                            " 0 = mute, 1 = default volume.\n" +
                            " To make it lower then default use decimals."+
                            " To make it higher then default volume, give it a higher number.",
                        arguments:
                        new CommandArgument[]
                        {
                            new("volume", typeof(float), defaultValue: 1.0f)
                        })
                },
                {
                    "invert_mouse_y", new CommandDefinition(ChangeMuseInvertY,
                        help: "Iverts mouse y",
                        arguments: new CommandArgument[]
                        {
                            new ("value", typeof(bool))
                        }
                    )
                },
            };

            // Check if references are properly assigned.
            if (commandInputField == null || logText == null)
            {
                Debug.LogError("GameConsole references are not set correctly.", this);
                return;
            }
            open = canvas.activeInHierarchy;
            Log("Console initialized. Type 'help' for a list of commands.");
        }

        private void OnEnable()
        {
            // Add listener for when a command is entered in the input field.
            commandInputField.onSubmit.AddListener(OnCommandEntered);

            if (openConsoleAction != null)
                openConsoleAction.action.performed += OnConsoleAction;
        }

        private void OnDisable()
        {
            // Remove the listener when the object is disabled.
            commandInputField.onSubmit.RemoveListener(OnCommandEntered);

            if (openConsoleAction != null)
                openConsoleAction.action.performed -= OnConsoleAction;
        }

        public void OnConsoleAction(InputAction.CallbackContext context)
        {
            open = !open;
            if (open)
            {
                canvas.SetActive(true);
                ConsoleOpened?.Invoke();
            }
            else
            {
                canvas.SetActive(false);
                ConsoleClosed?.Invoke();
            }
        }

        /// <summary>
        /// Called when a command is entered in the input field.
        /// </summary>
        /// <param name="input">The command entered by the user.</param>
        public void OnCommandEntered(string input)
        {
            if (string.IsNullOrWhiteSpace(input) ||
                string.IsNullOrEmpty(input)) return; // Ignore empty inputs.

            // Log the command entered by the user.
            Log($"> {input}");

            // Parse and execute the command.
            ParseCommand(input);
        }

        /// <summary>
        /// Parses the entered command and executes it.
        /// </summary>
        /// <param name="input">The command string to parse.</param>
        private void ParseCommand(string input) //todo: preview command arguments too.
        {
            string[] parts = input.Split(' ');  // Split the input into command and arguments.
            string commandName = parts[0].ToLower();  // Get the command name (first word).

            // Check if the command exists in the dictionary.
            if (!commands.TryGetValue(commandName, out CommandDefinition command))
            {
                Log($"Unknown command: {commandName}"); // Log if the command is not found.
                return;
            }

            string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>(); // Extract arguments.

            try
            {
                // Parse the arguments for the command.
                object[] parsedArgs = command.ParseArguments(args);
                // Invoke the action associated with the command using the parsed arguments.
                command.Action.Invoke(parsedArgs);
            }
            catch (Exception ex)
            {
                Log("An error occurred.");
                Debug.LogError($"Error: {ex.Message}"); // Log errors during command execution.
            }
        }

        /// <summary>
        /// Displays the available commands and their usage.
        /// </summary>
        private void ShowHelp(params object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is string commandStr)
            {
                if (commands.TryGetValue(commandStr, out CommandDefinition value))
                {
                    Log(value.Help, 6);
                }
                else ShowHelp(null);
            }
            else
            {
                Log("<size=22>Available commands:</size>", 8, 6);

                foreach (var cmd in commands)
                {
                    string commandText = $"{cmd.Key}";
                    string usage = cmd.Value.GetUsage();
                    if (!string.IsNullOrEmpty(usage) || !string.IsNullOrWhiteSpace(usage))
                    {
                        commandText += $": {usage}";
                    }
                    commandText += ";";

                    // Log each command's name and usage description.
                    Log($"{commandText}");
                }
            }
        }

        /// <summary>
        /// <br>Adjusts the sound effects volume in the audio mixer.</br>
        /// <br>Converts a linear volume value (0.0 to 1.0) into a decibel (dB) scale to match Unity's audio system.</br>
        /// </summary>
        /// <remarks>
        /// <br>- Clamps very low values (≤ 1e-5) to prevent errors from logarithm calculations.</br>
        /// <br>- Unity audio mixer interprets 0 dB as the original volume and clamps values near 0 to -80 dB (mute).</br>
        /// </remarks>
        /// <param name="args">
        /// <br>A single parameter is expected:</br>
        /// <br>- <c>args[0]</c>: A float between, being 0 mute.</br>
        /// </param>
        private void SoundEffectVolume(params object[] args)
        {
            // Check if arguments are provided
            if (args == null || args.Length == 0)
            {
                Log("Please provide a volume value.");
                return;
            }

            try
            {
                // Retrieve the first argument and cast it to a float
                float v = (float)args[0];
                v *= 100;
                playerSettings.SoundEffectsVolume = v;
            }
            catch (Exception ex)
            {
                Log("An error occurred while setting the main volume.");
                Debug.LogError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// <br>Adjusts the sound effects volume in the audio mixer.</br>
        /// <br>Converts a linear volume value (0.0 to 1.0) into a decibel (dB) scale to match Unity's audio system.</br>
        /// </summary>
        /// <remarks>
        /// <br>- Clamps very low values (≤ 1e-5) to prevent errors from logarithm calculations.</br>
        /// <br>- Unity audio mixer interprets 0 dB as the original volume and clamps values near 0 to -80 dB (mute).</br>
        /// </remarks>
        /// <param name="args">
        /// <br>A single parameter is expected:</br>
        /// <br>- <c>args[0]</c>: A float between, being 0 mute.</br>
        /// </param>
        private void MusicVolume(params object[] args)
        {
            // Check if arguments are provided
            if (args == null || args.Length == 0)
            {
                Log("Please provide a volume value.");
                return;
            }

            try
            {
                // Retrieve the first argument and cast it to a float
                float v = (float)args[0];
                v *= 100;
                playerSettings.MusicVolume = v;
            }
            catch (Exception ex)
            {
                Log("An error occurred while setting the music volume.");
                Debug.LogError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// <br>Adjusts the sound effects volume in the audio mixer.</br>
        /// <br>Converts a linear volume value (0.0 to 1.0) into a decibel (dB) scale to match Unity's audio system.</br>
        /// </summary>
        /// <remarks>
        /// <br>- Clamps very low values (≤ 1e-5) to prevent errors from logarithm calculations.</br>
        /// <br>- Unity audio mixer interprets 0 dB as the original volume and clamps values near 0 to -80 dB (mute).</br>
        /// </remarks>
        /// <param name="args">
        /// <br>A single parameter is expected:</br>
        /// <br>- <c>args[0]</c>: A float between, being 0 mute.</br>
        /// </param>
        private void MainVolume(params object[] args)
        {
            // Check if arguments are provided
            if (args == null || args.Length == 0)
            {
                Log("Please provide a volume value.");
                return;
            }

            try
            {
                // Retrieve the first argument and cast it to a float
                float v = (float)args[0];
                v *= 100;
                playerSettings.MainVolume = v;
            }
            catch (Exception ex)
            {
                Log("An error occurred while setting the main volume.");
                Debug.LogError($"Error: {ex.Message}");
            }

        }

        private void ChangeMuseInvertY(params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0] is bool boolValue)
                {
                    playerSettings.InvertMouseY = boolValue;
                    Log($"Invert Mouse Y set to {playerSettings.InvertMouseY}");
                }
                else if (args[0] == null)
                {
                    playerSettings.InvertMouseY = !playerSettings.InvertMouseY;
                    Log($"Invert Mouse Y set to {playerSettings.InvertMouseY}");
                }
                else
                {
                    Log("Invalid value. Expected: True, False, 0, 1");
                }
            }
            else
            {
                playerSettings.InvertMouseY = !playerSettings.InvertMouseY;
                Log($"Invert Mouse Y set to {playerSettings.InvertMouseY}");
            }
        }

        /// <summary>
        /// Logs a message to the console with optional spacing before and after the message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="beforeSpace">The size of the space before the message.</param>
        /// <param name="afterSpace">The size of the space after the message.</param>
        private void Log(string message, float beforeSpace = 4, float afterSpace = 0)
        {
            if (beforeSpace > 0)
                logText.text += $"<size={beforeSpace}> </size>\n";

            logText.text += $"{message}\n";

            if (afterSpace > 0)
                logText.text += $"<size={afterSpace}> </size>\n";
        }

        private void OnValidate()
        {
            // Resources Loading is expensive, we hope to do this once max.
            if (playerSettings == null)
            {
                PlayerSettings resourcesPlayerSettings = Resources.Load<PlayerSettings>(Path.Combine("Settings", "PlayerSettings"));
                if (resourcesPlayerSettings != null)
                    playerSettings = resourcesPlayerSettings;
                else
                    Debug.LogError("PlayeSerrings is not setup correctly, this may cause performance drops.");
            }

            if (commandInputField == null)
                commandInputField = GetComponentInChildren<InputField>();

            if (logText == null)
            {
                try
                {
                    TextMeshProUGUI tmpLogText = transform.Find("LogText").GetComponent<TextMeshProUGUI>();
                    logText = tmpLogText;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error finding or assigning logText: {ex.Message}");
                }
            }
        }
    }
}

