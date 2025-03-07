using System;
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    [Serializable]
    public class TranslationEntry
    {
        public string key; // Unique identifier for the text
        public List<LanguageTranslation> translations = new(); // Stores multiple languages

        private Dictionary<string, string> translationDict;

        public void BuildDictionary()
        {
            translationDict = new Dictionary<string, string>();

            foreach (var langEntry in translations)
            {
                if (langEntry.language != null)
                {
                    translationDict[langEntry.language] = langEntry.text;
                }
                else
                {
                    Debug.LogWarning("Language key is null for one of the translations.");
                }
            }
        }

        public string GetTranslation(string language)
        {
            if (translationDict == null) BuildDictionary(); // Ensure it's initialized
            return translationDict.TryGetValue(language, out var value) ? value : key; // Fallback to key
        }
    }
}
