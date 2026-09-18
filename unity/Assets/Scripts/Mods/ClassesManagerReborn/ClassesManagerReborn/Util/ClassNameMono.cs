using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClassesManagerReborn.Util
{
    public class DestroyOnUnParent : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (gameObject.transform.parent == null) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Can be added to cards to add text to the lower right hand cornner.
    /// </summary>
    public class ClassNameMono : MonoBehaviour
    {
        /// <summary>
        /// The text that will appear in the lower right hand corner of the card.
        /// </summary>
        public string className = "Class";
        /// <summary>
        /// The color the top 2 corners of Class cards will be (Black to disable)
        /// </summary>
        public Color ClassDefult = new Color(53 / 255f, 196 / 255f, 10 / 255f, 1);
        /// <summary>
        /// The color the top 2 corners of SubClass cards will be (Black to disable)
        /// </summary>
        public Color SubClassDefult = new Color(0.7f, 0.33f, .05f, 1);
        /// <summary>
        /// The color the top 2 corners of the card will be (Black for defualt)
        /// </summary>
        public Color color1 = Color.black;
        /// <summary>
        /// The color the Botton 2 corners of the card will be (Black for defualt)
        /// </summary>
        public Color color2 = Color.black;
        internal bool isOn;
        internal bool lastOn;
        private CardInfo card;
        private void Start()
        {
            try
            {
                card = gameObject.GetComponent<CardInfo>();
                var allChildrenRecursive = gameObject.GetComponentsInChildren<RectTransform>();
                var BottomLeftCorner = allChildrenRecursive.Where(obj => obj.gameObject.name == "EdgePart (1)")
                    .FirstOrDefault().gameObject;
                var modNameObj =
                    Instantiate(new GameObject("ExtraCardText", typeof(TextMeshProUGUI), typeof(DestroyOnUnParent)),
                        BottomLeftCorner.transform.position, BottomLeftCorner.transform.rotation,
                        BottomLeftCorner.transform);
                var modText = modNameObj.gameObject.GetComponent<TextMeshProUGUI>();

                modText.text = className;
                modText.enableWordWrapping = false;
                modText.alignment = TextAlignmentOptions.Bottom;
                modText.alpha = 0.1f;
                modText.fontSize = 50;

                modNameObj.transform.Rotate(0f, 0f, 135f);
                modNameObj.transform.localScale = new Vector3(1f, 1f, 1f);
                modNameObj.transform.localPosition = new Vector3(-50f, -50f, 0f);
                lastOn = !isOn;
            }catch { }
        }

        private void Update()
        {
            if(lastOn != isOn)
            try
            {
                if (ClassesRegistry.Get(card.sourceCard) != null && (ClassesRegistry.Get(card.sourceCard).type & CardType.NonClassCard) == 0)
                {
                    if ((ClassesRegistry.Get(card.sourceCard).type & CardType.Entry) != 0)
                        color1 = color2 = ClassDefult;
                    if ((ClassesRegistry.Get(card.sourceCard).type & CardType.SubClass) != 0)
                        color1 = color2 = SubClassDefult;
                }
                List<GameObject> triangles = FindObjectsInChildren(gameObject, "Triangle");
                bool up = true;
                int counter = 1;
                for (int i = 0; i < triangles.Count; i++)
                {
                    if (++counter > 2) { up = !up; counter = 1; }
                    if (up && color1 != Color.black)
                    {
                        for(int j = 4; j <=6; j++)
                            triangles[i].transform.Find($"FRAME ({j})").GetComponent<Image>().color = isOn ? color1 : new Color(color1.r, color1.g, color1.b, color1.a / 6);
                    }
                    if (!up && color2 != Color.black)
                    {
                        for (int j = 4; j <= 6; j++)
                            triangles[i].transform.Find($"FRAME ({j})").GetComponent<Image>().color = isOn ? color2 : new Color(color2.r, color2.g, color2.b, color2.a / 6);
                    }
                }
                lastOn = isOn;
            }
            catch { this.enabled = false; }
        }

        public static List<GameObject> FindObjectsInChildren(GameObject gameObject, string gameObjectName)
        {
            List<GameObject> returnObjects = new List<GameObject>();
            Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in children)
            {
                if (item.gameObject.name.Equals(gameObjectName))
                {
                    returnObjects.Add(item.gameObject);
                }
            }

            return returnObjects;
        }
    }
}
