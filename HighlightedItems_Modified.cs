// ✅ Modified with move counter by ChatGPT

using System.Windows.Forms;
using HighlightedItems.Utils;
using ExileCore2;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.MemoryObjects;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExileCore2.Shared.Enums;
using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared;
using ExileCore2.Shared.Helpers;
using ItemFilterLibrary;
using RectangleF = ExileCore2.Shared.RectangleF;
using Vector2 = System.Numerics.Vector2;

namespace HighlightedItems;

public class HighlightedItems : BaseSettingsPlugin<Settings>
{
    private SyncTask<bool> _currentOperation;
    private string _customStashFilter = "";
    private string _customInventoryFilter = "";
    private int _moveSuccessCount = 0; // ✅ ตัวนับการย้ายเสร็จ

    private record QueryOrException(ItemQuery Query, Exception Exception);

    private readonly ConditionalWeakTable<string, QueryOrException> _queries = [];

    private bool MoveCancellationRequested => Settings.CancelWithRightMouseButton && (Control.MouseButtons & MouseButtons.Right) != 0;
    private IngameState InGameState => GameController.IngameState;
    private Vector2 WindowOffset => GameController.Window.GetWindowRectangleTimeCache.TopLeft;

    public override bool Initialise()
    {
        Graphics.InitImage(Path.Combine(DirectoryFullName, "images\pick.png").Replace('\', '/'), false);
        Graphics.InitImage(Path.Combine(DirectoryFullName, "images\pickL.png").Replace('\', '/'), false);

        return true;
    }

    public override void Render()
    {
        // ✅ แสดงจำนวนที่ย้ายสำเร็จ
        Graphics.DrawText($"Moved: {_moveSuccessCount}", new Vector2(10, 10), Color.Yellow);
    }

    private async SyncTask<bool> MoveItemsToInventory(List<NormalInventoryItem> items)
    {
        if (!await MoveItemsCommonPreamble())
        {
            return false;
        }

        var prevMousePos = Mouse.GetCursorPosition();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            _itemsToMove = items[i..].Select(x => x.GetClientRectCache).ToList();
            if (MoveCancellationRequested)
            {
                _itemsToMove = null;
                return false;
            }

            if (!IsStashSourceOpened || !InGameState.IngameUi.InventoryPanel.IsVisible || IsInventoryFull())
            {
                break;
            }

            await MoveItem(item.GetClientRect().Center);
            _moveSuccessCount++; // ✅ เพิ่มตัวนับหลังจากย้าย
        }

        Mouse.moveMouse(prevMousePos);
        _itemsToMove = null;
        return true;
    }

    private async SyncTask<bool> MoveItemsToStash(List<ServerInventory.InventSlotItem> items)
    {
        if (!await MoveItemsCommonPreamble())
        {
            return false;
        }

        var prevMousePos = Mouse.GetCursorPosition();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            _itemsToMove = items[i..].Select(x => x.GetClientRect()).ToList();
            if (MoveCancellationRequested)
            {
                _itemsToMove = null;
                return false;
            }

            if (!InGameState.IngameUi.InventoryPanel.IsVisible || !IsStashTargetOpened)
            {
                break;
            }

            await MoveItem(item.GetClientRect().Center);
            _moveSuccessCount++; // ✅ เพิ่มตัวนับหลังจากย้าย
        }

        Mouse.moveMouse(prevMousePos);
        _itemsToMove = null;
        return true;
    }
}