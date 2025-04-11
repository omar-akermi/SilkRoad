using UnityEngine.UI;
using UnityEngine;

public static class UIFactory
{
    public static GameObject Panel(string name, Transform parent, Color bgColor, Vector2? anchorMin = null, Vector2? anchorMax = null, bool fullAnchor = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();

        if (fullAnchor)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = anchorMin ?? Vector2.zero;
            rt.anchorMax = anchorMax ?? Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Image bg = go.AddComponent<Image>();
        bg.color = bgColor;

        return go;
    }


    public static Text Text(string name, string content, Transform parent, int fontSize = 16, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 30f);

        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.fontStyle = style;
        txt.color = Color.white;

        return txt;
    }

    public static GameObject Button(string name, string label, Transform parent, Color color)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        Image img = buttonGO.AddComponent<Image>();
        img.color = color;

        Button btn = buttonGO.AddComponent<Button>();
        btn.targetGraphic = img;

        // Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        Text txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;

        return buttonGO;
    }

    public static RectTransform ScrollableVerticalList(string name, Transform parent, out VerticalLayoutGroup layoutGroup)
    {
        GameObject scrollGO = new GameObject(name);
        scrollGO.transform.SetParent(parent, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewport = Panel("Viewport", scrollGO.transform, new Color(0, 0, 0, 0.05f));
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;

        layoutGroup = content.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandHeight = false;

        scroll.content = contentRT;

        return scrollRT;
    }
}
