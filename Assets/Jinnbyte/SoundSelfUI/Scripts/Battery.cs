using UnityEngine;
using UnityEngine.UI;
public class Battery : MonoBehaviour
{
    [SerializeField]
    private Image healthBar;
    [SerializeField]
    private Image chargingIndicator;
    public void SetHealth(float health)
    {
        healthBar.fillAmount = health;
    }
    public void SetCharging(bool isCharging)
    {
        if (chargingIndicator != null)
            chargingIndicator.gameObject.SetActive(isCharging);
    }
}
