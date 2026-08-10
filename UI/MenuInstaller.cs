using System;
using System.Collections;
using System.Collections.Generic;
using YoYoStudio;
using YoYoStudio.Core.Utils;
using YoYoStudio.GUI;
using YoYoStudio.GUI.Gadgets;
using YoYoStudio.GUI.Layout;

namespace DamirLut.DebuggerDump.UI;

internal static class MenuInstaller
{
    public static bool EnsureMenuEntry(ButtonClick onExportCsv, ButtonClick onExportJson)
    {
        try
        {
            DesktopDetails dd = IDE.WindowManager.GetDesktopDetails(WindowManager.CurrentDesktopID);
            MenuBarEntry debuggerEntry = FindDebuggerMenuEntry(dd.stackPanel)
                ?? FindDebuggerMenuEntry(dd.MasterMenuBar);

            if (debuggerEntry == null)
            {
                YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: debugger MenuBarEntry not found");
                return false;
            }

            ContextMenu contextMenu = debuggerEntry.Menu;
            if (contextMenu == null)
            {
                return false;
            }

            ContextMenu exportMenu = new ContextMenu();
            exportMenu.AddEntry("Export CSV", onExportCsv);
            exportMenu.AddEntry("Export JSON", onExportJson);
            SubMenuEntry subMenu = contextMenu.AddSubMenuEntry("Export", exportMenu);
            if (subMenu == null)
            {
                YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: failed to add Export submenu");
                return false;
            }

            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: Export submenu added to Debugger menu");
            return true;
        }
        catch (Exception ex)
        {
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: EnsureMenuEntry error: {0}", ex.Message);
            return false;
        }
    }

    private static MenuBarEntry FindDebuggerMenuEntry(GUIBase root)
    {
        if (root == null)
        {
            return null;
        }

        foreach (var gadget in GetChildren(root))
        {
            if (gadget is MenuBarEntry entry && entry.LocalisedLabel is "Debugger_MenuBar_Debugger")
            {
                return entry;
            }
            if (gadget is GUIBase child)
            {
                var result = FindDebuggerMenuEntry(child);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    private static IEnumerable<IGadget> GetChildren(GUIBase parent)
    {
        var prop = parent.GetType().GetProperty("StackedGadgets");
        if (prop == null)
        {
            yield break;
        }

        if (prop.GetValue(parent) is not IEnumerable children)
        {
            yield break;
        }

        foreach (var child in children)
        {
            if (child is IGadget gadget)
            {
                yield return gadget;
            }
        }
    }
}
