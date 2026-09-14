using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Color = UnityEngine.Color;
using UnityEngine;

namespace MonsterSanctuaryAP.ArchipelagoClient
{
    internal class ItemHistoryEntry
    {
        public string Text { get; set; }
        public float Timer { get; set; } = 0;
        public float Alpha { get; set; } = 1;
    }

    public enum ItemTransferType
    {
        Acquired,
        Received,
        Sent
    }

    public enum ItemClassification
    {
        Normal,
        Progression,
        Useful,
        Trap
    }

    public class ItemTransferNotification
    {
        public string PlayerName { get; set; }
        public string ItemName { get; set; }
        public ItemClassification Classification { get; set; }
        public ItemTransferType Action { get; set; }
    }

    public static class Colors
    {
        public static string Self => "00ff00";
        public static string OtherPlayer => "ffcc00";

        private static readonly Dictionary<ItemClassification, string> ItemColors = new()
        {
            { ItemClassification.Normal, "ffffff" },
            { ItemClassification.Progression, "ff6600" },
            { ItemClassification.Useful, "00ccff" },
            { ItemClassification.Trap, "ff4444" },
        };

        public static string GetItemColor(ItemClassification classification)
        {
            return ItemColors.TryGetValue(classification, out var color) ? color : "ffffff";
        }
    }

    public class ArchipelagoUI : MonoBehaviour
    {
        public int MaxItemHistory = 10;
        public int FontSize = 20;
        public int X = 16;
        public int Y = 50;
        public int Width = 300;
        public int OutlineOffset = 1;
        public bool DrawBox = true;
        public bool FadeOutEntries = true;
        public float FadeOutAfterSeconds = 8f;
        public float FadeOutTime = 2f;

        private GUIStyle _style;
        private readonly List<ItemHistoryEntry> _itemHistory = new();

        private bool _lastKnownConnectionToAp = false;
        private bool _connecting;
        private string _connHost = "";
        private string _connPort = "38281";
        private string _connSlot = "";
        private string _connPassword = "";

        public void Awake()
        {
            _style = new() { richText = true };
            _style.normal.textColor = Color.white;

            ApState.ItemReceived += OnItemReceived;
        }

        private void OnItemReceived(ItemInfo item)
        {
            bool isOwnItem = item.Player.Slot == ApState.Session.ConnectionInfo.Slot;
            var classification = item.Flags switch
            {
                ItemFlags.Advancement => ItemClassification.Progression,
                ItemFlags.Trap => ItemClassification.Trap,
                _ => ItemClassification.Normal
            };

            if (isOwnItem)
            {
                AddItemToHistory(new ItemTransferNotification
                {
                    PlayerName = item.Player.Name,
                    ItemName = item.ItemName,
                    Classification = classification,
                    Action = ItemTransferType.Acquired
                });
            }
            else
            {
                AddItemToHistory(new ItemTransferNotification
                {
                    PlayerName = item.Player.Name,
                    ItemName = item.ItemName,
                    Classification = classification,
                    Action = ItemTransferType.Received
                });
            }
        }

        public void Update()
        {
            if (!FadeOutEntries)
                return;

            List<ItemHistoryEntry> toRemove = new();

            foreach (ItemHistoryEntry entry in _itemHistory)
            {
                entry.Timer += Time.deltaTime;

                if (entry.Timer < FadeOutAfterSeconds)
                    continue;

                entry.Alpha = Mathf.Lerp(1, 0, (entry.Timer - FadeOutAfterSeconds) / FadeOutTime);

                var hex = FloatToHex(entry.Alpha);
                entry.Text = Regex.Replace(entry.Text, @"(?<=color=#[0-9a-f]{6})[0-9a-f]{2}", hex);

                if (entry.Alpha <= 0)
                    toRemove.Add(entry);
            }

            foreach (var entry in toRemove)
                _itemHistory.Remove(entry);

            if (!_lastKnownConnectionToAp && ApState.IsConnected)
            {
                _lastKnownConnectionToAp = true;
            }
            else if (_lastKnownConnectionToAp && !ApState.IsConnected && GameStateManager.Instance.IsExploring())
            {
                _lastKnownConnectionToAp = false;

                if (ApData.HasApDataFile())
                {
                    ShowDisconnectedMessageWithPromptToReconnect();
                }
                else
                {
                    ShowDisconnectedMessage();
                }
            }
        }

