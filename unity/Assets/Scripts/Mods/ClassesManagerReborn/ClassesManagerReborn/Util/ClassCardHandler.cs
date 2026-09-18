using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ClassesManagerReborn {
    public class ClassCardHandler: MonoBehaviour {

        [SerializeField]
        public string className;

        [EnumToggleButtons]
        [ShowInInspector]
        [SerializeField]
        public CardType cardType = CardType.Card;

        public int maxAllowed = 0;

        [EnumToggleButtons]
        [ShowInInspector]
        public RequirementsType requirementType = RequirementsType.Single;

        [AssetsOnly]
        [ShowIf("requirementType", RequirementsType.Single)]
        public CardInfo singleReq;

        [AssetsOnly]
        [ShowIf("requirementType", RequirementsType.List)]
        public CardInfo[] listReq;

        [AssetsOnly]
        [ShowIf("requirementType", RequirementsType.MultiList)]
        public CardInfo[] multilistReq;
        [ShowIf("requirementType", RequirementsType.MultiList)]
        public int multilistCount;

        public CardInfo[] whiteList;

        public CardInfo[] blackList;

        [HideInInspector]
        public ClassObject classObject;

        [Serializable]
        public enum RequirementsType {
            None,
            Single,
            List,
            MultiList
        }
        internal IEnumerator Regester() {
            switch(requirementType) {
                case RequirementsType.None:
                    classObject=ClassesRegistry.Register(GetComponent<CardInfo>(), cardType, maxAllowed);
                    break;
                case RequirementsType.Single:
                    classObject=ClassesRegistry.Register(GetComponent<CardInfo>(), cardType, singleReq, maxAllowed);
                    break;
                case RequirementsType.List:
                    classObject=ClassesRegistry.Register(GetComponent<CardInfo>(), cardType, listReq, maxAllowed);
                    break;
                case RequirementsType.MultiList:
                    var req = ClassObject.TecTreeHelper(multilistReq, multilistCount);
                    classObject=ClassesRegistry.Register(GetComponent<CardInfo>(), cardType, req, maxAllowed);
                    break;
            }
            yield break;
        }
        internal IEnumerator PostRegester() {
            foreach(var card in whiteList) {
                classObject.Whitelist(card);
            }
            foreach(var card in blackList) {
                classObject.Blacklist(card);
            }
            yield break;
        }
    }

}