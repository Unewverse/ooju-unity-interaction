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
        void SaveSettings();
        void LoadSettings();
    }

    public interface ISettingsProvider
    {
        IOISettings GetSettings();
    }
}