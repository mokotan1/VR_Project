using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRProject.Presentation.Common.UI;

namespace VRProject.Presentation.Interaction.Views
{
    /// <summary>
    /// Displays dialogue text in the VR UI.
    /// Supports streaming text display for real-time LLM output.
    /// </summary>
    public sealed class DialogueView : ViewBase
    {
        [Header("UI References")]
        [SerializeField] private Text playerTextField;
        [SerializeField] private Text aiTextField;
        [SerializeField] private GameObject listeningIndicator;
        [SerializeField] private Text errorTextField;

        private readonly StringBuilder _aiTextBuffer = new();

        public void SetPlayerText(string text)
        {
            if (playerTextField != null)
                playerTextField.text = text;
        }

        public void AppendAIText(string delta)
        {
            _aiTextBuffer.Append(delta);
            if (aiTextField != null)
                aiTextField.text = _aiTextBuffer.ToString();
        }

        public void FinalizeAIText(string fullText)
        {
            _aiTextBuffer.Clear();
            _aiTextBuffer.Append(fullText);
            if (aiTextField != null)
                aiTextField.text = fullText;
        }

        public void ShowListeningIndicator(bool show)
        {
            if (listeningIndicator != null)
                listeningIndicator.SetActive(show);
        }

        public void ShowError(string error)
        {
            if (errorTextField != null)
            {
                errorTextField.text = error;
                errorTextField.gameObject.SetActive(true);
            }
            Debug.LogWarning($"[DialogueView] {error}");
        }

        public void Clear()
        {
            _aiTextBuffer.Clear();
            if (playerTextField != null) playerTextField.text = "";
            if (aiTextField != null) aiTextField.text = "";
            if (errorTextField != null)
            {
                errorTextField.text = "";
                errorTextField.gameObject.SetActive(false);
            }
        }

        protected override void OnHide()
        {
            Clear();
        }
    }
}
