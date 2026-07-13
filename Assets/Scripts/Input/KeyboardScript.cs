// Reconstructed KeyboardScript - handles on-screen keyboard input
// Original: KeyboardScript (TypeDefIndex: 6217)

using UnityEngine;
using UnityEngine.UI;

namespace Chess3D.Input
{
    /// <summary>
    /// Handles the on-screen keyboard for name entry.
    /// </summary>
    public class KeyboardScript : MonoBehaviour
    {
        public InputField targetInputField;
        public GameObject keyboardPanel;

        public void OnKeyPressed(string key)
        {
            if (targetInputField == null) return;

            switch (key)
            {
                case "BACK":
                    if (targetInputField.text.Length > 0)
                        targetInputField.text = targetInputField.text.Substring(0, targetInputField.text.Length - 1);
                    break;
                case "ENTER":
                    HideKeyboard();
                    break;
                case "SPACE":
                    targetInputField.text += " ";
                    break;
                case "SHIFT":
                    // Toggle shift state
                    break;
                default:
                    targetInputField.text += key;
                    break;
            }
        }

        public void ShowKeyboard(InputField target)
        {
            targetInputField = target;
            if (keyboardPanel != null)
                keyboardPanel.SetActive(true);
        }

        public void HideKeyboard()
        {
            if (keyboardPanel != null)
                keyboardPanel.SetActive(false);
        }
    }
}
