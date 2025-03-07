using TMPro;
using UnityEngine;

namespace Localization.GUI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private LocalizationData localizationData;
        [SerializeField] private string translationKey;
        [SerializeField] private TextMeshProUGUI textComponent;

        private void Start()
        {
            OnValidate(); // Ensure textComponent is set

            localizationData.OnLanguageChanged += UpdateText;
        }

        private void OnDestroy()
        {
            localizationData.OnLanguageChanged -= UpdateText;
        }

        private void OnValidate()
        {
            if (textComponent == null)
                textComponent = GetComponent<TextMeshProUGUI>();
            if (localizationData == null)
                localizationData = Resources.Load<LocalizationData>("MainLocalizationData");

            UpdateText();
        }

        private void UpdateText()
        {
            textComponent.text = localizationData.GetTranslation(translationKey);
        }
    }
}
