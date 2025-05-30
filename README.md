# OOJU Interaction Unity Package

## Overview
OOJU Interaction is a powerful Unity Editor extension package that enhances your development workflow with AI-powered scene analysis and interaction generation capabilities. This package provides a comprehensive set of tools for creating interactive behaviors and animations in your Unity projects.

Supports multiple AI models: **OpenAI**, **Claude**, **Gemini** (with local LLM support planned for the future).

## Key Features
- **AI-Powered Scene Analysis**
  - Generate detailed scene descriptions using OpenAI, Claude, or Gemini APIs
  - Get intelligent interaction suggestions for selected objects
  - Convert natural language descriptions into Unity C# scripts
  - Automatic object detection and script application
  - Easily switch between OpenAI, Claude, and Gemini models in the settings (local LLM support coming soon)

- **Animation System**
  - Built-in animation types (hover, wobble, etc.)
  - Customizable animation parameters
  - Animation preset system for reusable configurations
  - Real-time animation preview and testing

- **Editor Integration**
  - Intuitive Unity Editor window interface
  - Drag-and-drop functionality
  - Real-time feedback and suggestions
  - Seamless integration with Unity's existing tools

- **Player & Ground Utility Tools**
  - Add Player section in the Editor window for quick scene setup
  - **Add First-person Player**: Instantly create a first-person player prefab (WASD/arrow keys, mouse look, spacebar jump)
  - **Add Ground**: Quickly add a large ground cube to your scene
  - **Set Selected as Ground**: Add a MeshCollider and set layer to Default for selected objects
  - All generated player scripts are saved to `Assets/OOJU/Interaction/Player` for maximum compatibility, regardless of where the package is installed in your project

## Installation
1. **Package Manager Installation**
   - Open Unity Package Manager
   - Click the '+' button
   - Select "Add package from git URL"
   - Enter: `https://github.com/Unewverse/ooju-unity-interaction.git`

2. **Manual Installation**
   - Download the package
   - Copy the `OOJUInteraction` folder into your project's `Assets` directory
   - Ensure all dependencies are installed

## Dependencies
- Unity 2021.3 or later
- Newtonsoft.Json (com.unity.nuget.newtonsoft-json: 3.0.2)
- API key for at least one supported LLM:
  - OpenAI (for GPT models)
  - Claude (Anthropic)
  - Gemini (Google)
- (Planned) Local LLM support in a future release

## Getting Started
1. **Setup**
   - Open the OOJU Interaction window (OOJU > Interaction)
   - Configure your preferred LLM API key (OpenAI, Claude, or Gemini) in the **Settings** tab
   - Create animation presets if needed

2. **Scene Analysis**
   - Select objects in your scene
   - Click "Generate Scene Description"
   - Review the generated description and suggestions

3. **Creating Interactions**
   - Enter a natural language description of the desired interaction
   - Click "Generate Interaction"
   - Follow the instructions to apply the generated script

4. **Player & Ground Setup**
   - Use the **Add Player** section to quickly add a first-person player or ground to your scene
   - "Add First-person Player" creates a ready-to-use player prefab (script saved to `Assets/OOJU/Interaction/Player`)
   - "Add Ground" creates a large ground cube at y=0
   - "Set Selected as Ground" adds a MeshCollider and sets the layer to Default for selected objects

5. **Applying Animations**
   - Select target objects
   - Choose animation type and parameters
   - Click "Apply Animation"
   - Adjust parameters in real-time

## Advanced Usage
### Custom Animation Types
1. Extend the `AnimationType` enum
2. Implement corresponding logic in `ObjectAutoAnimator`
3. Update `AnimationUI` for parameter controls

### Custom Editor Styles
- Modify `UIStyles.cs` to customize the editor appearance
- Add new UI elements as needed

### Settings Management
- Extend `CAIGSettings.cs` for additional configuration options
- Create custom settings assets using the ScriptableObject system

## Best Practices
1. **Scene Organization**
   - Keep related objects grouped together
   - Use clear, descriptive names for GameObjects
   - Maintain a clean hierarchy

2. **Animation Usage**
   - Create and save animation presets for common behaviors
   - Test animations in Play mode before finalizing
   - Use appropriate animation types for different scenarios

3. **Script Generation & Player Tools**
   - Provide clear, specific descriptions
   - Review generated scripts before applying
   - Test interactions and player movement thoroughly
   - All generated player scripts are always saved to `Assets/OOJU/Interaction/Player` for compatibility
   - The package is designed to work regardless of where the OOJUInteraction folder is placed in your Assets

## Troubleshooting
- **API Key Issues**: Ensure your LLM API key (OpenAI, Claude, or Gemini) is correctly set in the Settings tab
- **Script Generation Failures**: Check your internet connection and API key validity
- **Animation Problems**: Verify object components and hierarchy structure
- **Player/Ground Issues**: Ensure the correct colliders are present and the player prefab is not duplicated in the scene

## Version History
- 0.1.0: Initial release
  - Basic scene analysis
  - Animation system
  - Script generation
  - Editor integration
- 0.2.0: Add Player section and utility buttons (Add First-person Player, Add Ground, Set Selected as Ground)
  - Player script always saved to `Assets/OOJU/Interaction/Player`
  - Improved package compatibility and usability
  - Support for Claude and Gemini APIs (in addition to OpenAI)
  - Local LLM support planned for a future release