        private void ShowDisconnectedMessageWithPromptToReconnect()
        {
            PopupController.Instance.ShowRequest(
                "Disconnected",
                "Disconnected from Archipelago server. Attempt to reconnect?",
                () =>
                {
                    ApState.Connect(
                        ApData.CurrentFile.ConnectionInfo.HostName,
                        ApData.CurrentFile.ConnectionInfo.SlotName,
                        ApData.CurrentFile.ConnectionInfo.Password);

                    if (!ApState.IsConnected)
                    {
                        StartCoroutine(DelayedAction(0.25f, () => ShowFailedToReconnectMessage()));
                    }
                });
        }

        private void ShowFailedToReconnectMessage()
        {
            PopupController.Instance.ShowMessage(
                "Disconnect",
                "Failed to reconnect to the Archipelago server.");
        }

        private void ShowDisconnectedMessage()
        {
            PopupController.Instance.ShowMessage(
                "Disconnect",
                "Disconnected from Archipelago server. It is recommended to exit to the menu and reconnect.");
        }

        private string FloatToHex(float f)
        {
            int val = (int)(f * 255);
            return val.ToString("X2").ToLower();
        }

        public void AddItemToHistory(ItemTransferNotification itemTransfer)
        {
            var entry = new ItemHistoryEntry()
            {
                Text = GetEntryText(itemTransfer.PlayerName, itemTransfer.ItemName, itemTransfer.Classification, itemTransfer.Action)
            };

            if (string.IsNullOrEmpty(entry.Text))
                return;

            _itemHistory.Insert(0, entry);
            if (_itemHistory.Count > MaxItemHistory)
            {
                _itemHistory.RemoveRange(MaxItemHistory, _itemHistory.Count() - MaxItemHistory);
            }
        }

        private string GetEntryText(string playerName, string itemName, ItemClassification classification, ItemTransferType action)
        {
            var itemColor = Colors.GetItemColor(classification);
            if (action == ItemTransferType.Acquired)
            {
                return $"<color=#{Colors.Self}ff>You</color> found your <color=#{itemColor}ff>{itemName}</color>";
            }
            else if (action == ItemTransferType.Received)
            {
                return $"<color=#{Colors.OtherPlayer}ff>{playerName}</color> sent you <color=#{itemColor}ff>{itemName}</color>";
            }
            else if (action == ItemTransferType.Sent)
            {
                return $"<color=#{Colors.Self}ff>You</color> sent <color=#{itemColor}ff>{itemName}</color> to <color=#{Colors.OtherPlayer}ff>{playerName}</color>";
            }

            return "";
        }

        void OnGUI()
        {
            int y = DisplayConnectionInfo();
            DisplayItemHistory(y);
        }

        private int DisplayConnectionInfo()
        {
            string status;
            float boxWidth = 220;

            if (_connecting)
            {
                status = "Connecting...";
            }
            else if (ApState.IsConnected && ApState.Authenticated)
            {
                status = $"Connected ({ApState.Session.ConnectionInfo.Slot})";
                boxWidth = 300;
            }
            else if (ApState.Session != null)
            {
                status = "Status: Authentication failed";
            }
            else
            {
                status = "Not Connected — click to connect";
            }

            var oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Box(new Rect(12, 12, boxWidth, 24), "");
            GUI.color = oldColor;

            _style.normal.textColor = Color.white;
            _style.fontSize = 16;

            if (GUI.Button(new Rect(16, 16, boxWidth - 8, 20), status, _style))
            {
                if (ApState.IsConnected)
                {
                    PopupController.Instance.ShowRequest(
                        "Disconnect",
                        "Disconnect from Archipelago?",
                        () => ApState.Disconnect(),
                        null);
                }
                else if (!_connecting)
                {
                    BeginConnectionFlow();
                }
            }

            return 40;
        }

        private void BeginConnectionFlow()
        {
            _connecting = true;
            _connHost = "";
            _connPort = "38281";
            _connSlot = "";
            _connPassword = "";
            this.StartCoroutine(DelayedAction(0.25f, PromptForHost));
        }

        private void PromptForHost()
        {
            UIController.Instance.NameMenu.Open(
                "Enter Archipelago host address",
                string.IsNullOrEmpty(_connHost) ? "archipelago.gg" : _connHost,
                (string host) =>
                {
                    _connHost = host;
                    this.StartCoroutine(DelayedAction(0.25f, PromptForPort));
                },
                () => _connecting = false,
                NameMenu.ENameType.MapMarker);
        }

