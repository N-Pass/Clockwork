using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [SerializeField] private RectTransform canvasRectTransform;
    private const string TEXT = "Text";
    private const string BACKGROUND = "Background";

    private RectTransform rectTransform;
    private TextMeshProUGUI textMeshPro;
    private RectTransform backgroundRextTransform;
    private TooltipTimer tooltipTimer;

    private void Awake()
    {
        Instance = this;

        rectTransform = GetComponent<RectTransform>();
        textMeshPro = transform.Find(TEXT).GetComponent<TextMeshProUGUI>();
        backgroundRextTransform = transform.Find(BACKGROUND).GetComponent<RectTransform>();

        Hide();
    }

    private void Update()
    {
        HandleFollowMouse();
        if(tooltipTimer != null)
        {
            tooltipTimer.timer -= Time.deltaTime;
            if(tooltipTimer.timer <= 0)
            {
                Hide();
            }
        }
    }

    private void HandleFollowMouse()
    {
        Vector2 anchoredPosition = rectTransform.anchoredPosition = Input.mousePosition / canvasRectTransform.localScale.x;

        if (anchoredPosition.x + backgroundRextTransform.rect.width > canvasRectTransform.rect.width)
        {
            anchoredPosition.x = canvasRectTransform.rect.width - backgroundRextTransform.rect.width;
        }

        if (anchoredPosition.y + backgroundRextTransform.rect.height > canvasRectTransform.rect.height)
        {
            anchoredPosition.y = canvasRectTransform.rect.height - backgroundRextTransform.rect.height;
        }

        rectTransform.anchoredPosition = anchoredPosition;
    }

    private void SetText(string tooltipText)
    {
        textMeshPro.SetText(tooltipText);
        textMeshPro.ForceMeshUpdate();

        Vector2 textSize = textMeshPro.GetRenderedValues(false);
        Vector2 padding = new Vector2(8, 8);
        backgroundRextTransform.sizeDelta = textSize + padding;
    }

    public void Show(string tooltipText, TooltipTimer tooltipTimer = null)
    {
        this.tooltipTimer = tooltipTimer;
        gameObject.SetActive(true);
        SetText(tooltipText);
        HandleFollowMouse();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public class TooltipTimer
    {
        public float timer;
    }
}
