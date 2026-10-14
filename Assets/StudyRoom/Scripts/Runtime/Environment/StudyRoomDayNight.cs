using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Local device clock and a readable, smoothly changing room atmosphere.</summary>
public class StudyRoomDayNight : MonoBehaviour
{
    private Text clock, period, date;
    private Image indicator;
    private Light sunlight;
    private Color originalLightColor, originalAmbient;
    private float originalIntensity;
    private AmbientMode originalAmbientMode;
    private Camera roomCamera;
    private Color originalBackground;
    private float nextUpdate;
    private bool captured;

    private void Start()
    {
        BuildInterface();
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional && light.gameObject.scene == gameObject.scene && light.enabled)
            { sunlight = light; break; }
        if (!sunlight)
        {
            var source = new GameObject("RealTime Sunlight", typeof(Light));
            source.transform.SetParent(transform, false);
            source.transform.rotation = Quaternion.Euler(50, -30, 0);
            sunlight = source.GetComponent<Light>();
            sunlight.type = LightType.Directional;
            sunlight.intensity = 2;
            sunlight.shadows = LightShadows.Soft;
        }
        originalLightColor = sunlight.color;
        originalIntensity = sunlight.intensity;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbient = RenderSettings.ambientLight;
        roomCamera = Camera.main;
        if (roomCamera) originalBackground = roomCamera.backgroundColor;
        captured = true;
        RefreshClock();
    }

    private void Update()
    {
        // Unscaled time keeps the clock running while gameplay is paused.
        if (Time.unscaledTime < nextUpdate) return;
        nextUpdate = Time.unscaledTime + 0.25f;
        RefreshClock();
    }

    private void OnApplicationFocus(bool focused) { if (focused && captured) RefreshClock(); }

    private void RefreshClock() => ApplyTime(DateTime.Now);

    public void ApplyTime(DateTime now)
    {
        float hour = (float)now.TimeOfDay.TotalHours;
        bool morning = hour >= 6 && hour < 12;
        bool afternoon = hour >= 12 && hour < 18;
        if (clock) clock.text = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        if (date) date.text = now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        if (period) period.text = morning ? "BUỔI SÁNG" : afternoon ? "BUỔI CHIỀU" : "BAN ĐÊM";

        // Blend around dawn and dusk; preserve ambient fill even at midnight.
        float daylight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(5, 7, hour)) *
                         (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(17, 19, hour)));
        float warmth = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(11, 15, hour));
        Color dayColor = Color.Lerp(new Color(1f, 0.95f, 0.86f), new Color(1f, 0.73f, 0.43f), warmth);
        Color lightColor = Color.Lerp(new Color(0.66f, 0.75f, 1f), dayColor, daylight);
        if (indicator) indicator.color = morning ? new Color32(255, 209, 114, 255) :
            afternoon ? new Color32(240, 160, 86, 255) : new Color32(151, 163, 224, 255);
        if (!captured) return;
        if (sunlight)
        {
            sunlight.color = lightColor;
            sunlight.intensity = Mathf.Lerp(0.65f, Mathf.Max(1.2f, originalIntensity), daylight);
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(new Color(0.32f, 0.36f, 0.48f),
            Color.Lerp(new Color(0.64f, 0.61f, 0.57f), new Color(0.66f, 0.53f, 0.41f), warmth), daylight);
        if (roomCamera) roomCamera.backgroundColor = Color.Lerp(new Color(0.12f, 0.15f, 0.23f), originalBackground, daylight);
    }

    public void BuildInterface()
    {
        var canvas = transform.Find("Canvas");
        if (!canvas) return;
        var existing = canvas.Find("RealTimeBar");
        if (existing)
        {
            clock = existing.Find("Clock").GetComponent<Text>();
            period = existing.Find("Period").GetComponent<Text>();
            indicator = existing.Find("Indicator").GetComponent<Image>();
            ConfigureClockLayout(existing);
            ApplyTime(DateTime.Now);
            return;
        }
        var reference = canvas.GetComponentInChildren<Text>(true);
        Font font = reference && reference.font ? reference.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var bar = new GameObject("RealTimeBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(canvas, false);
        var rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(28, -28);
        rect.sizeDelta = new Vector2(348, 114);
        var panel = bar.GetComponent<Image>();
        panel.color = new Color32(255, 247, 252, 245);
        panel.raycastTarget = false;
        var menu = canvas.Find("MenuButton");
        if (menu)
        {
            var style = menu.GetComponent<Image>();
            if (style) { panel.sprite = style.sprite; panel.type = style.type; }
        }
        var dot = new GameObject("Indicator", typeof(RectTransform), typeof(Image));
        dot.transform.SetParent(bar.transform, false);
        var dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchorMin = dotRect.anchorMax = new Vector2(0, 0.5f);
        dotRect.anchoredPosition = new Vector2(22, 30);
        dotRect.sizeDelta = new Vector2(10, 10);
        indicator = dot.GetComponent<Image>();
        indicator.raycastTarget = false;
        period = CreateText(bar.transform, "Period", font, 13, new Vector2(40, -17), new Vector2(290, 20));
        clock = CreateText(bar.transform, "Clock", font, 21, new Vector2(18, -39), new Vector2(315, 28));
        ConfigureClockLayout(bar.transform);
        // Keep the existing menu overlay above this HUD.
        bar.transform.SetAsFirstSibling();
        ApplyTime(DateTime.Now);
    }

    private void ConfigureClockLayout(Transform bar)
    {
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = barRect.anchorMax = barRect.pivot = Vector2.zero;
        barRect.anchoredPosition = new Vector2(28, 28);
        barRect.sizeDelta = new Vector2(410, 114);
        clock.fontSize = 32;
        clock.rectTransform.anchoredPosition = new Vector2(18, -37);
        clock.rectTransform.sizeDelta = new Vector2(315, 46);
        clock.horizontalOverflow = HorizontalWrapMode.Overflow;
        clock.verticalOverflow = VerticalWrapMode.Overflow;
        var existingDate = bar.Find("Date");
        date = existingDate ? existingDate.GetComponent<Text>() :
            CreateText(bar, "Date", clock.font, 15, new Vector2(20, -81), new Vector2(310, 26));
        date.horizontalOverflow = HorizontalWrapMode.Overflow;
        date.verticalOverflow = VerticalWrapMode.Overflow;
        var dotRect = indicator.rectTransform;
        dotRect.anchoredPosition = new Vector2(22, 30);
    }

    private static Text CreateText(Transform parent, string name, Font font, int size, Vector2 position, Vector2 dimensions)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        var text = obj.GetComponent<Text>();
        text.font = font; text.fontSize = size;
        text.color = new Color32(79, 57, 98, 255);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private void OnDestroy()
    {
        if (!captured) return;
        if (sunlight) { sunlight.color = originalLightColor; sunlight.intensity = originalIntensity; }
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientLight = originalAmbient;
        if (roomCamera) roomCamera.backgroundColor = originalBackground;
    }
}
