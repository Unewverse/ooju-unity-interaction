using UnityEngine;
using UnityEditor;

// UIStyles: Provides custom GUIStyles for EditorWindow UI
public class UIStyles
{
    public GUIStyle BoldLabel { get; private set; }
    public GUIStyle MiniLabel { get; private set; }
    public GUIStyle WordWrappedLabel { get; private set; }
    public GUIStyle HelpBox { get; private set; }
    public GUIStyle ToolbarButton { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    // Call this once before using styles
    public void Initialize()
    {
        if (IsInitialized) return;

        BoldLabel = new GUIStyle(EditorStyles.boldLabel);
        MiniLabel = new GUIStyle(EditorStyles.miniLabel);
        WordWrappedLabel = new GUIStyle(EditorStyles.wordWrappedLabel);
        HelpBox = new GUIStyle(EditorStyles.helpBox);
        ToolbarButton = new GUIStyle(EditorStyles.toolbarButton);

        IsInitialized = true;
    }
}
