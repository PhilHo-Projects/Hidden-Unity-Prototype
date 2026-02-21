using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class WordListImporter : EditorWindow
    {
        private string _jsonFilePath = "";
        private Core.WordListScriptableObject _wordListObject;

        [MenuItem("Tools/Word List Importer")]
        public static void ShowWindow()
        {
            GetWindow<WordListImporter>("Word List Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Import Words from JSON", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _jsonFilePath = EditorGUILayout.TextField("JSON File Path", _jsonFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                _jsonFilePath = EditorUtility.OpenFilePanel("Select JSON file", "", "json");
            }
            EditorGUILayout.EndHorizontal();

            _wordListObject = EditorGUILayout.ObjectField("Word List Object", _wordListObject, typeof(Core.WordListScriptableObject), false) as Core.WordListScriptableObject;

            if (GUILayout.Button("Import"))
            {
                ImportWords();
            }
        }

        private void ImportWords()
        {
            if (string.IsNullOrEmpty(_jsonFilePath) || _wordListObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select both a JSON file and a Word List Object.", "OK");
                return;
            }

            string jsonContent = File.ReadAllText(_jsonFilePath);
            WordList wordList = JsonUtility.FromJson<WordList>(jsonContent);

            _wordListObject.words = wordList.words;

            EditorUtility.SetDirty(_wordListObject);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", "Words imported successfully!", "OK");
        }

        [System.Serializable]
        private class WordList
        {
            public List<string> words;
        }
    }
}