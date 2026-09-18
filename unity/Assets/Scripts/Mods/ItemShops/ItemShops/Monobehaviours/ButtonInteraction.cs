using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnboundLib;

namespace ItemShops.Monobehaviours
{
    public class ButtonInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEvent mouseClick = new UnityEvent();
        public UnityEvent mouseEnter = new UnityEvent();
        public UnityEvent mouseExit = new UnityEvent();
        public Button button;
        public AudioSource source;

        private System.Random random = new System.Random();

        private void Start()
        {
            button = gameObject.GetComponent<Button>();
            source = gameObject.GetOrAddComponent<AudioSource>();

            mouseEnter.AddListener(OnEnter);
            mouseExit.AddListener(OnExit);
            mouseClick.AddListener(OnClick);
        }

        public void OnEnter()
        {
            if (button.interactable)
            {
                source.PlayOneShot(ItemShops.instance.hover[random.Next(ItemShops.instance.hover.Count)]);
            }
        }

        public void OnExit()
        {
            if (button.interactable)
            {
                source.PlayOneShot(ItemShops.instance.hover[random.Next(ItemShops.instance.hover.Count)]);
            }
        }

        public void OnClick()
        {
            if (button.interactable)
            {
                source.PlayOneShot(ItemShops.instance.click[random.Next(ItemShops.instance.click.Count)]);
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.interactable)
            {
                mouseEnter?.Invoke();
            }
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (button.interactable)
            {
                mouseExit?.Invoke();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button.interactable)
            {
                mouseClick?.Invoke();
            }
        }
    }
}
