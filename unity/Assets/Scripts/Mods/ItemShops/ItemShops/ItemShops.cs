using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemShops.Extensions;
using ItemShops.Utils;
using UnboundLib;
using UnboundLib.GameModes;
using Jotunn.Utils;
using UnityEngine;
using Sonigon;

namespace ItemShops
{
    // These are the mods required for our mod to work
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]
    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]
    public class ItemShops : BaseUnityPlugin
    {
        private const string ModId = "com.willuwontu.rounds.itemshops";
        private const string ModName = "Item Shops";
        public const string Version = "0.0.1"; // What version are we on (major.minor.patch)?

        public static ItemShops instance { get; private set; }

        public AssetBundle assets { get; private set; }

        public List<AudioClip> click;
        public List<AudioClip> hover;
        public List<SoundEvent> clickSounds = new List<SoundEvent>();
        public List<SoundEvent> hoverSounds = new List<SoundEvent>();

        void Awake()
        {

        }
        void Start()
        {
            instance = this;

            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            assets = AssetUtils.LoadAssetBundleFromResources("shopuiassets", typeof(ItemShops).Assembly);

            { // Button Sounds
                
                click = assets.LoadAllAssets<AudioClip>().ToList().Where(clip => clip.name.Contains("UI_Button_Click")).ToList();
                hover = assets.LoadAllAssets<AudioClip>().ToList().Where(clip => clip.name.Contains("UI_Button_Hover")).ToList();

                try
                {
                    foreach (var sound in click)
                    {
                        SoundContainer soundContainer = ScriptableObject.CreateInstance<SoundContainer>();
                        soundContainer.setting.volumeIntensityEnable = true;
                        soundContainer.setting.volumeDecibel = 10f;
                        soundContainer.audioClip[0] = sound;
                        SoundEvent soundEvent = ScriptableObject.CreateInstance<SoundEvent>();
                        soundEvent.soundContainerArray[0] = soundContainer;
                        clickSounds.Add(soundEvent);
                    }

                    foreach (var sound in hover)
                    {
                        SoundContainer soundContainer = ScriptableObject.CreateInstance<SoundContainer>();
                        soundContainer.setting.volumeIntensityEnable = true;
                        soundContainer.setting.volumeDecibel = 10f;
                        soundContainer.audioClip[0] = sound;
                        SoundEvent soundEvent = ScriptableObject.CreateInstance<SoundEvent>();
                        soundEvent.soundContainerArray[0] = soundContainer;
                        hoverSounds.Add(soundEvent);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            
            gameObject.AddComponent<ShopManager>();
            gameObject.AddComponent<CurrencyManager>();

            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEnd);
        }

        private IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            foreach (var shop in ShopManager.instance.Shops.Values)
            {
                shop.Hide();
            }

            yield break;
        }

        private IEnumerator OnGameStart(IGameModeHandler gm)
        {
            foreach (var player in PlayerManager.instance.players)
            {
                player.GetAdditionalData().bankAccount.RemoveAllMoney();
            }

            yield break;
        }

        //private void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.I))
        //    {
        //        Shop shop = null;

        //        if (ShopManager.instance.Shops.Count() > 0)
        //        {
        //            shop = ShopManager.instance.Shops.First().Value;
        //        }
        //        else
        //        {
        //            shop = ShopManager.instance.CreateShop("Test Shop");

        //            this.ExecuteAfterSeconds(1f, () =>
        //            {
        //                var items = UnboundLib.Utils.CardManager.cards.Values.Select(card => new PurchasableCard(card.cardInfo, new Dictionary<string, int> { { "Credits", 1 }, { "Banana", 2 } }, new Tag[] { new Tag(card.category), new Tag("Banana") })).ToArray();
        //                shop.AddItems(items.Select(item => item.Card.cardName).ToArray(), items);
        //            });
        //            //this.ExecuteAfterSeconds(2f, () =>
        //            //{
        //            //    shop.AddItem("Walrus 500", UnboundLib.Utils.CardManager.cards.Values.Select(card => new PurchasableTRTCard(card.cardInfo, new Dictionary<string, int>(), new Tag[] { new Tag(card.category) })).ToArray().GetRandom<Purchasable>());
        //            //});
        //        }

        //        if (shop.IsOpen)
        //        {
        //            if (!shop.Filter.isFocused)
        //            {
        //                shop.Hide();
        //            }
        //        }
        //        else
        //        {
        //            shop.Show(PlayerManager.instance.players[0]);
        //        }
        //    }
        //}
    }
}
