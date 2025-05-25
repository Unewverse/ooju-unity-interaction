# OOJU Interaction Unity Package

## Overview
OOJU Interaction is a powerful Unity Editor extension package that enhances your development workflow with AI-powered scene analysis and interaction generation capabilities. This package provides a comprehensive set of tools for creating interactive behaviors and animations in your Unity projects.

## Key Features
- **AI-Powered Scene Analysis**
  - Generate detailed scene descriptions using OpenAI
  - Get intelligent interaction suggestions for selected objects
  - Convert natural language descriptions into Unity C# scripts
  - Automatic object detection and script application

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
- OpenAI API key (for AI features)

## Getting Started
1. **Setup**
   - Open the OOJU Interaction window (OOJU > Interaction)
   - Configure your OpenAI API key in the **Settings** tab
   - Create animation presets if needed

2. **Scene Analysis**
   - Select objects in your scene
   - Click "Generate Scene Description"
   - Review the generated description and suggestions

3. **Creating Interactions**
   - Enter a natural language description of the desired interaction
   - Click "Generate Interaction"
   - Follow the instructions to apply the generated script

4. **Applying Animations**
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

3. **Script Generation**
   - Provide clear, specific descriptions
   - Review generated scripts before applying
   - Test interactions thoroughly

## Troubleshooting
- **API Key Issues**: Ensure your OpenAI API key is correctly set in the Settings tab
- **Script Generation Failures**: Check your internet connection and API key validity
- **Animation Problems**: Verify object components and hierarchy structure

## Version History
- 0.1.0: Initial release
  - Basic scene analysis
  - Animation system
  - Script generation
  - Editor integration

