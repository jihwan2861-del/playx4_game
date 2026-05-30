using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mirrors an animated prefab's SpriteRenderer into a UI Image for the workshop preview.
/// </summary>
public class UICharacterPreview : MonoBehaviour
{
    [Header("Preview Prefab")]
    [Tooltip("Prefab used for the workshop preview. Add a SpriteRenderer and Animator to this prefab.")]
    public GameObject playerShipPrefab;

    [Tooltip("Optional override controller for the preview prefab's Animator.")]
    public RuntimeAnimatorController previewAnimatorController;

    [Tooltip("Optional state name to play as soon as the preview is created.")]
    public string previewAnimatorState;

    [Header("Preview Safety")]
    [Tooltip("Disable gameplay scripts on the preview instance while leaving SpriteRenderer and Animator active.")]
    public bool disableGameplayBehaviours = true;

    private Image uiImage;
    private GameObject previewInstance;
    private SpriteRenderer targetSpriteRenderer;
    private Animator previewAnimator;

    private void Start()
    {
        uiImage = GetComponent<Image>();
        LoadCombatPlayerPreview();
    }

    private void LateUpdate()
    {
        if (targetSpriteRenderer == null || uiImage == null || targetSpriteRenderer.sprite == null)
        {
            return;
        }

        uiImage.sprite = targetSpriteRenderer.sprite;
        uiImage.color = Color.white;
        uiImage.enabled = true;
    }

    private void LoadCombatPlayerPreview()
    {
        GameObject prefab = playerShipPrefab;

        if (prefab == null)
        {
            UseExistingPlayerSprite();
            if (targetSpriteRenderer != null)
            {
                return;
            }
        }

        if (prefab != null)
        {
            previewInstance = Instantiate(prefab);
            previewInstance.name = "ShipPreviewInstance";
            previewInstance.transform.position = new Vector3(-9999f, -9999f, 0f);

            if (disableGameplayBehaviours)
            {
                DisableGameplayBehaviours(previewInstance);
            }

            targetSpriteRenderer = previewInstance.GetComponent<SpriteRenderer>();
            if (targetSpriteRenderer == null)
            {
                targetSpriteRenderer = previewInstance.GetComponentInChildren<SpriteRenderer>(true);
            }

            SetupPreviewAnimator();
        }

        if (targetSpriteRenderer == null)
        {
            UseHubPlayerSprite();
        }
    }

    private void SetupPreviewAnimator()
    {
        if (previewInstance == null)
        {
            return;
        }

        previewAnimator = previewInstance.GetComponentInChildren<Animator>(true);

        if (previewAnimator == null && previewAnimatorController != null)
        {
            GameObject animatorTarget = targetSpriteRenderer != null ? targetSpriteRenderer.gameObject : previewInstance;
            previewAnimator = animatorTarget.AddComponent<Animator>();
        }

        if (previewAnimator == null)
        {
            return;
        }

        if (previewAnimatorController != null)
        {
            previewAnimator.runtimeAnimatorController = previewAnimatorController;
        }

        previewAnimator.enabled = true;
        previewAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (!string.IsNullOrEmpty(previewAnimatorState))
        {
            previewAnimator.Play(previewAnimatorState, 0, 0f);
        }
    }

    private void UseExistingPlayerSprite()
    {
        PlayerMoving existingPlayer = FindObjectOfType<PlayerMoving>();
        if (existingPlayer == null)
        {
            return;
        }

        targetSpriteRenderer = existingPlayer.GetComponent<SpriteRenderer>();
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = existingPlayer.GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    private void UseHubPlayerSprite()
    {
        HubPlayerMovement hubPlayer = FindObjectOfType<HubPlayerMovement>();
        if (hubPlayer == null)
        {
            return;
        }

        targetSpriteRenderer = hubPlayer.GetComponent<SpriteRenderer>();
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = hubPlayer.GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    private void DisableGameplayBehaviours(GameObject obj)
    {
        MonoBehaviour[] behaviours = obj.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null && behaviour != this)
            {
                behaviour.enabled = false;
            }
        }

        Rigidbody2D[] rigidbodies = obj.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D rb in rigidbodies)
        {
            rb.simulated = false;
        }

        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
    }
}
