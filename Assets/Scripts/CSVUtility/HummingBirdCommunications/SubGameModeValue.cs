using UnityEngine;
using TMPro;

/// <summary>Displays <see cref="CSVLoader.contentPack"/> on a TMP label. Class name kept for Unity scene/prefab script references.</summary>
public class SubGameModeValue : MonoBehaviour
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
            tmpText.text = CSVLoader.instance.contentPack;
        else if (csvWriter != null)
            tmpText.text = CSVWriter.contentPack;
    }
}
