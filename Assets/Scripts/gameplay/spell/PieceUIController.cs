using UnityEngine;
using UnityEngine.UI;

public class PieceUIController : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider healthSlider;
    public Slider manaSlider;

    [Header("跟随设置")]
    public Transform target; // UI要跟随的棋子
    public Vector3 offset = new Vector3(0, 1.2f, 0); // UI相对于棋子的位置偏移

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    // 使用 LateUpdate 可以防止UI跟随棋子移动时产生抖动
    void LateUpdate()
    {
        if (target != null && mainCamera != null)
        {
            // 直接设置UI的世界坐标，并让它始终朝向摄像机
            transform.position = target.position + offset;
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    // 更新血量显示
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider != null && maxHealth > 0)
        {
            // 只有在不满血时才显示血条
            healthSlider.gameObject.SetActive(currentHealth < maxHealth);
            healthSlider.value = currentHealth / maxHealth;
        }
    }

    // 更新法力显示
    public void UpdateMana(float currentMana, float maxMana)
    {
        if (manaSlider != null && maxMana > 0)
        {
            // 只有在有法力时才显示法力条
            manaSlider.gameObject.SetActive(currentMana > 0);
            manaSlider.value = currentMana / maxMana;
        }
    }
}