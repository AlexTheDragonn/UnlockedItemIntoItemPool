﻿using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace AlexTheDragon
{
    [BepInPlugin("com.AlexTheDragon.UnlockedItemIntoItemPool", "Unlocked Item into Item Pool", "1.0.0")]
    public class UnlockedItemIntoItemPool : BaseUnityPlugin
    {
        public void Awake()
        {
            commandCubePrefab = Resources.Load<GameObject>("Prefabs/NetworkedObjects/CommandCube");

            On.RoR2.UserAchievementManager.GrantAchievement += (orig, self, achievementDef) =>  //This works on User unlocks, like via UnlockAchievement().
            {
                foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList)
                {
                    GenericStaticEnumerable<AchievementDef, AchievementManager.Enumerator> allAchievementDefs = AchievementManager.allAchievementDefs;//get all the achievementdefs
                    foreach (AchievementDef userAchievDef in allAchievementDefs)
                    {
                        if (userAchievDef == achievementDef) //did we actually get this achievement?
                        {
                            AddItemFromAchievement(achievementDef); //add this item from this achievement to the itempool PLEAAASE
                        }
                    }
                }
                orig(self, achievementDef);
            };

            On.RoR2.NetworkUser.ServerHandleUnlock += (orig, self, unlockableDef) => //This is NetworkUser, like what usually happens when someone unlocks an achievement.
            {
                AddItemFromString(unlockableDef.cachedName);
                orig(self, unlockableDef);
            };

        }

        /// <summary>
        /// Add an item to the item pool with an AchievementDef.
        /// </summary>
        /// <param name="achievementDef">The achievementDef of the thing that unlocks the item.</param>
        public void AddItemFromAchievement(AchievementDef achievementDef)
        {
            string unlockableRewardIdentifier = achievementDef.unlockableRewardIdentifier; //Takes "Items.[Item Name]" from the achievementDef
            string pattern = @"\w+\."; //this just means "[infinite letters until]."
            bool equipment = false;
            unlockableRewardIdentifier = Regex.Replace(unlockableRewardIdentifier, pattern, ""); //remove "[infinite letters until]." so we have the itemname remaining

            foreach (EquipmentIndex i in EquipmentCatalog.equipmentList)
            {
                EquipmentDef EqDef = EquipmentCatalog.GetEquipmentDef(i);
                string equipmentString = EqDef.name;
                if (unlockableRewardIdentifier == equipmentString)
                {
                    Run.instance.availableEquipment.Add(EquipmentCatalog.FindEquipmentIndex(unlockableRewardIdentifier));
                    equipment = true;
                    break; //So we don't search everything if we already have it
                }
            }
            if (!equipment) //it doesn't matter if we try to find itemindex for characters or logs, due to the fact that they won't have the same name as an available item, and will not result in an ItemIndex that we can use
            {
                Run.instance.availableItems.Add(ItemCatalog.FindItemIndex(unlockableRewardIdentifier)); //Add the item from this string into the available items
            }
            Run.instance.BuildDropTable(); //Makes it so that everything we added actually gets put into the game pool so we can get it on the next items, you can see it that old items do not have it with command, but hopefully that won't matter :]
            UpdateDroppedCommandDroplets();
        }
        /// <summary>
        /// Add an item to the item pool, but uses a string instead of AchievementDefs.
        /// </summary>
        /// <param name="unlockableRewardIdentifier">The unlockableRewardIndentifier, e.g. "Item.Bear"</param>
        public void AddItemFromString(string unlockableRewardIdentifier)
        {
            string pattern = @"\w+\.";
            unlockableRewardIdentifier = Regex.Replace(unlockableRewardIdentifier, pattern, "");
            foreach (EquipmentIndex i in EquipmentCatalog.equipmentList)
            {
                EquipmentDef EqDef = EquipmentCatalog.GetEquipmentDef(i);
                string equipmentString = EqDef.name;
                if (unlockableRewardIdentifier == equipmentString)
                {
                    Run.instance.availableEquipment.Add(EquipmentCatalog.FindEquipmentIndex(unlockableRewardIdentifier));
                }
                else //items
                {
                    Run.instance.availableItems.Add(ItemCatalog.FindItemIndex(unlockableRewardIdentifier));
                }
            }
            Run.instance.BuildDropTable();
            UpdateDroppedCommandDroplets();
        }
        private static GameObject commandCubePrefab;
        /// <summary>
        /// Destroys and rebuilds the dropped commmands so they have the updated item pool.
        /// </summary>
        public void UpdateDroppedCommandDroplets()
        {
            Dictionary<NetworkInstanceId, NetworkIdentity> destroyObjects = new Dictionary<NetworkInstanceId, NetworkIdentity>();
            List<GameObject> spawnObjects = new List<GameObject>();
            foreach (KeyValuePair<NetworkInstanceId, NetworkIdentity> a in NetworkServer.objects) //CommandCube(Clone)
            {
                if (a.Value.gameObject.name.StartsWith("CommandCube"))
                {

                    Logger.LogMessage(a.Value.gameObject.name);
                    Transform tf = a.Value.gameObject.transform;

                    PickupIndex pickupIndex = a.Value.gameObject.GetComponent<PickupIndexNetworker>().NetworkpickupIndex;

                    GameObject gameObject = Object.Instantiate<GameObject>(commandCubePrefab, tf.position, tf.rotation);
                    gameObject.GetComponent<PickupIndexNetworker>().NetworkpickupIndex = pickupIndex;
                    gameObject.GetComponent<PickupPickerController>().SetOptionsFromPickupForCommandArtifact(pickupIndex);
                    destroyObjects.Add(a.Key, a.Value);
                    spawnObjects.Add(gameObject);
                }
            }
            foreach (KeyValuePair<NetworkInstanceId, NetworkIdentity> a in destroyObjects)
            {
                NetworkServer.Destroy(a.Value.gameObject);
            }
            foreach (GameObject a in spawnObjects)
            {
                NetworkServer.Spawn(a);
            }
        }
    }


}