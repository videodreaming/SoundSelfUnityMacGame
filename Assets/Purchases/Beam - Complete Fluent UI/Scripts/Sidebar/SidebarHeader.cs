using UnityEngine;

namespace Michsky.UI.Beam
{
    public class SidebarHeader : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private CanvasGroupAnimator headerContent;
        [SerializeField] private GameObject targetContent;
        [SerializeField] private GameObject expandButton;
        [SerializeField] private GameObject minimizeButton;

        public void ExpandItem()
        {
            if (Application.isPlaying && gameObject.activeInHierarchy) { headerContent.FadeIn(); }
            else { headerContent.gameObject.SetActive(true); }
        }

        public void MinimizeItem()
        {
            if (Application.isPlaying && gameObject.activeInHierarchy) { headerContent.FadeOut(); }
            else { headerContent.gameObject.SetActive(false); }
        }

        public void ExpandContent()
        {
            if (targetContent != null) { targetContent.SetActive(true); }
            if (expandButton != null) { expandButton.SetActive(false); }
            if (minimizeButton != null) { minimizeButton.SetActive(true); }  
        }

        public void MinimizeContent()
        {
            if (targetContent != null) { targetContent.SetActive(false); }
            if (expandButton != null) { expandButton.SetActive(true); }
            if (minimizeButton != null) { minimizeButton.SetActive(false); }
        }
    }
}