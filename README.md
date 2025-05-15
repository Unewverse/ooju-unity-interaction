# OOJU Interaction Unity Package

## Overview
This package provides a set of tools and editor extensions for easily adding interactive behaviors and AI-powered suggestions to your Unity projects. It includes:
- Editor windows for scene analysis and interaction script generation
- Animation utilities for common object behaviors (hover, wobble, etc.)
- ScriptableObject-based settings and animation presets
- Integration with OpenAI for scene description and interaction suggestions

## Installation
1. Copy the `OOJUInteraction` folder into your project's `Assets` directory.
2. Ensure all dependencies are installed (see below).
3. Open Unity. The package will be available in the Editor menu under `OOJU`.

## Main Features
- **OOJU Interaction Window**: Go to `OOJU > Interaction` in the Unity Editor menu. Use this window to:
  - Generate scene descriptions using OpenAI
  - Get interaction suggestions for selected objects
  - Generate and apply C# interaction scripts from natural language
  - Apply common animations (hover, wobble, etc.) to objects
- **Animation Presets**: Create and manage reusable animation parameter sets using `AnimationPreset` ScriptableObjects.
- **Settings**: Store API keys and default animation parameters using the `OISettings` ScriptableObject.

## How to Use
1. **Scene Description & Suggestions**: Open the Interaction window, click `Generate Scene Description`, and follow the prompts.
2. **Sentence-to-Interaction**: Enter a natural language description and generate a Unity C# script. The script will be saved and instructions shown in the window.
3. **Apply Animations**: Select objects in the scene, choose an animation type and parameters, and click `Apply Animation`.
4. **Animation Presets**: Create a new `AnimationPreset` asset via `Create > OOJU > Animation Preset` and assign it in the relevant UI.

## Extending & Customizing
- **Add New Animation Types**: Extend the `AnimationType` enum and implement the corresponding logic in `ObjectAutoAnimator` and `AnimationUI`.
- **Custom Editor Styles**: Modify or extend `UIStyles.cs` for custom editor UI appearance.
- **Settings**: Add new fields to `OISettings` and update the editor UI as needed.

## Dependencies
- **Newtonsoft.Json**: Required for OpenAI API integration. Install via Unity Package Manager (`com.unity.nuget.newtonsoft-json`) or from [GitHub](https://github.com/JamesNK/Newtonsoft.Json).
- **UnityEditor**: All editor scripts require Unity Editor.
- **UnityEngine**: Core Unity engine.

## Support & Contact
For questions, bug reports, or feature requests, please contact the OOJU team or open an issue on the repository where this package is hosted.

