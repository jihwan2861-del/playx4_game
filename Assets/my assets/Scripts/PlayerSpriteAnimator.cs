using UnityEngine;

/// <summary>
/// Simple sprite-frame animator for player idle/run previews.
/// </summary>
public class PlayerSpriteAnimator : MonoBehaviour
{
    [Header("Idle Animation Frames")]
    public Sprite[] idleFrames;

    [Header("Run Animation Frames")]
    public Sprite[] runFrames;

    [Header("Animation Speed")]
    [Tooltip("Seconds between sprite frames. Lower values play faster.")]
    public float frameInterval = 0.15f;

    private readonly int[] playOrder = { 0, 1, 0, 2 };

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentFrame;
    private int playIndex;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        bool isMoving = Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f;

        timer += Time.deltaTime;
        if (timer < frameInterval)
        {
            return;
        }

        timer = 0f;
        playIndex = (playIndex + 1) % playOrder.Length;
        currentFrame = playOrder[playIndex];

        Sprite[] frames = isMoving ? runFrames : idleFrames;
        if (frames != null && frames.Length > currentFrame && frames[currentFrame] != null)
        {
            spriteRenderer.sprite = frames[currentFrame];
        }
    }
}
