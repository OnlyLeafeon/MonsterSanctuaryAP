using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using UnityEngine;

namespace MonsterSanctuaryAP.ArchipelagoClient
{
    public static class ApState
    {
        public static ArchipelagoSession Session { get; private set; }
        public static bool Authenticated { get; private set; }
        public static bool IsConnected => Session?.Socket?.Connected ?? false;

        public static string Seed
        {
            get
            {
                if (Session == null || !Authenticated) return null;
                return Session.RoomState?.Seed;
            }
        }

        public static event Action<ItemInfo> ItemReceived;

        public static void Connect(string hostName, string slotName, string password)
        {
            Disconnect();

            try
            {
                string host = hostName;
                int port = 38281;
                if (hostName.Contains(":"))
                {
                    var parts = hostName.Split(':');
                    host = parts[0];
                    int.TryParse(parts[1], out port);
                }

                Session = ArchipelagoSessionFactory.CreateSession(host, port);

                var result = Session.TryConnectAndLogin(
                    "Monster Sanctuary",
                    slotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(0, 5, 1),
                    null,
                    null,
                    password,
                    true
                );

                if (result is LoginSuccessful login)
                {
                    Authenticated = true;
                    Session.Items.ItemReceived += OnItemReceived;
                }
                else if (result is LoginFailure failure)
                {
                    Debug.LogError($"[AP] Login failed: {string.Join(", ", failure.Errors)}");
                    Authenticated = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AP] Connection error: {ex.Message}");
                Authenticated = false;
            }
        }

        private static void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (helper.Any())
            {
                var item = helper.DequeueItem();
                ItemReceived?.Invoke(item);
            }
        }

        public static int GetSeedHash()
        {
            string seed = Seed;
            if (string.IsNullOrEmpty(seed)) return 0;
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < seed.Length; i++)
                {
                    hash ^= seed[i];
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }

        public static bool IsLocationChecked(long locationId)
        {
            return Session != null && Session.Locations.AllLocationsChecked.Contains(locationId);
        }

        public static void CheckLocation(long locationId)
        {
            CheckLocations(locationId);
        }

        public static void CheckLocations(params long[] locationIds)
        {
            if (!IsConnected) return;

            long[] toCheck = locationIds.Where(id => !IsLocationChecked(id)).ToArray();
            if (toCheck.Length == 0) return;

            System.Threading.Tasks.Task.Run(() =>
            {
                try { Session.Locations.CompleteLocationChecksAsync(toCheck); }
                catch (Exception ex) { Debug.LogError($"[AP] Failed to send checks: {ex}"); }
            }).ConfigureAwait(false);
        }

        public static void Disconnect()
        {
            if (Session != null)
            {
                try { Session.Socket.DisconnectAsync(); } catch { }
                Session = null;
            }
            Authenticated = false;
        }
    }

    public static class ApData
    {
        public static bool HasApDataFile()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("AP_Host", ""));
        }

        public static ApDataFile CurrentFile { get; } = new();

        public class ApDataFile
        {
            public ConnectionInfo ConnectionInfo { get; } = new();
        }

        public class ConnectionInfo
        {
            public string HostName
            {
                get => PlayerPrefs.GetString("AP_Host", "");
                set => PlayerPrefs.SetString("AP_Host", value);
            }

            public string SlotName
            {
                get => PlayerPrefs.GetString("AP_Slot", "");
                set => PlayerPrefs.SetString("AP_Slot", value);
            }

            public string Password
            {
                get => PlayerPrefs.GetString("AP_Password", "");
                set => PlayerPrefs.SetString("AP_Password", value);
            }
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
