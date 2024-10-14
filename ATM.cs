using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            List<WeightedSelection<ItemObject>> weights = [];
            foreach (var field in Plugin.listsOfQuarters) // No idea, but it works...
                weights.Add(new() { selection = field.selection, weight = field.weight });
            foreach (var quart in weights)
                quart.weight -= Mathf.RoundToInt(FindObjectsOfType<ATM>().Count(x => x.outputItem == quart.selection) * (quart.weight / FindObjectsOfType<ATM>().Count(x => x.outputItem != quart.selection)));
            outputItem = WeightedItemObject.ControlledRandomSelectionList(weights, new System.Random(CoreGameManager.Instance.Seed()));
            render.sprite = outputItem.itemSpriteLarge;
        }

        public void InsertItem(PlayerManager player, EnvironmentController ec)
        {
            StartCoroutine(Delay(player));
        }

        public bool ItemFits(Items item)
        {
            return item != outputItem.itemType & Plugin.listsOfQuarters.Exists(x => x.selection.itemType == item);
        }

        private IEnumerator Delay(PlayerManager pm)
        {
            yield return null;
            pm.itm.AddItem(outputItem);
        }
    }
}
