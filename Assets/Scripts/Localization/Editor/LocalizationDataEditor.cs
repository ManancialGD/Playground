#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Localization.Editor
{
    [CustomEditor(typeof(LocalizationData))]
    public class MyScriptableObjectEditor : UnityEditor.Editor
    {
        private int languageIndex = 0;

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            // Get reference to the target ScriptableObject
            LocalizationData localizationData = (LocalizationData)target;

            // Create a button in the inspector
            if (GUILayout.Button("Switch Language"))
            {
                languageIndex += 1;
                if (languageIndex > 2)
                    languageIndex = 0;

                localizationData.SetLanguage(languageIndex == 0 ? "English" : languageIndex == 1 ? "Portuguese" : "Japanese");
            }
        }
    }
}

#endif