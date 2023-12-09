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

        public static void myDoTrait(string _trait, ref Trait __instance)
        {
            // get info you may need
            Enums.EventActivation _theEvent = Traverse.Create(__instance).Field("theEvent").GetValue<Enums.EventActivation>();
            Character _character = Traverse.Create(__instance).Field("character").GetValue<Character>();
            Character _target = Traverse.Create(__instance).Field("target").GetValue<Character>();
            int _auxInt = Traverse.Create(__instance).Field("auxInt").GetValue<int>();
            string _auxString = Traverse.Create(__instance).Field("auxString").GetValue<string>();
            CardData _castedCard = Traverse.Create(__instance).Field("castedCard").GetValue<CardData>();
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            List<CardData> cardDataList = new List<CardData>();
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();

            // activate traits
            if (_trait == "zekdarkfeast")
            {
                if (_character.HeroData != null)
                {
                    int num = _character.EffectCharges("dark");
                    int num2 = 0;
                    if (num >= 5 && num <20)
                    {
                        num2 = 1;
                    }
                    else if (num >= 20)
                    {
                        num2 = 2;
                    }
                    if (num2 > 0)
                    {
                        heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
                        List<CardData> list = new List<CardData>();
                        for (int i = 0; i < heroHand.Count; i++)
                        {
                            CardData cardData = MatchManager.Instance.GetCardData(heroHand[i]);
                            if (cardData.GetCardFinalCost() > 0)
                            {
                                list.Add(cardData);
                            }
                        }
                        for (int j = 0; j < list.Count; j++)
                        {
                            CardData cardData = list[j];
                            cardData.EnergyReductionTemporal += num2;
                            MatchManager.Instance.UpdateHandCards();
                            CardItem cardFromTableByIndex = MatchManager.Instance.GetCardFromTableByIndex(cardData.InternalId);
                            cardFromTableByIndex.PlayDissolveParticle();
                            cardFromTableByIndex.ShowEnergyModification(-num2);
                            _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_Dark Feast", ""), Enums.CombatScrollEffectType.Trait);
                            MatchManager.Instance.CreateLogCardModification(cardData.InternalId, MatchManager.Instance.GetHero(_character.HeroIndex));
                        }
                    }
                    return;
                }
            }
            else if (_trait == "zekcursepower")
            {
                if (_character != null && _character.GetHp() > 0)
                {
                    int num = Functions.FuncRoundToInt((float)_auxInt * 0.15f);
                    Enums.CardClass cc = Enums.CardClass.None;
                    if (_castedCard != null)
                    {
                        cc = _castedCard.CardClass;
                    }
                    num = _character.HealWithCharacterBonus(num, cc, 0);
                    num = _character.HealReceivedFinal(num, false);
                    _character.ModifyHp(num, true, true);
                    CastResolutionForCombatText castResolutionForCombatText = new CastResolutionForCombatText();
                    castResolutionForCombatText.heal = num;
                    if (_character.HeroItem != null)
                    {
                        _character.HeroItem.ScrollCombatTextDamageNew(castResolutionForCombatText);
                        return;
                    }
                    if (_character.NPCItem != null)
                    {
                        _character.NPCItem.ScrollCombatTextDamageNew(castResolutionForCombatText);
                    }
                }
            }
            else if (_trait == "zekfrozendark")
            {
                if (MatchManager.Instance != null && _castedCard != null)
                {
                    traitData = Globals.Instance.GetTraitData("zekfrozendark");
                    if (MatchManager.Instance.activatedTraits != null && MatchManager.Instance.activatedTraits.ContainsKey("zekfrozendark") && MatchManager.Instance.activatedTraits["zekfrozendark"] > traitData.TimesPerTurn - 1)
                    {
                        return;
                    }
                    if (MatchManager.Instance.energyJustWastedByHero > 0 && (_castedCard.GetCardTypes().Contains(Enums.CardType.Cold_Spell) || _castedCard.GetCardTypes().Contains(Enums.CardType.Shadow_Spell)) && _character.HeroData != null)
                    {
                        if (!MatchManager.Instance.activatedTraits.ContainsKey("zekfrozendark"))
                        {
                            MatchManager.Instance.activatedTraits.Add("zekfrozendark", 1);
                        }
                        else
                        {
                            Dictionary<string, int> activatedTraits = MatchManager.Instance.activatedTraits;
                            activatedTraits["zekfrozendark"] = activatedTraits["zekfrozendark"] + 1;
                        }
                        MatchManager.Instance.SetTraitInfoText();
                        if (_castedCard.GetCardTypes().Contains(Enums.CardType.Cold_Spell))
                        {
                            _character.ModifyEnergy(1, true);
                            if (_character.HeroItem != null)
                            {
                                _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_Frozen Dark", "") + TextChargesLeft(MatchManager.Instance.activatedTraits["zekfrozendark"], traitData.TimesPerTurn), Enums.CombatScrollEffectType.Trait);
                                EffectsManager.Instance.PlayEffectAC("energy", true, _character.HeroItem.CharImageT, false, 0f);
                            }
                            NPC[] teamNPC = MatchManager.Instance.GetTeamNPC();
                            for (int i = 0; i < teamNPC.Length; i++)
                            {
                                if (teamNPC[i] != null && teamNPC[i].Alive)
                                {
                                    teamNPC[i].SetAuraTrait(_character, "dark", 1);
                                }
                            }
                            return;
                        }
                        else if (_castedCard.GetCardTypes().Contains(Enums.CardType.Shadow_Spell))
                        {
                            NPC[] teamNPC = MatchManager.Instance.GetTeamNPC();
                            for (int i = 0; i < teamNPC.Length; i++)
                            {
                                if (teamNPC[i] != null && teamNPC[i].Alive)
                                {
                                    teamNPC[i].SetAuraTrait(_character, "scourge", 2);
                                    teamNPC[i].SetAuraTrait(_character, "chill", 3);
                                }
                            }
                            return;
                        }
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static bool DoTrait(Enums.EventActivation _theEvent, string _trait, Character _character, Character _target, int _auxInt, string _auxString, CardData _castedCard, ref Trait __instance)
        {
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
                return false;
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            if (Content.medsCustomTraitsSource.Contains(_trait) && myTraitList.Contains(_trait))
            {
                myDoTrait(_trait, ref __instance);
                return false;
            }
            return true;
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
                if (_type == "set" && !flag2)
                {
                    if (__instance.TeamHaveTrait("zekfrozendark"))
                    {
                        __result.MaxCharges = 100;
                        __result.MaxMadnessCharges = 100;
                    }
                }
            }
        }
    }
}
