using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Michsky.UI.Beam
{
    public class ContextMenu : MonoBehaviour
    {
        // Content
        public List<ContextMenuItem> menuItems = new List<ContextMenuItem>();

        // Resources
        public RectTransform contextRect;
        public UIPopup contextPopup;
        [SerializeField] private GameObject buttonPreset;
        [SerializeField] private GameObject separatorPreset;
        [SerializeField] private Transform itemParent;
        [SerializeField] private GameObject boundTrigger;

        // Helpers
        float cachedPosY;
        public bool isOn;

        public enum ItemType { Button, Separator }

        [System.Serializable]
        public class ContextMenuItem
        {
            public string itemText = "Item Text";
            public string localizationKey;
            public Sprite itemIcon;
            public ItemType itemType;
            public UnityEvent onClick = new UnityEvent();
        }

        void Start()
        {
            if (contextRect == null || contextPopup == null)
                return;

            CreateItems();
            cachedPosY = contextRect.anchoredPosition.y;

            if (boundTrigger != null)
            {
                boundTrigger.transform.SetParent(transform, false);
                boundTrigger.transform.SetAsLastSibling();
                boundTrigger.gameObject.SetActive(false);
            }

            contextPopup.transform.SetAsLastSibling();
            contextPopup.gameObject.SetActive(false);
        }

        public void CreateItems()
        {
            if (menuItems.Count == 0)
                return;

            foreach (Transform child in itemParent) { Destroy(child.gameObject); }
            for (int i = 0; i < menuItems.Count; ++i)
            {
                if (menuItems[i].itemType == ItemType.Button) { CreateButton(menuItems[i].itemIcon, menuItems[i].itemText, menuItems[i].localizationKey, menuItems[i].onClick); }
                else if (menuItems[i].itemType == ItemType.Separator) { CreateSeparator(); }
            }
        }

        public void CreateButton(Sprite icon, string text, string localizationKey = null)
        {
            GameObject go = Instantiate(buttonPreset, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(itemParent, false);

            ContextMenuObject goItem = go.GetComponent<ContextMenuObject>();
            goItem.button.buttonText = text;
            goItem.button.buttonIcon = icon;
            goItem.button.onClick.AddListener(Close);

            LocalizedObject tempLoc = goItem.GetComponent<LocalizedObject>();
            if (!string.IsNullOrEmpty(localizationKey) && tempLoc != null && tempLoc.CheckLocalizationStatus())
            {
                tempLoc.localizationKey = localizationKey;
                goItem.button.buttonText = tempLoc.GetKeyOutput(tempLoc.localizationKey);
                tempLoc.onLanguageChanged.AddListener(delegate
                {
                    goItem.button.buttonText = tempLoc.GetKeyOutput(tempLoc.localizationKey);
                    goItem.button.UpdateUI();
                });
            }

            goItem.button.UpdateUI();
        }

        public void CreateButton(Sprite icon, string text, string localizationKey, UnityEvent events)
        {
            GameObject go = Instantiate(buttonPreset, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(itemParent, false);

            ContextMenuObject goItem = go.GetComponent<ContextMenuObject>();
            goItem.button.buttonText = text;
            goItem.button.buttonIcon = icon;
            goItem.button.onClick.AddListener(Close);
            goItem.button.onClick.AddListener(events.Invoke);

            LocalizedObject tempLoc = goItem.GetComponent<LocalizedObject>();
            if (!string.IsNullOrEmpty(localizationKey) && tempLoc != null && tempLoc.CheckLocalizationStatus())
            {
                tempLoc.localizationKey = localizationKey;
                goItem.button.buttonText = tempLoc.GetKeyOutput(tempLoc.localizationKey);
                tempLoc.onLanguageChanged.AddListener(delegate
                {
                    goItem.button.buttonText = tempLoc.GetKeyOutput(tempLoc.localizationKey);
                    goItem.button.UpdateUI();
                });
            }

            goItem.button.UpdateUI();
        }

        public void CreateSeparator()
        {
            GameObject go = Instantiate(separatorPreset, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(itemParent, false);
        }

        public void Open()
        {
            if (isOn) { return; }
            if (boundTrigger != null) { boundTrigger.SetActive(true); }

            if (IsContextMenuVisible())
            {
                contextRect.anchorMin = new Vector2(0, 0);
                contextRect.anchorMax = new Vector2(1, 0);
                contextRect.pivot = new Vector2(contextRect.pivot.x, 1);
                contextRect.anchoredPosition = new Vector2(0, cachedPosY);
            }

            else
            {
                contextRect.anchorMin = new Vector2(0, 1);
                contextRect.anchorMax = new Vector2(1, 1);
                contextRect.pivot = new Vector2(contextRect.pivot.x, 0);
                contextRect.anchoredPosition = new Vector2(0, -cachedPosY);
            }

            isOn = true;
            contextPopup.PlayIn();
        }

        public void Close()
        {
            if (!isOn) { return; }
            if (boundTrigger != null) { boundTrigger.SetActive(false); }

            isOn = false;
            contextPopup.PlayOut();
        }

        bool IsContextMenuVisible()
        {
            Vector3[] v = new Vector3[4];
            contextRect.GetWorldCorners(v);
          
            float maxY = Mathf.Max(v[0].y, v[1].y, v[2].y, v[3].y);

            if (maxY < contextPopup.rectHelper.y) { return false; }
            else { return true; }
        }

        /*
        bool IsRectTransformVisible(RectTransform rectTransform)
        {
            Vector3[] v = new Vector3[4];
            rectTransform.GetWorldCorners(v);

            // Vertical
            float maxY = Mathf.Max(v[0].y, v[1].y, v[2].y, v[3].y);
            float minY = Mathf.Min(v[0].y, v[1].y, v[2].y, v[3].y);

            // Horizontal
            // float maxX = Mathf.Max (v [0].x, v [1].x, v [2].x, v [3].x);
            // float minX = Mathf.Min (v [0].x, v [1].x, v [2].x, v [3].x);

            if (maxY < contextRect.sizeDelta.y || minY > Screen.height - contextRect.sizeDelta.y) { return false; }
            else { return true; }
        }
        */
    }
}