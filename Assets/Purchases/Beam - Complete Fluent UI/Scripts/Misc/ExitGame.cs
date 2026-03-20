using UnityEngine;

namespace Michsky.UI.Beam
{
    public class ExitGame : MonoBehaviour
    {
        public void Exit() 
        { 
            Application.Quit();
#if UNITY_EDITOR
            Debug.Log("<b>[Beam UI]</b> Exit function works in builds only.");
#endif
        }
    }
}