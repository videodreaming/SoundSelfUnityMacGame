using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace Michsky.UI.Beam
{
    public class ServerBrowserItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        // Content
        public string serverName = "Server Name";
        public int serverPing = 30;
        [SerializeField] private int badPingThreshold = 99;
        [SerializeField] private int normalPingThreshold = 60;
        [SerializeField] private int goodPingThreshold = 20;
        public int currentPlayers = 1;
        public int maxPlayers = 4;
        public bool isFavorite = false;
        public bool isLocked = false;

        // Resources
        [SerializeField] private CanvasGroup highlightCG;
        public RectTransform objectRect;
        [SerializeField] private GameObject detailsParent;
        public ButtonManager connectButton;
        [SerializeField] private TextMeshProUGUI serverNameObj;
        [SerializeField] private TextMeshProUGUI playersObj;
        [SerializeField] private GameObject favoriteObj;
        [SerializeField] private GameObject notFavoriteObj;
        [SerializeField] private GameObject lockedObj;
        [SerializeField] private GameObject notLockedObj;
        [SerializeField] private TextMeshProUGUI pingTextObj;
        [SerializeField] private Image pingIconObj;
        [SerializeField] private Sprite badPingIcon;
        [SerializeField] private Sprite normalPingIcon;
        [SerializeField] private Sprite goodPingIcon;

        // Settings
        public bool isInteractable = true;
        [SerializeField] private bool bypassConnectLimitations = false;
        [SerializeField] private bool useSounds = true;
        [SerializeField] [Range(1, 15)] private float fadingMultiplier = 8;
        public bool useUINavigation = false;
        public Navigation.Mode navigationMode = Navigation.Mode.Automatic;
        public GameObject selectOnUp;
        public GameObject selectOnDown;
        public GameObject selectOnLeft;
        public GameObject selectOnRight;
        public bool wrapAround = false;

        // Animation
        [SerializeField] private AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        [SerializeField] [Range(0.5f, 10)] private float curveSpeed = 3;
        [SerializeField] private float normalHeight = 50;
        [SerializeField] private float expandedHeight = 140;

        // Events
        public UnityEvent onClick = new UnityEvent();
        public UnityEvent onConnect = new UnityEvent();

        // Helpers
        Button targetButton;
        [HideInInspector] public bool isExpanded;

        void Start()
        {
            if (ControllerManager.instance != null) { ControllerManager.instance.serverItems.Add(this); }
            if (UIManagerAudio.instance == null) { useSounds = false; }
            if (objectRect == null) { objectRect = gameObject.GetComponent<RectTransform>(); }
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

            objectRect.sizeDelta = new Vector2(objectRect.sizeDelta.x, normalHeight);
            detailsParent.SetActive(false);
            connectButton.onClick.AddListener(onConnect.Invoke);

            UpdateUI();
        }

        void OnEnable()
        {
            if (highlightCG != null) { highlightCG.alpha = 0; }
            if (Application.isPlaying && useUINavigation) { AddUINavigation(); }
            else if (Application.isPlaying && !useUINavigation && targetButton == null)
            {
                if (gameObject.GetComponent<Button>() == null) { targetButton = gameObject.AddComponent<Button>(); }
                else { targetButton = GetComponent<Button>(); }

                targetButton.transition = Selectable.Transition.None;
            }
        }

        public void Animate()
        {
            if (isExpanded) { Minimize(); }
            else { Expand(); }
        }

        public void Expand()
        {
            StartCoroutine("DoExpand");
            isExpanded = true;
        }

        public void Minimize()
        {
            StartCoroutine("DoMinimize");
            isExpanded = false;
        }

        public void UpdateUI()
        {
            SetName(serverName);
            SetPing(serverPing);
            SetFavorite(isFavorite);
            SetLocked(isLocked);
            SetPlayers(currentPlayers);
            UpdateButtonState();
        }

        public void SetName(string newName)
        {
            serverName = newName;
            serverNameObj.text = serverName;
        }

        public void SetPlayers(int currentPlayerCount)
        {
            currentPlayers = currentPlayerCount;
            playersObj.text = $"{currentPlayers}/{maxPlayers}";
        }

        public void SetPing(int newPing)
        {
            serverPing = newPing;

            // Set text
            if (pingTextObj != null) { pingTextObj.text = serverPing.ToString(); }

            // Check for icons
            if (pingIconObj == null)
                return;

            // Set icon
            if (serverPing > badPingThreshold) { pingIconObj.sprite = badPingIcon; }
            else if (serverPing < normalPingThreshold && serverPing > goodPingThreshold) { pingIconObj.sprite = normalPingIcon; }
            else if (serverPing < goodPingThreshold) { pingIconObj.sprite = goodPingIcon; }
        }

        public void SetFavorite(bool favorite)
        {
            isFavorite = favorite;

            if (isFavorite) { notFavoriteObj.SetActive(false); favoriteObj.SetActive(true); }
            else { notFavoriteObj.SetActive(true); favoriteObj.SetActive(false); }
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;

            if (isLocked) { notLockedObj.SetActive(false); lockedObj.SetActive(true); }
            else { notLockedObj.SetActive(true); lockedObj.SetActive(false); }
        }

        public void UpdateButtonState()
        {
            if (bypassConnectLimitations)
                return;

            if (currentPlayers >= maxPlayers || isLocked) { connectButton.Interactable(false); }
            else if (currentPlayers < maxPlayers && !isLocked) { connectButton.Interactable(true); }
        }

        public void Interactable(bool value)
        {
            isInteractable = value;
            if (!gameObject.activeInHierarchy) { return; }
            StartCoroutine("SetNormal");
        }

        public void AddUINavigation()
        {
            if (targetButton == null)
            {
                if (gameObject.GetComponent<Button>() == null) { targetButton = gameObject.AddComponent<Button>(); }
                else { targetButton = GetComponent<Button>(); }

                targetButton.transition = Selectable.Transition.None;
            }

            if (targetButton.navigation.mode == navigationMode)
                return;

            Navigation customNav = new Navigation();
            customNav.mode = navigationMode;

            if (navigationMode == Navigation.Mode.Vertical || navigationMode == Navigation.Mode.Horizontal) { customNav.wrapAround = wrapAround; }
            else if (navigationMode == Navigation.Mode.Explicit) { StartCoroutine("InitUINavigation", customNav); return; }

            targetButton.navigation = customNav;
        }

        public void DisableUINavigation()
        {
            if (targetButton != null)
            {
                Navigation customNav = new Navigation();
                Navigation.Mode navMode = Navigation.Mode.None;
                customNav.mode = navMode;
                targetButton.navigation = customNav;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
            if (useSounds) { UIManagerAudio.instance.audioSource.PlayOneShot(UIManagerAudio.instance.UIManagerAsset.clickSound); }

            Animate();
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

            Animate();
            onClick.Invoke();
        }

        IEnumerator DoExpand()
        {
            StopCoroutine("DoMinimize");

            float elapsedTime = 0;
            detailsParent.SetActive(true);

            Vector2 startPos = objectRect.sizeDelta;
            Vector2 endPos = new Vector2(objectRect.sizeDelta.x, expandedHeight);

            while (objectRect.sizeDelta.y < expandedHeight - 0.1f)
            {
                elapsedTime += Time.deltaTime;
                objectRect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));
                yield return null;
            }

            objectRect.sizeDelta = endPos;
        }

        IEnumerator DoMinimize()
        {
            StopCoroutine("DoExpand");

            float elapsedTime = 0;

            Vector2 startPos = objectRect.sizeDelta;
            Vector2 endPos = new Vector2(objectRect.sizeDelta.x, normalHeight);

            while (objectRect.sizeDelta.y > normalHeight + 0.1f)
            {
                elapsedTime += Time.deltaTime;
                objectRect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));
                yield return null;
            }

            objectRect.sizeDelta = endPos;
            detailsParent.SetActive(false);
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

        IEnumerator InitUINavigation(Navigation nav)
        {
            yield return new WaitForSecondsRealtime(0.1f);

            if (selectOnUp != null) { nav.selectOnUp = selectOnUp.GetComponent<Selectable>(); }
            if (selectOnDown != null) { nav.selectOnDown = selectOnDown.GetComponent<Selectable>(); }
            if (selectOnLeft != null) { nav.selectOnLeft = selectOnLeft.GetComponent<Selectable>(); }
            if (selectOnRight != null) { nav.selectOnRight = selectOnRight.GetComponent<Selectable>(); }

            targetButton.navigation = nav;
        }
    }
}