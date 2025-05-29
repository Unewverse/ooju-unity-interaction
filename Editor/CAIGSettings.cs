using UnityEngine;
using System;

namespace OojuInteractionPlugin
{
    // Implementation of OI settings using ScriptableObject
    [CreateAssetMenu(fileName = "OISettings", menuName = "OOJU/OI Settings")]
    public class OISettings : ScriptableObject, IOISettings
    {
        // Singleton instance
        private static OISettings instance;
        public static OISettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<OISettings>("OISettings");
                    if (instance == null)
                    {
                        instance = CreateInstance<OISettings>();
                        instance.LoadSettings();
                    }
                }
                return instance;
            }
        }

        // API settings
        [SerializeField] private string apiKey = "";
        [SerializeField] private string baseUrl = "https://api.ooju.com";
        [SerializeField] private string claudeApiKey = "";
        [SerializeField] private string geminiApiKey = "";
        [SerializeField] private string selectedLLMType = "OpenAI";

        // Animation settings
        [SerializeField] private float defaultHoverAmplitude = 0.5f;
        [SerializeField] private float defaultHoverFrequency = 1f;
        [SerializeField] private float defaultWobbleAmount = 15f;
        [SerializeField] private float defaultWobbleSpeed = 1f;
        [SerializeField] private float defaultScaleAmount = 0.2f;
        [SerializeField] private float defaultScaleSpeed = 1f;

        // Property implementations
        public string ApiKey
        {
            get
            {
                // Priority: Environment variable
                string envKey = Environment.GetEnvironmentVariable("OOJU_OPENAI_API_KEY");
                if (!string.IsNullOrEmpty(envKey))
                    return envKey;
                // Fallback: PlayerPrefs or asset value
                return apiKey;
            }
            set { apiKey = value; SaveSettings(); }
        }

        public string BaseUrl
        {
            get => baseUrl;
            set
            {
                baseUrl = value;
                SaveSettings();
            }
        }

        public float DefaultHoverAmplitude
        {
            get => defaultHoverAmplitude;
            set
            {
                defaultHoverAmplitude = value;
                SaveSettings();
            }
        }

        public float DefaultHoverFrequency
        {
            get => defaultHoverFrequency;
            set
            {
                defaultHoverFrequency = value;
                SaveSettings();
            }
        }

        public float DefaultWobbleAmount
        {
            get => defaultWobbleAmount;
            set
            {
                defaultWobbleAmount = value;
                SaveSettings();
            }
        }

        public float DefaultWobbleSpeed
        {
            get => defaultWobbleSpeed;
            set
            {
                defaultWobbleSpeed = value;
                SaveSettings();
            }
        }

        public float DefaultScaleAmount
        {
            get => defaultScaleAmount;
            set
            {
                defaultScaleAmount = value;
                SaveSettings();
            }
        }

        public float DefaultScaleSpeed
        {
            get => defaultScaleSpeed;
            set
            {
                defaultScaleSpeed = value;
                SaveSettings();
            }
        }

        public string ClaudeApiKey { get => claudeApiKey; set { claudeApiKey = value; SaveSettings(); } }
        public string GeminiApiKey { get => geminiApiKey; set { geminiApiKey = value; SaveSettings(); } }
        public string SelectedLLMType { get => selectedLLMType; set { selectedLLMType = value; SaveSettings(); } }

        // Save settings to PlayerPrefs
        public void SaveSettings()
        {
            PlayerPrefs.SetString("CAIG_ApiKey", apiKey);
            PlayerPrefs.SetString("CAIG_ClaudeApiKey", claudeApiKey);
            PlayerPrefs.SetString("CAIG_GeminiApiKey", geminiApiKey);
            PlayerPrefs.SetString("CAIG_SelectedLLMType", selectedLLMType);
            PlayerPrefs.SetString("CAIG_BaseUrl", baseUrl);
            PlayerPrefs.SetFloat("CAIG_HoverAmplitude", defaultHoverAmplitude);
            PlayerPrefs.SetFloat("CAIG_HoverFrequency", defaultHoverFrequency);
            PlayerPrefs.SetFloat("CAIG_WobbleAmount", defaultWobbleAmount);
            PlayerPrefs.SetFloat("CAIG_WobbleSpeed", defaultWobbleSpeed);
            PlayerPrefs.SetFloat("CAIG_ScaleAmount", defaultScaleAmount);
            PlayerPrefs.SetFloat("CAIG_ScaleSpeed", defaultScaleSpeed);
            PlayerPrefs.Save();
        }

        // Load settings from PlayerPrefs
        public void LoadSettings()
        {
            apiKey = PlayerPrefs.GetString("CAIG_ApiKey", "");
            claudeApiKey = PlayerPrefs.GetString("CAIG_ClaudeApiKey", "");
            geminiApiKey = PlayerPrefs.GetString("CAIG_GeminiApiKey", "");
            selectedLLMType = PlayerPrefs.GetString("CAIG_SelectedLLMType", "OpenAI");
            baseUrl = PlayerPrefs.GetString("CAIG_BaseUrl", "https://api.ooju.com");
            defaultHoverAmplitude = PlayerPrefs.GetFloat("CAIG_HoverAmplitude", 0.5f);
            defaultHoverFrequency = PlayerPrefs.GetFloat("CAIG_HoverFrequency", 1f);
            defaultWobbleAmount = PlayerPrefs.GetFloat("CAIG_WobbleAmount", 15f);
            defaultWobbleSpeed = PlayerPrefs.GetFloat("CAIG_WobbleSpeed", 1f);
            defaultScaleAmount = PlayerPrefs.GetFloat("CAIG_ScaleAmount", 0.2f);
            defaultScaleSpeed = PlayerPrefs.GetFloat("CAIG_ScaleSpeed", 1f);
        }
    }

    // Default implementation of settings provider
    public class DefaultSettingsProvider : ISettingsProvider
    {
        public IOISettings GetSettings()
        {
            return OISettings.Instance;
        }
    }
} 