        private void PromptForPort()
        {
            UIController.Instance.NameMenu.Open(
                "Enter port number",
                _connPort,
                (string port) =>
                {
                    _connPort = port;
                    this.StartCoroutine(DelayedAction(0.25f, PromptForSlot));
                },
                () => this.StartCoroutine(DelayedAction(0.25f, PromptForHost)),
                NameMenu.ENameType.MapMarker);
        }

        private void PromptForSlot()
        {
            UIController.Instance.NameMenu.Open(
                "Enter your slot name",
                _connSlot,
                (string slot) =>
                {
                    _connSlot = slot;
                    this.StartCoroutine(DelayedAction(0.25f, PromptForPassword));
                },
                () => this.StartCoroutine(DelayedAction(0.25f, PromptForPort)),
                NameMenu.ENameType.MapMarker);
        }

        private void PromptForPassword()
        {
            UIController.Instance.NameMenu.Open(
                "Enter password (leave blank for none)",
                _connPassword,
                (string password) =>
                {
                    _connPassword = password;
                    this.StartCoroutine(DelayedAction(0.25f, ConfirmConnection));
                },
                () => this.StartCoroutine(DelayedAction(0.25f, PromptForSlot)),
                NameMenu.ENameType.MapMarker);
        }

        private void ConfirmConnection()
        {
            var fullAddress = $"{_connHost}:{_connPort}";
            PopupController.Instance.ShowRequest(
                "Confirm Connection",
                $"Host: {fullAddress}\nSlot: {_connSlot}\nPassword: {_connPassword}",
                () => ConnectToArchipelago(fullAddress),
                () => this.StartCoroutine(DelayedAction(0.25f, PromptForHost)));
        }

        private void ConnectToArchipelago(string fullAddress)
        {
            ApData.CurrentFile.ConnectionInfo.HostName = fullAddress;
            ApData.CurrentFile.ConnectionInfo.SlotName = _connSlot;
            ApData.CurrentFile.ConnectionInfo.Password = _connPassword;
            ApData.Save();

            ApState.Connect(fullAddress, _connSlot, _connPassword);

            if (!ApState.IsConnected)
            {
                this.StartCoroutine(DelayedAction(0.5f, () =>
                {
                    PopupController.Instance.ShowMessage(
                        "Connection Failed",
                        "Failed to connect. Check your settings and try again.",
                        () => _connecting = false);
                }));
            }
            else
            {
                _connecting = false;
            }
        }

        private void DisplayItemHistory(int y)
        {
            _style.fontSize = FontSize;
            int height = FontSize * MaxItemHistory;

            if (DrawBox && _itemHistory.Count > 0)
            {
                var alpha = _itemHistory.Max(i => i.Alpha);
                var oldColor = GUI.color;
                GUI.color = new Color(0, 0, 0, alpha * 0.5f);
                GUI.Box(new Rect(X - 3, y + Y - 3, Width + 6, height + 6), "");
                GUI.color = oldColor;
            }

            foreach (var entry in _itemHistory)
            {
                var oldColor = GUI.color;
                GUI.color = new Color(1, 1, 1, entry.Alpha);
                _style.normal.textColor = new Color(1, 1, 1, entry.Alpha);

                DrawTextWithOutline(new Rect(X, y + Y, Width, height), entry.Text, _style, Color.black);

                GUI.color = oldColor;

                y += FontSize;
            }
        }

        private void DrawTextWithOutline(Rect position, string text, GUIStyle style, Color borderColor)
        {
            var backupStyle = style;

            if (OutlineOffset > 0)
            {
                var borderText = Regex.Replace(text, @"\<color=#[0-9a-f]+\>", "");
                borderText = Regex.Replace(borderText, @"\<\/color\>", "");

                var oldColor = style.normal.textColor;
                style.normal.textColor = borderColor;

                position.x -= OutlineOffset;
                GUI.Label(position, borderText, style);
                position.x += (OutlineOffset * 2);
                GUI.Label(position, borderText, style);
                position.x -= OutlineOffset;

                position.y -= OutlineOffset;
                GUI.Label(position, borderText, style);
                position.y += (OutlineOffset * 2);
                GUI.Label(position, borderText, style);
                position.y -= OutlineOffset;

                style.normal.textColor = oldColor;
            }

            GUI.Label(position, text, style);
            style = backupStyle;
        }

        private static System.Collections.IEnumerator DelayedAction(float delay, System.Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }
    }
}
