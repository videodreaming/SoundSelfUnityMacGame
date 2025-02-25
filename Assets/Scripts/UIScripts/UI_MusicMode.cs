using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class UI_MusicMode : MonoBehaviour
{
    public MusicSystem1 musicSystem1;
    public TextMeshProUGUI musicModeText;

    // Update is called once per frame
    void Update()
    {
        musicModeText.text = musicSystem1.currentMusicMode.ToString();
    }
}
