namespace OojuInteractionPlugin
{
    public interface IOISettings
    {
        string ApiKey { get; set; }
        string BaseUrl { get; set; }
        float DefaultHoverAmplitude { get; set; }
        float DefaultHoverFrequency { get; set; }
        float DefaultWobbleAmount { get; set; }
        float DefaultWobbleSpeed { get; set; }
        float DefaultScaleAmount { get; set; }
        float DefaultScaleSpeed { get; set; }
        string ClaudeApiKey { get; set; } // Claude API key
        string GeminiApiKey { get; set; } // Gemini API key
        string SelectedLLMType { get; set; } // Selected LLM type: "OpenAI", "Claude", or "Gemini"
        void SaveSettings();
        void LoadSettings();
    }

    public interface ISettingsProvider
    {
        IOISettings GetSettings();
    }
}