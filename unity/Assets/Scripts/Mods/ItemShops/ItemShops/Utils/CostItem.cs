using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnboundLib;
using Sonigon;
using Sonigon.Internal;
using TMPro;

namespace ItemShops.Utils
{
    internal class CostItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Currency { get; internal set; }

        private Shop shop;

        private UnityEvent mouseEnter = new UnityEvent();
        private UnityEvent mouseExit = new UnityEvent();

        private Image _icon = null;
        private TextMeshProUGUI _text = null;

        private GUIStyle guiStyleFore;
        private bool inBounds = false;

        public Image Image
        {
            get
            {
                if (!_icon)
                {
                    _icon = this.gameObject.transform.Find("Cost Icon").GetComponent<Image>();
                }
                return _icon;
            }
        }

        public TextMeshProUGUI Text
        {
            get
            {
                if (!_text)
                {
                    _text = this.gameObject.transform.Find("Cost Text").GetComponent<TextMeshProUGUI>();
                }
                return _text;
            }
        }
        private void Start()
        {
            guiStyleFore = new GUIStyle();
            guiStyleFore.richText = true;
            guiStyleFore.normal.textColor = Color.white;
            guiStyleFore.alignment = TextAnchor.UpperLeft;
            guiStyleFore.wordWrap = false;
            guiStyleFore.stretchWidth = true;
            var background = new Texture2D(1, 1);
            background.SetPixel(0, 0, Color.gray);
            background.Apply();
            guiStyleFore.normal.background = background;

            shop = this.gameObject.GetComponentInParent<Shop>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            mouseEnter?.Invoke();
            inBounds = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            mouseExit?.Invoke();
            inBounds = false;
        }

        private void OnGUI()
        {
            if (this.inBounds && Currency?.Trim().Length > 0)
            {
                Vector2 size = guiStyleFore.CalcSize(new GUIContent(Currency));
                GUILayout.BeginArea(new Rect(Input.mousePosition.x + 25, Screen.height - Input.mousePosition.y + 25, size.x + 10, size.y + 10));
                GUILayout.BeginVertical();
                GUILayout.Label(Currency, guiStyleFore);
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
    }
}
