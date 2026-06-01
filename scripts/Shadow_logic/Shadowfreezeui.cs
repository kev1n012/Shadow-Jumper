using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the shadow freeze HUD:
///   - Swaps a single button Image between 3 states (normal / pressed / empty)
///   - Shows the freeze countdown in one row of digit Images
///   - Shows the cooldown countdown in a second row of digit Images
/// </summary>
public class ShadowFreezeUI : MonoBehaviour
{
    [Header("Button Image")]
    [Tooltip("The single Image component used for the F button.")]
    public Image buttonImage;
    public Sprite buttonNormal;   // F button, idle
    public Sprite buttonPressed;  // F button, visually pressed
    public Sprite buttonEmpty;    // Empty outline, shown during cooldown

    [Header("Digit Sprites (0 - 9)")]
    [Tooltip("Index 0 = '0', index 9 = '9'. Shared by both rows.")]
    public Sprite[] digitSprites = new Sprite[10];

    [Header("Freeze Countdown Row")]
    [Tooltip("The Image GameObjects that show the freeze timer (e.g. '5', '4' ...).")]
    public Image[] freezeDigitImages;

    [Header("Cooldown Countdown Row")]
    [Tooltip("The Image GameObjects that show the cooldown timer (e.g. '3', '2' ...).")]
    public Image[] cooldownDigitImages;

    [Tooltip("Hide leading zeros in both rows.")]
    public bool hideLeadingZeros = true;

    // -------------------------------------------------------------------

    private void Start()
    {
        ShowReady();
    }

    /// <summary>Called by ShadowCaster2DCreator when freeze begins.</summary>
    public void OnFreezeStart()
    {
        SetButtonSprite(buttonPressed);
        SetRowVisible(freezeDigitImages, true);
        SetRowVisible(cooldownDigitImages, false);
    }

    /// <summary>Called every frame while frozen.</summary>
    public void UpdateFreezeTimer(float secondsRemaining)
    {
        SetDigits(freezeDigitImages, Mathf.CeilToInt(Mathf.Max(secondsRemaining, 0f)));
    }

    /// <summary>Called by ShadowCaster2DCreator when cooldown begins.</summary>
    public void OnCooldownStart()
    {
        SetButtonSprite(buttonEmpty);
        SetRowVisible(freezeDigitImages, false);
        SetRowVisible(cooldownDigitImages, true);
    }

    /// <summary>Called every frame while on cooldown.</summary>
    public void UpdateCooldownTimer(float secondsRemaining)
    {
        SetDigits(cooldownDigitImages, Mathf.CeilToInt(Mathf.Max(secondsRemaining, 0f)));
    }

    /// <summary>Called when the cooldown finishes and the ability is ready.</summary>
    public void ShowReady()
    {
        SetButtonSprite(buttonNormal);
        SetRowVisible(freezeDigitImages, false);
        SetRowVisible(cooldownDigitImages, false);
    }

    // -------------------------------------------------------------------

    private void SetButtonSprite(Sprite sprite)
    {
        if (buttonImage != null && sprite != null)
            buttonImage.sprite = sprite;
    }

    private void SetRowVisible(Image[] row, bool visible)
    {
        if (row == null) return;
        foreach (Image img in row)
            if (img != null) img.enabled = visible;
    }

    private void SetDigits(Image[] row, int value)
    {
        if (row == null || row.Length == 0) return;
        if (digitSprites == null || digitSprites.Length < 10) return;

        string str = value.ToString().PadLeft(row.Length, '0');
        int numSlots = row.Length;

        if (str.Length > numSlots)
            str = new string('9', numSlots);

        bool leadingZero = true;

        for (int i = 0; i < numSlots; i++)
        {
            if (row[i] == null) continue;

            int digit = str[i] - '0';
            row[i].sprite = digitSprites[digit];

            if (hideLeadingZeros && leadingZero && digit == 0 && i < numSlots - 1)
                row[i].enabled = false;
            else
            {
                row[i].enabled = true;
                leadingZero = false;
            }
        }
    }
}