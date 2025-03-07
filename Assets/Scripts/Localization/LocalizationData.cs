using System;
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    [CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Translation Data")]
    public class LocalizationData : ScriptableObject
    {
        public List<TranslationEntry> translations;
        public event Action OnLanguageChanged;

        private string currentLanguage = "English";
        private Dictionary<string, TranslationEntry> translationDict;

        private void OnEnable()
        {
            BuildTranslationDictionary();
        }

        private void BuildTranslationDictionary()
        {
            translationDict = new();

            translationDict.Clear();

            foreach (var entry in translations)
            {
                entry.BuildDictionary(); // Ensure translation dictionaries are built
                translationDict[entry.key] = entry;
            }
        }

        public void SetLanguage(string language)
        {
            currentLanguage = language;
            OnLanguageChanged?.Invoke();
        }

        public string GetTranslation(string key)
        {
            if (translationDict.TryGetValue(key, out var entry))
            {
                return entry.GetTranslation(currentLanguage);
            }
            return key; // Fallback
        }
    }
}
