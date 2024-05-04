using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BaldiQuarterConverter
{
    public class ATM : EnvironmentObject, IItemAcceptor
    {
        private ItemObject outputItem;
        public SpriteRenderer render;

        public override void LoadingFinished()
        {
            outputItem = Plugin.listsOfQuarters[Mathf.RoundToInt(UnityEngine.Random.Range(0f, Plugin.listsOfQuarters.Count-1))];

            render.sprite = outputItem.itemSpriteLarge;
        }

        public void InsertItem(PlayerManager player, EnvironmentController ec)
        {
            StartCoroutine(Delay(player));
        }

        public bool ItemFits(Items item)
        {
            return item != outputItem.itemType & Plugin.listsOfQuarters.Exists(x => x.itemType == item);
        }

        private IEnumerator Delay(PlayerManager pm)
        {
            yield return null;
            pm.itm.AddItem(outputItem);
        }
    }
}
