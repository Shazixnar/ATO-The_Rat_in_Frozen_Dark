using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using Obeliskial_Essentials;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections;
using TMPro.Examples;
using System.Text;

namespace TraitMod
{
    [HarmonyPatch]
    internal class Traits
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "IndirectDamage")]
        public static void IndirectDamagePrefix(ref Character __instance, ref int damage, ref Enums.DamageType damageType, ref String effect)
        {
            if (damageType == Enums.DamageType.Shadow && AtOManager.Instance.TeamHaveTrait("zekdarkestabyss") && !__instance.IsHero && effect == "dark")
            {
                int darkCharge = __instance.GetAuraCharges("dark");
                damage += Functions.FuncRoundToInt((float)damage * 0.05f * (float)darkCharge);
                damage += Functions.FuncRoundToInt((float)damage * 0.3f);
            }
        }

        // list of your trait IDs
        public static string[] myTraitList = { "zekdarkfeast", "zekcursepower", "zekfrozendark" };

        private static readonly Traits _instance = new Traits();

        public static void myDoTrait(
            string trait,
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard)
        {
            switch(trait)
            {
                case "zekdarkfeast":
                    _instance.zekdarkfeast(evt, character, target, auxInt, auxString, castedCard, trait);
                    break;
                    
                case "zekcursepower":
                    _instance.zekcursepower(evt, character, target, auxInt, auxString, castedCard, trait);
                    break;

                case "zekfrozendark":
                    _instance.zekfrozendark(evt, character, target, auxInt, auxString, castedCard, trait);
                    break;
            }
        }

        // activate traits
        public void zekdarkfeast(
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard,
            string trait)
        {
            if (character == null || character.HeroData == null) return;

            int darkCharges = character.EffectCharges("dark");
            int energyReduction = 0;

            if (darkCharges >= 5 && darkCharges < 20) energyReduction = 1;
            else if (darkCharges >= 20) energyReduction = 2;

            if (energyReduction <= 0) return;

            List<string> heroHand = MatchManager.Instance.GetHeroHand(character.HeroIndex);
            List<CardData> cardsToModify = new List<CardData>();

            foreach (var cardId in heroHand)
            {
                CardData card = MatchManager.Instance.GetCardData(cardId);
                if (card.GetCardFinalCost() > 0)
                    cardsToModify.Add(card);
            }

            foreach (var card in cardsToModify)
            {
                card.EnergyReductionTemporal += energyReduction;
                MatchManager.Instance.UpdateHandCards();

                var cardItem = MatchManager.Instance.GetCardFromTableByIndex(card.InternalId);
                cardItem?.PlayDissolveParticle();
                cardItem?.ShowEnergyModification(-energyReduction);

                character.HeroItem?.ScrollCombatText(
                    Texts.Instance.GetText("traits_Dark Feast", ""),
                    Enums.CombatScrollEffectType.Trait
                );

                MatchManager.Instance.CreateLogCardModification(card.InternalId, MatchManager.Instance.GetHero(character.HeroIndex));
            }
        }

        public void zekcursepower(
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard,
            string trait)
        {
            if (character == null || character.GetHp() <= 0) return;

            int healAmount = Functions.FuncRoundToInt(auxInt * 0.15f);
            Enums.CardClass cc = castedCard != null ? castedCard.CardClass : Enums.CardClass.None;

            healAmount = character.HealWithCharacterBonus(healAmount, cc, 0);
            healAmount = character.HealReceivedFinal(healAmount, false);

            character.ModifyHp(healAmount, true, true);

            var combatText = new CastResolutionForCombatText { heal = healAmount };

            if (character.HeroItem != null)
                character.HeroItem.ScrollCombatTextDamageNew(combatText);
            else if (character.NPCItem != null)
                character.NPCItem.ScrollCombatTextDamageNew(combatText);
        }

        public void zekfrozendark(
            Enums.EventActivation evt,
            Character character,
            Character target,
            int auxInt,
            string auxString,
            CardData castedCard,
            string trait)
        {
            if (character == null || castedCard == null) return;

            TraitData traitData = Globals.Instance.GetTraitData(trait);
            int used = MatchManager.Instance.activatedTraits.ContainsKey(trait) ? MatchManager.Instance.activatedTraits[trait] : 0;
            if (used >= traitData.TimesPerTurn) return;

            bool isCold = castedCard.HasCardType(Enums.CardType.Cold_Spell);
            bool isShadow = castedCard.HasCardType(Enums.CardType.Shadow_Spell);

            if (!isCold && !isShadow) return;
            if (character.HeroData == null) return;

            // 更新次数
            MatchManager.Instance.activatedTraits[trait] = used + 1;
            MatchManager.Instance.SetTraitInfoText();

            if (isCold)
            {
                character.ModifyEnergy(1, true);
                character.HeroItem?.ScrollCombatText(
                    Texts.Instance.GetText("traits_Frozen Dark", "")
                    + Functions.TextChargesLeft(used + 1, traitData.TimesPerTurn),
                    Enums.CombatScrollEffectType.Trait
                );
                EffectsManager.Instance.PlayEffectAC("energy", true, character.HeroItem?.CharImageT, false, 0f);

                NPC[] teamNPC = MatchManager.Instance.GetTeamNPC();
                foreach (var npc in teamNPC)
                {
                    if (npc != null && npc.Alive)
                        npc.SetAuraTrait(character, "dark", 1);
                }
            }
            else if (isShadow)
            {
                NPC[] teamNPC = MatchManager.Instance.GetTeamNPC();
                foreach (var npc in teamNPC)
                {
                    if (npc != null && npc.Alive)
                    {
                        npc.SetAuraTrait(character, "scourge", 1);
                        npc.SetAuraTrait(character, "chill", 1);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static class Trait_DoTrait_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(
                Enums.EventActivation __0,   // theEvent
                string __1,                  // trait id
                Character __2,               // character
                Character __3,               // target
                int __4,                     // auxInt
                string __5,                  // auxString
                CardData __6,                // castedCard
                Trait __instance)
            {
                string trait = __1;

                // 如果是自定义 trait，就直接调用我们的逻辑
                if (myTraitList.Contains(trait))
                {
                    myDoTrait(
                        trait,
                        __0,        // event
                        __2,        // character
                        __3,        // target
                        __4,        // auxInt
                        __5,        // auxString
                        __6         // castedCard
                    );

                    // 返回 false = 阻止原版 DoTrait 执行
                    return false;
                }

                // 否则走原版逻辑
                return true;
            }
        }

        public static string TextChargesLeft(int currentCharges, int chargesTotal)
        {
            int cCharges = currentCharges;
            int cTotal = chargesTotal;
            return "<br><color=#FFF>" + cCharges.ToString() + "/" + cTotal.ToString() + "</color>";
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), "GlobalAuraCurseModificationByTraitsAndItems")]
        public static void GlobalAuraCurseModificationByTraitsAndItemsPostfix(ref AtOManager __instance, ref AuraCurseData __result, string _type, string _acId, Character _characterCaster, Character _characterTarget)
        {
            bool flag = false;
            bool flag2 = false;
            if (_characterCaster != null && _characterCaster.IsHero)
            {
                flag = _characterCaster.IsHero;
            }
            if (_characterTarget != null && _characterTarget.IsHero)
            {
                flag2 = true;
            }
            float DamageWhenConsumedPerChargeModify = 2;
            if (_acId == "dark")
            {
                if (_type == "set")
                {
                    if (!flag2)
                    {
                        if (__instance.TeamHaveTrait("zekdarkfeast"))
                        {
                            __result.ResistModifiedPercentagePerStack = -1.5f;
                        }
                        if (__instance.TeamHaveTrait("malukahshadowform"))
                        {
                            DamageWhenConsumedPerChargeModify += 0.5f;
                        }
                        if (__instance.TeamHavePerk("mainperkdark2c"))
                        {
                            DamageWhenConsumedPerChargeModify += 0.7f;
                        }
                        if (__instance.TeamHavePerk("mainperkChill2d"))
                        {
                            int chillCharge = _characterTarget.GetAuraCharges("chill");
                            DamageWhenConsumedPerChargeModify += (float)(chillCharge / 20 * 0.1f);
                        }
                    }
                    else if (flag2)
                    {
                        if (_characterTarget != null && __instance.CharacterHaveTrait(_characterTarget.SubclassName, "zekcursepower"))
                        {
                            __result.AuraDamageType = Enums.DamageType.All;
                            __result.ExplodeAtStacks = 44;
                            __result.AuraDamageIncreasedPercentPerStack = 10;
                            __result.Removable = false;
                            __result.ResistModifiedPercentagePerStack = 0f;
                        }
                    }
                }
                else if (_type == "consume" && !flag)
                {
                    if (__instance.TeamHaveTrait("malukahshadowform"))
                    {
                        DamageWhenConsumedPerChargeModify += 0.5f;
                    }
                    if (__instance.TeamHavePerk("mainperkdark2c"))
                    {
                        DamageWhenConsumedPerChargeModify += 0.7f;
                    }
                    if (__instance.TeamHavePerk("mainperkChill2d"))
                    {
                        int chillCharge = _characterCaster.GetAuraCharges("chill");
                        DamageWhenConsumedPerChargeModify += (float)(chillCharge / 20 * 0.1f);
                    }
                }
                __result.DamageWhenConsumedPerCharge = DamageWhenConsumedPerChargeModify;
            }
            else if (_acId == "chill")
            {
                if (_type == "set")
                {
                    if (!flag2)
                    {
                        if (__instance.TeamHaveTrait("zekfrozendark"))
                        {
                            __result.MaxCharges = 100;
                            __result.MaxMadnessCharges = 100;
                        }
                    }
                    // 消除修改寒冷影响
                    else if (_characterTarget != null && __instance.CharacterHavePerk(_characterTarget.SubclassName, "mainperkChill2d"))
                    {
                        __result.CharacterStatChargesMultiplierNeededForOne = 5;
                    }
                }
            }
        }
    }
}
