using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's height (Y position as integer) on the HUD
/// using digit sprites from an Aseprite sprite sheet (one frame per digit 0-9).
/// </summary>
public class HeightDisplay : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("The Transform of your player character.")]
    public Transform player;

    [Tooltip("Y position that counts as height 0 (e.g. your ground level).")]
    public float groundY = 0f;

    [Header("Digit Sprites (0 - 9)")]
    [Tooltip("Drag in your 10 digit sprites here in order: index 0 = '0', index 1 = '1', ... index 9 = '9'.")]
    public Sprite[] digitSprites = new Sprite[10];

    [Header("HUD Digit Images")]
    [Tooltip("Drag in your UI Image components here, left to right (most significant digit first).")]
    public Image[] digitImages;

    [Tooltip("Hide leading zero images (e.g. '007' becomes '  7').")]
    public bool hideLeadingZeros = true;

    // ---------------------------------------------------------------

    private int lastHeight = int.MinValue;

    void Update()
    {
        if (player == null) return;

        int height = Mathf.FloorToInt(player.position.y - groundY);
        height = Mathf.Max(height, 0); // clamp to non-negative

        if (height == lastHeight) return; // no update needed
        lastHeight = height;

        UpdateDigits(height);
    }

    void UpdateDigits(int height)
    {
        if (digitImages == null || digitImages.Length == 0) return;

        string heightStr = height.ToString();
        int numDigits = digitImages.Length;

        // Pad left with zeros to fill all digit slots
        heightStr = heightStr.PadLeft(numDigits, '0');

        // If the number is too large for the digit slots, clamp display to max value
        if (heightStr.Length > numDigits)
            heightStr = new string('9', numDigits);

        bool leadingZero = true;

        for (int i = 0; i < numDigits; i++)
        {
            if (digitImages[i] == null) continue;

            int digit = heightStr[i] - '0'; // char to int

            // Assign the correct sprite
            digitImages[i].sprite = digitSprites[digit];

            // Handle leading zero visibility
            if (hideLeadingZeros && leadingZero && digit == 0 && i < numDigits - 1)
            {
                digitImages[i].enabled = false;
            }
            else
            {
                digitImages[i].enabled = true;
                leadingZero = false;
            }
        }
    }
}