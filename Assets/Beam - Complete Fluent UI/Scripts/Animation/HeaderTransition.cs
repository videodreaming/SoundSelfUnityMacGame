using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.Beam
{
    public class HeaderTransition : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private Animator animator;
        [SerializeField] private Image filler;
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform contentHelper;
        [SerializeField] private TextMeshProUGUI helperText;
        [SerializeField] private TextMeshProUGUI contentText;

        [Header("Settings")]
        [SerializeField] private UpdateMode updateMode = UpdateMode.UnscaledTime;

        // Helpers
        float cachedStateLength = 0.5f;

        public enum UpdateMode { DeltaTime, UnscaledTime }

        void Awake()
        {
            cachedStateLength = BeamUIInternalTools.GetAnimatorClipLength(animator, "PanelHeader_Go");
            animator.enabled = false;
        }

        public void DoTransition(string newText)
        {
            if (filler.fillAmount < 0.25f)
            {
                helperText.text = contentText.text;
                contentText.text = newText;

                animator.enabled = true;
                animator.Play("Init");

                StopCoroutine("SetFillerWidth");
                StartCoroutine("SetFillerWidth");
            }

            else if (helperText.text.Length < newText.Length)
            {
                contentText.text = newText;
              
                StopCoroutine("SetFillerWidtWhilePlaying");
                StartCoroutine("SetFillerWidtWhilePlaying");
            }

            else
            {
                contentText.text = newText;
            }
        }

        IEnumerator SetFillerWidth()
        {
            StopCoroutine("DisableAnimator");
            StopCoroutine("SetFillerWidtWhilePlaying");

            if (updateMode == UpdateMode.UnscaledTime) { yield return new WaitForSecondsRealtime(0.01f); }
            else { yield return new WaitForSeconds(0.01f); }

            if (content.sizeDelta.x > contentHelper.sizeDelta.x) { filler.rectTransform.sizeDelta = new Vector2(content.sizeDelta.x, filler.rectTransform.sizeDelta.y); }
            else { filler.rectTransform.sizeDelta = new Vector2(contentHelper.sizeDelta.x, filler.rectTransform.sizeDelta.y); }

            animator.Play("Go");
            StartCoroutine("DisableAnimator");
        }

        IEnumerator SetFillerWidtWhilePlaying()
        {
            StopCoroutine("DisableAnimator");
            StopCoroutine("SetFillerWidth");

            if (updateMode == UpdateMode.UnscaledTime) { yield return new WaitForSecondsRealtime(0.01f); }
            else { yield return new WaitForSeconds(0.01f); }

            filler.rectTransform.sizeDelta = new Vector2(content.sizeDelta.x, filler.rectTransform.sizeDelta.y);
        }

        IEnumerator DisableAnimator()
        {
            if (updateMode == UpdateMode.UnscaledTime) { yield return new WaitForSecondsRealtime(cachedStateLength); }
            else { yield return new WaitForSeconds(cachedStateLength); }

            animator.enabled = false;
        }
    }
}