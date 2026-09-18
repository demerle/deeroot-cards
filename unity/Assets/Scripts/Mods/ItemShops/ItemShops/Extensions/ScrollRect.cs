using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using UnityEngine;

namespace ItemShops.Extensions
{
    public static class ScrollRectExtension
    {
        public static void VerticalScrollToChild(this ScrollRect scrollRect, GameObject child)
        {
            var childRect = child.GetComponent<RectTransform>();
            var scrollSize = scrollRect.GetComponent<RectTransform>().sizeDelta.y;
            var childPos = Mathf.Abs(childRect.anchoredPosition.y);
            var childPivot = (1f - childRect.pivot.y);
            var childSize = childRect.sizeDelta.y;
            var contentSize = scrollRect.content.sizeDelta.y;

            var childCenter = childPos - ((0.5f - childPivot) * childSize);
            var contentAdjustedSize = contentSize - scrollSize;

            var scrollBarPos = 1f - ((childCenter / contentAdjustedSize) - ((scrollSize / 2f) / contentAdjustedSize));

            //UnityEngine.Debug.Log($"Setting Scrollbar position to {string.Format("{0:F2}", scrollBarPos)}%\nCenter: {childCenter}\nContentSize: {contentSize}\nViewport Size: {scrollSize}\nAjusted Size: {contentAdjustedSize}");

            scrollRect.verticalScrollbar.value = scrollBarPos;
        }
    }
}
