using BepInEx;
using RoR2;
using System.Text.RegularExpressions;

namespace AlexTheDragon
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.AlexTheDragon.UnlockedItemIntoItemPool", "Unlocked Item into Item Pool", "1.0.0")]
    public class UnlockedItemIntoItemPool : BaseUnityPlugin
    {
        public void Awake()
        {
            On.RoR2.UserAchievementManager.GrantAchievement += (orig, self, achievementDef) =>  //This works on User unlocks, like via UnlockAchievement().
            {
                Logger.LogMessage("User granting achievement: " + achievementDef.nameToken);

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
        }
    }
}