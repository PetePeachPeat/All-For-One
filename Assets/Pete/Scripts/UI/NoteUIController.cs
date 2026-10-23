using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pete.Interaction;

namespace Pete.UI
{
    public class NoteUIController : MonoBehaviour
    {
        [Header("Note UI")]
        [SerializeField] private GameObject notePanel;
        [SerializeField] private TextMeshProUGUI noteTitleText;
        [SerializeField] private TextMeshProUGUI noteContentText;
        [SerializeField] private Button closeNoteButton;

        private void Start()
        {
            if (closeNoteButton != null)
                closeNoteButton.onClick.AddListener(CloseNote);

            if (notePanel != null) notePanel.SetActive(false);

            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnInspectStarted += HandleInspectStarted;
                CameraController.Instance.OnInspectEnded += HandleInspectEnded;
            }
        }

        private void OnDestroy()
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.OnInspectStarted -= HandleInspectStarted;
                CameraController.Instance.OnInspectEnded -= HandleInspectEnded;
            }
        }

        private void HandleInspectStarted(Data.InspectTargetType type)
        {
            if (notePanel != null)
            {
                notePanel.SetActive(type == Data.InspectTargetType.Note);
            }
        }

        private void HandleInspectEnded()
        {
            if (notePanel != null)
            {
                notePanel.SetActive(false);
            }
        }

        public void OpenNote()
        {
            if (notePanel != null) notePanel.SetActive(true);
        }

        public void CloseNote()
        {
            if (notePanel != null) notePanel.SetActive(false);
            if (CameraController.Instance != null && CameraController.Instance.CurrentInspectType == Data.InspectTargetType.Note)
            {
                CameraController.Instance.ExitInspect();
            }
        }
    }
}
