using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VersionDisplay : MonoBehaviour
{
    private TMP_Text versionText;
    // Start is called before the first frame update
    void Start()
    {
        versionText = GetComponent<TMP_Text>();
        versionText.text = "Version: " + Application.version;
    }

}
