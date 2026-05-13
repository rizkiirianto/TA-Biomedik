# GameManager.cs Documentation

## Overview

`GameManager.cs` is the core controller for the game's scenario progression system. It orchestrates the flow of the game by transitioning between different types of steps: **Quizzes, Minigames, Dialogs, and Cutscenes**. It handles UI updates, score tracking, timers, audio, and records player choices for a final recap.

The system is highly data-driven, relying on a `QuizData` JSON structure loaded from `Resources` to determine the sequence of events.

---

## Key Responsibilities

1. **Scenario Flow Management**: Reads JSON scenario data and transitions the game state between different steps (`quiz`, `minigame`, `dialog`, `cutscene`).
2. **UI Orchestration**: Updates text, character portraits, background images, narrative images, and quiz option buttons dynamically.
3. **Timer Management**: Handles timed quizzes with visual cues (progress bar, color pulse) and audio cues (clock ticking, heartbeat).
4. **Minigame & Cutscene Integration**: Spawns UI or World prefabs based on registries and interfaces with them via `IMiniGame` and `ICutscene` interfaces.
5. **Scoring System**: Awards points based on the number of attempts it takes to answer a quiz question correctly.
6. **Recap & Export**: Tracks the player's choices, attempts, and minigame results to generate a final summary that can be exported to a `.txt` file.

---

## Core Components

### 1. Registries
The `GameManager` uses registries to map string IDs from the JSON scenario to actual Unity Prefabs or AudioClips.
* **`MiniGameRegistry`**: Maps `minigameID` to a Minigame Prefab.
* **`CutsceneRegistry`**: Maps `cutsceneID` to a Cutscene Prefab.
* **`AmbanceAudioRegistry`**: Maps a Scenario ID to looping ambiance audio clips.

### 2. Step Types
The `GameManager` processes the `quizData.steps` array. Each step has a `stepType`:
* **`dialog`**: Displays narrative text, a character portrait, and an optional narrative/background image. Waits for the player to click the screen to advance.
* **`quiz`**: Displays a question and multiple choice options. Options appear sequentially. Handles correct/incorrect feedback, retries, fatal choices, and timers.
* **`minigame`**: Hides the main UI and spawns a minigame prefab. Calls `BeginGame(this)` on the prefab's `IMiniGame` interface.
* **`cutscene`**: Spawns a cutscene prefab and calls `BeginCutscene(this)` on its `ICutscene` interface. Can handle fatal cutscenes (returning to quiz) or temporary cutscenes (returning to minigame).

---

## Important Methods

### Initialization & Loading
* **`Start()`**: Initializes UI, resets scores, and loads the scenario.
* **`LoadQuizData()`**: Loads the scenario JSON file named in `PlayerPrefs("SelectedScenario")` from the `Resources` folder and plays the corresponding ambiance audio.

### Flow Control
* **`ShowStep(int stepIndex)`**: The main router that checks the `stepType` of the current step and calls `ShowQuiz`, `StartMiniGame`, `ShowDialog`, or `ShowCutscene`.
* **`GoToNextStep()`**: Increments the `currentStepIndex` and calls `ShowStep()`.
* **`PrepareToAdvance(bool showOverlayPanel)`**: Activates the transparent invisible button covering the screen, waiting for the player to click to proceed to the next step.

### Interaction Handlers
* **`OnOptionSelected(int optionIndex)`**: Called when a quiz option is clicked. Checks if the answer is correct, handles fatal choices (triggering fatal cutscenes), updates the score, and logs the attempt for the recap.
* **`OnAdvanceClicked()`**: Triggered by the invisible full-screen button to move to the next step or return to the main menu if the scenario is finished.

### Minigame & Cutscene Callbacks
* **`OnMiniGameComplete(string successFeedback)`**: Called by an `IMiniGame` script when a minigame finishes. Cleans up the minigame instance and prepares to advance.
* **`OnCutsceneComplete(string optionalFeedback)`**: Called by an `ICutscene` script. Restores the camera and handles returning to the correct state (next step, retry quiz, or resume minigame).

### Recap System
* **`RegisterKeyPointRecap()`**: Records the question, chosen answer, and attempts for the final summary.
* **`RegisterMinigameCall112Result()` / `RegisterMinigameCPRResult()`**: Specific methods to log detailed metrics from minigames.
* **`ExportRecapButtonClicked()`**: Generates a `.txt` file containing the episode summary and saves it to the game directory or persistent data path.

---

## UI and Visual Management

* **Portraits & Backgrounds**: Uses `Resources.Load<Sprite>()` to dynamically load background and narrative images specified in the JSON. Character portraits are loaded from an assigned array `gambarPortraitKarakter` based on index.
* **Quiz Options Reveal**: Uses the `RevealOptionsOneByOne` coroutine to create a staggered animation effect when quiz buttons appear.
* **Feedback Panel**: Displays temporary feedback (correct/incorrect/timeout messages) for a few seconds using `HideFeedbackPanelAfterDelay`.

---

## Timers

Quizzes can have a time limit defined in the JSON.
* **Visuals**: A fill bar that decreases over time. When time is low, the bar pulses red (`timerOverlayPulseMaxAlpha`).
* **Audio**: Plays a clock tick sound periodically, switching to a heartbeat sound when time is running low (`lowTimeCueSeconds`).
* **Timeout**: If time runs out, `HandleQuizTimeExpired()` is called, registering a failed attempt, showing feedback, and restarting the timer for a retry.

---

## Development & Debugging

The `GameManager` includes built-in shortcuts for testing in the Editor or standalone builds:
* **Numpad 0 / Alpha 0**: Skips the current step (`OnSkipButtonClicked()`).
* **Numpad 9 / Alpha 9**: Returns to the previous step (`OnPreviousButtonClicked()`).

---

## Integration Guide for Minigames & Cutscenes

To create a new Minigame or Cutscene that works with `GameManager`:
1. **Minigame**: Create a prefab with a script implementing the `IMiniGame` interface.
   * In `BeginGame(GameManager gm)`, store the `GameManager` reference.
   * When finished, call `gm.OnMiniGameComplete("Feedback text");`.
2. **Cutscene**: Create a prefab with a script implementing the `ICutscene` interface.
   * In `BeginCutscene(GameManager gm)`, store the `GameManager` reference.
   * When finished, call `gm.OnCutsceneComplete();`.
3. Register the prefab in the `GameManager` inspector under `Mini Game Registry` or `Cutscene Registry` with a unique ID matching the JSON data.
