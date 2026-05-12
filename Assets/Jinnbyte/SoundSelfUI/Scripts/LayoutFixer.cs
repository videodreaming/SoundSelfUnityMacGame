using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayoutFixer : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame(); // Wait a frame to ensure all layout elements have done their initial pass, so the forced rebuild in OnEnable has an effect.
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}
