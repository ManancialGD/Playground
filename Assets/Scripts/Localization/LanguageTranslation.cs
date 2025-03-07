using System;

namespace Localization
{
    [Serializable]
    public struct LanguageTranslation
    {
        public string language;
        public string text;

        public LanguageTranslation(string language, string text)
        {
            this.language = language;
            this.text = text;
        }
    }
}
