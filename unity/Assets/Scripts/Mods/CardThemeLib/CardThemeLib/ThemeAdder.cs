using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardThemeLib
{
    internal class ThemeAdder : MonoBehaviour
    {
        public string Name;

        public Color targetColor;

        public Color bgColor;


        void Start()
        {
            GetComponent<CardInfo>().colorTheme = CardThemeLib.instance.CreateOrGetType(Name, new CardThemeColor { targetColor = targetColor, bgColor = bgColor });
        }
    }
}
