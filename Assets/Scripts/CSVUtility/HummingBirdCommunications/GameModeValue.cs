using UnityEngine;
using TMPro;

public class GameModeValue : MonoBehaviour
{
    public CSVWriter csvWriter;
    private TMP_Text tmpText;

    void Start()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (tmpText == null)
            return;
        if (CSVLoader.instance != null)
            tmpText.text = CSVLoader.instance.gameMode;
        else if (csvWriter != null)
            tmpText.text = CSVWriter.gameMode;
    }
}
