using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Michsky.UI.Beam
{
    public class SidebarAnimator : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // Resources
        public RectTransform targetRect;
        [SerializeField] private CanvasGroup background;
        [SerializeField] private GameObject boundTrigger;
        public List<SidebarHeader> headers = new List<SidebarHeader>();
        public List<SidebarIndividual> individuals = new List<SidebarIndividual>();

        // Settings
        public float targetWidth = 400;
        [SerializeField] [Range(0, 10)] private float curveSpeed = 4f;
        [SerializeField] [Range(1, 2)] private float fadingMultiplier = 1.5f;
        [SerializeField] private AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        public StartState startState = StartState.Closed;
        public InteractType interactType = InteractType.Hover;

        // Events
        public UnityEvent onOpen = new UnityEvent();
        public UnityEvent onClose = new UnityEvent();

        // Helpers
        public bool isOn;
        public float defaultWidth;

        public enum StartState { Closed, Open }
        public enum InteractType { Hover, Click, Hybrid }

        void Awake()
        {
            if (targetRect == null) { targetRect = GetComponent<RectTransform>(); }
            if (!isOn) { defaultWidth = targetRect.sizeDelta.x; }
            if (boundTrigger != null) { boundTrigger.SetActive(false); }
            if (GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }
        }

        void Start()
        {
            if (startState == StartState.Closed) { CloseInstant(false); }
            else { OpenInstant(false); }
        }

        public void Animate()
        {
            if (!isOn) { Open(); }
            else { Close(); }
        }

        public void Open()
        {
            if (isOn) { return; }
            if (interactType == InteractType.Click && boundTrigger != null) { boundTrigger.SetActive(true); }
        
            foreach (SidebarHeader sh in headers) { if (sh != null) { sh.ExpandItem(); } }
            foreach (SidebarIndividual si in individuals) { if (si != null) { si.ExpandItem(); } }
        
            StopCoroutine("DoExpand");
            StartCoroutine("DoExpand");

            isOn = true;
            onOpen.Invoke();
        }

        public void Close()
        {
            if (!isOn) { return; }
            if (interactType == InteractType.Click && boundTrigger != null) { boundTrigger.SetActive(false); }
           
            foreach (SidebarHeader sh in headers) { if (sh != null) { sh.MinimizeItem(); } }
            foreach (SidebarIndividual si in individuals) { if (si != null) { si.MinimizeItem(); } }

            StopCoroutine("DoMinimize");
            StartCoroutine("DoMinimize");

            isOn = false;
            onClose.Invoke();
        }

        public void OpenInstant(bool invokeEvents = true)
        {
            targetRect.sizeDelta = new Vector2(targetWidth, targetRect.sizeDelta.y);

            foreach (SidebarHeader sh in headers) { if (sh != null) { sh.ExpandItem(); } }
            foreach (SidebarIndividual si in individuals) { if (si != null) { si.ExpandItem(); } }

            if (background != null) { background.alpha = 1; }
            if (interactType == InteractType.Click && boundTrigger != null) { boundTrigger.SetActive(true); }
            if (Application.isPlaying && invokeEvents) { onOpen.Invoke(); }

            isOn = true;
        }

        public void CloseInstant(bool invokeEvents = true)
        {
            targetRect.sizeDelta = new Vector2(defaultWidth, targetRect.sizeDelta.y);

            foreach (SidebarHeader sh in headers) { if (sh != null) { sh.MinimizeItem(); } }
            foreach (SidebarIndividual si in individuals) { if (si != null) { si.MinimizeItem(); } }

            if (background != null) { background.alpha = 0; }
            if (interactType == InteractType.Click && boundTrigger != null) { boundTrigger.SetActive(false); }
            if (Application.isPlaying && invokeEvents) { onClose.Invoke(); }

            isOn = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (interactType == InteractType.Click || interactType == InteractType.Hybrid)
            {
                Animate();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (interactType == InteractType.Hover)
            {
                Open();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (interactType == InteractType.Hover || interactType == InteractType.Hybrid)
            {
                Close();
            }
        }

        IEnumerator DoExpand()
        {
            StopCoroutine("DoMinimize");
            float elapsedTime = 0;

            Vector2 startPos = targetRect.sizeDelta;
            Vector2 endPos = new Vector2(targetWidth, targetRect.sizeDelta.y);

            if (curveSpeed == 0) { targetRect.sizeDelta = endPos; }
            else
            {
                while (targetRect.sizeDelta.x < endPos.x - 0.1f)
                {
                    if (background.alpha < 1) { background.alpha += Time.unscaledDeltaTime * (curveSpeed * fadingMultiplier); }
                    targetRect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));
                    elapsedTime += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            targetRect.sizeDelta = endPos;
            if (background != null) { background.alpha = 1; }
        }

        IEnumerator DoMinimize()
        {
            StopCoroutine("DoExpand");
            float elapsedTime = 0;

            Vector2 startPos = new Vector2(targetRect.sizeDelta.x, targetRect.sizeDelta.y);
            Vector2 endPos = new Vector2(defaultWidth, targetRect.sizeDelta.y);

            if (curveSpeed == 0) { targetRect.sizeDelta = endPos; }
            else
            {
                while (targetRect.sizeDelta.x > endPos.x)
                {
                    if (background.alpha > 0) { background.alpha -= Time.unscaledDeltaTime * curveSpeed; }
                    targetRect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));
                    elapsedTime += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            targetRect.sizeDelta = endPos;
            if (background != null) { background.alpha = 0; }
        }
    }
}