using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.Beam
{
    public class SidebarIndividual : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        // Resources
        public ContextMenu contextMenu;
        [SerializeField] private Image profileImage;
        [SerializeField] private TextMeshProUGUI profileText;
        [SerializeField] private CanvasGroupAnimator contentGroup;
        [SerializeField] private CanvasGroup highlightCG;
        [SerializeField] private List<GameObject> onlineIndicators = new List<GameObject>();
        [SerializeField] private List<GameObject> awayIndicators = new List<GameObject>();
        [SerializeField] private List<GameObject> offlineIndicators = new List<GameObject>();
        [SerializeField] private List<GameObject> customIndicators = new List<GameObject>();

        // Settings
        [SerializeField] private bool setStateOnEnable = true;
        public bool isInteractable = true;
        public bool useSounds = true;
        [Range(1, 15)] public float fadingMultiplier = 8;
        public IndividualState individualState = new IndividualState();

        // Events
        public UnityEvent onClick = new UnityEvent();

        public enum IndividualState { Online, Away, Offline, Custom }

        void Start()
        {
            if (UIManagerAudio.instance == null) { useSounds = false; }
            if (highlightCG == null)
            {
                highlightCG = new GameObject().AddComponent<CanvasGroup>();
                highlightCG.gameObject.AddComponent<RectTransform>();
                highlightCG.transform.SetParent(transform);
                highlightCG.gameObject.name = "Highlight";
            }
            if (GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }
        }

        void OnEnable()
        {
            if (setStateOnEnable) { SetState(individualState); }
            if (highlightCG != null) { highlightCG.alpha = 0; }
        }

        public void SetProfilePicture(Sprite pic)
        {
            profileImage.sprite = pic;
        }

        public void SetProfileName(string name)
        {
            profileText.text = name;
        }

        public void SetCustomStatus(string newStatus)
        {
            foreach (GameObject go in customIndicators) 
            {
                if (go == null) { continue; }
                else if (go.GetComponent<TextMeshProUGUI>() != null)
                {
                    go.GetComponent<TextMeshProUGUI>().text = newStatus;
                    break;
                }
            }
        }

        public void SetState(IndividualState state)
        {
            individualState = state;

            if (individualState == IndividualState.Online)
            {
                foreach (GameObject go in onlineIndicators) { go.SetActive(true); }
                foreach (GameObject go in awayIndicators) { go.SetActive(false); }
                foreach (GameObject go in offlineIndicators) { go.SetActive(false); }
                foreach (GameObject go in customIndicators) { go.SetActive(false); }
            }

            else if (individualState == IndividualState.Away)
            {
                foreach (GameObject go in onlineIndicators) { go.SetActive(false); }
                foreach (GameObject go in awayIndicators) { go.SetActive(true); }
                foreach (GameObject go in offlineIndicators) { go.SetActive(false); }
                foreach (GameObject go in customIndicators) { go.SetActive(false); }
            }

            else if (individualState == IndividualState.Offline)
            {
                foreach (GameObject go in onlineIndicators) { go.SetActive(false); }
                foreach (GameObject go in awayIndicators) { go.SetActive(false); }
                foreach (GameObject go in offlineIndicators) { go.SetActive(true); }
                foreach (GameObject go in customIndicators) { go.SetActive(false); }
            }

            else if (individualState == IndividualState.Custom)
            {
                foreach (GameObject go in onlineIndicators) { go.SetActive(false); }
                foreach (GameObject go in awayIndicators) { go.SetActive(false); }
                foreach (GameObject go in offlineIndicators) { go.SetActive(false); }
                foreach (GameObject go in customIndicators) { go.SetActive(true); }
            }
        }

        public void ExpandItem()
        {
            if (Application.isPlaying && gameObject.activeInHierarchy) { contentGroup.FadeIn(); }
            else { contentGroup.gameObject.SetActive(true); }
        }

        public void MinimizeItem()
        {
            if (Application.isPlaying && gameObject.activeInHierarchy) 
            { 
                contentGroup.FadeOut();
                if (contextMenu != null && contextMenu.isOn) { contextMenu.Close(); }
            }

            else
            { 
                contentGroup.gameObject.SetActive(false);
            }
        }

        public void Interactable(bool value)
        {
            isInteractable = value;
            if (!gameObject.activeInHierarchy) { return; }
            StartCoroutine("SetNormal");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            if (useSounds) { UIManagerAudio.instance.audioSource.PlayOneShot(UIManagerAudio.instance.UIManagerAsset.clickSound); }
            if (contextMenu != null) { contextMenu.Open(); }

            onClick.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            if (useSounds) { UIManagerAudio.instance.audioSource.PlayOneShot(UIManagerAudio.instance.UIManagerAsset.hoverSound); }

            StartCoroutine("SetHighlight");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine("SetNormal");
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable) { return; }
            if (useSounds) { UIManagerAudio.instance.audioSource.PlayOneShot(UIManagerAudio.instance.UIManagerAsset.hoverSound); }

            StartCoroutine("SetHighlight");
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable || highlightCG == null)
                return;

            StartCoroutine("SetNormal");
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isInteractable) { return; }
            if (useSounds) { UIManagerAudio.instance.audioSource.PlayOneShot(UIManagerAudio.instance.UIManagerAsset.clickSound); }

            onClick.Invoke();
        }

        IEnumerator SetNormal()
        {
            StopCoroutine("SetHighlight");

            while (highlightCG.alpha > 0.01f)
            {
                highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                yield return null;
            }

            highlightCG.alpha = 0;
        }

        IEnumerator SetHighlight()
        {
            StopCoroutine("SetNormal");

            while (highlightCG.alpha < 0.99f)
            {
                highlightCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                yield return null;
            }

            highlightCG.alpha = 1;
        }
    }
}