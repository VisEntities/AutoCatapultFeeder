/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Catapult Feeder", "VisEntities", "1.0.1")]
    [Description("Automatically refills catapults with ammo from a linked stash.")]
    public class AutoCatapultFeeder : RustPlugin
    {
        #region Fields

        private static AutoCatapultFeeder _plugin;

        private static readonly Vector3 _stashPosition = new Vector3(-1.25f, 0.8f, 0.1f);
        private static readonly Vector3 _stashRotation = new Vector3(90f, 0f, 90f);

        private const string PREFAB_STASH = "assets/prefabs/deployable/small stash/small_stash_deployed.prefab";
        private const string PREFAB_CATAPULT_AMMO_STORAGE = "assets/content/vehicles/siegeweapons/catapult/subents/catapult.ammo_storage.prefab";
        
        private readonly string[] _supportedAmmo = new string[]
        {
            "catapult.ammo.boulder",
            "catapult.ammo.incendiary",
            "catapult.ammo.explosive"
        };

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            foreach (Catapult catapult in BaseNetworkable.serverEntities.OfType<Catapult>())
            {
                if (catapult == null)
                    continue;

                StashContainer stash = GetStash(catapult);  
                if (stash != null)
                    stash.Kill();  
            }

            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            foreach (Catapult catapult in BaseNetworkable.serverEntities.OfType<Catapult>())
            {
                if (catapult != null && !HasStash(catapult))
                {
                    AttachStash(catapult);
                }
            }
        }

        private void OnEntitySpawned(Catapult catapult)
        {
            if (catapult != null && !HasStash(catapult))
            {
                AttachStash(catapult);
            }
        }

        private void OnSiegeWeaponFire(Catapult catapult, BasePlayer player)
        {
            if (catapult == null || player == null)
                return;

            StashContainer stash = GetStash(catapult);
            if (stash == null)
                return;

            foreach (Item item in stash.inventory.itemList)
            {
                if (item != null && _supportedAmmo.Contains(item.info.shortname))
                {
                    List<CatapultAmmoContainer> catapultAmmoContainers = FindChildrenOfType<CatapultAmmoContainer>(catapult, PREFAB_CATAPULT_AMMO_STORAGE);
                    if (catapultAmmoContainers.Count > 0)
                    {
                        CatapultAmmoContainer ammoContainer = catapultAmmoContainers[0];
                        item.MoveToContainer(ammoContainer.inventory);
                    }
                    break;
                }
            }
        }

        private object CanHideStash(BasePlayer player, StashContainer stash)
        {
            if (player == null || stash == null)
                return null;

            BaseEntity parent = stash.GetParentEntity();
            if (parent != null && parent is Catapult)
                return true;

            return null;
        }

        #endregion Oxide Hooks

        #region Stash Setup

        private StashContainer AttachStash(Catapult catapult)
        {
            StashContainer stash = GameManager.server.CreateEntity(PREFAB_STASH, _stashPosition, Quaternion.Euler(_stashRotation)) as StashContainer;
            if (stash == null)
                return null;

            stash.SetParent(catapult);
            stash.Spawn();
            RemoveProblematicComponents(stash);

            return stash;
        }

        private StashContainer GetStash(Catapult catapult)
        {
            foreach (StashContainer stash in FindChildrenOfType<StashContainer>(catapult, PREFAB_STASH))
            {
                if (stash != null)
                    return stash;
            }
            return null;
        }

        private bool HasStash(Catapult catapult)
        { 
            return FindChildrenOfType<StashContainer>(catapult, PREFAB_STASH).Count > 0;
        }

        #endregion Stash Setup

        #region Helper Functions

        private static void RemoveProblematicComponents(BaseEntity entity, bool removeGroundWatch = true, bool removeColliders = false)
        {
            if (removeColliders)
            {
                foreach (var collider in entity.GetComponentsInChildren<Collider>())
                {
                    if (!collider.isTrigger)
                        UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            if (removeGroundWatch)
            {
                UnityEngine.Object.Destroy(entity.GetComponent<GroundWatch>());
                UnityEngine.Object.Destroy(entity.GetComponent<DestroyOnGroundMissing>());
            }
        }

        private static List<T> FindChildrenOfType<T>(BaseEntity parentEntity, string prefabName = null) where T : BaseEntity
        {
            List<T> foundChildren = new List<T>();
            foreach (BaseEntity child in parentEntity.children)
            {
                T childOfType = child as T;
                if (childOfType != null && (prefabName == null || child.PrefabName == prefabName))
                    foundChildren.Add(childOfType);
            }

            return foundChildren;
        }

        #endregion Helper Functions
    }
}