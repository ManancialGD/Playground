using UnityEngine;
using UnityEngine.UI;

namespace Localization.GUI
{
    public class LanguageSwitcher : MonoBehaviour
    {
        [SerializeField] private LocalizationData localizationData;
        [SerializeField] private Button switchLanguageButton;
        private int languageIndex;

        private void Start()
        {
            if (localizationData == null)
            {
                localizationData = Resources.Load<LocalizationData>("MainLocalizationData");
            }

            switchLanguageButton.onClick.AddListener(SwitchLanguage);
        }

        public void SwitchLanguage()
        {
            languageIndex += 1;
            if (languageIndex > 2)
                languageIndex = 0;
            
            localizationData.SetLanguage(languageIndex == 0 ? "English" : languageIndex == 1 ? "Portuguese" : "Japanese");
        }

        private void OnValidate()
        {
            if (switchLanguageButton == null)
                switchLanguageButton = GetComponent<Button>();
        }
    }
}
