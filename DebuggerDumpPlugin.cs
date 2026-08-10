using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using CorePlugins;
using CorePlugins.Attributes;
using YoYoStudio;
using YoYoStudio.Core.Utils;
using YoYoStudio.FileAPI;
using YoYoStudio.GMDebug;
using YoYoStudio.GMDebug.Debugger;
using YoYoStudio.GMDebug.Networking;
using YoYoStudio.Plugins;
using DamirLut.DebuggerDump.Models;
using DamirLut.DebuggerDump.Services;
using DamirLut.DebuggerDump.UI;

namespace DamirLut.DebuggerDump;

[SupportedOSPlatform("windows")]
public sealed class DebuggerDumpPlugin : IPlugin, IDisposable
{
    private long ideLoadedCommandId;
    private long exportCsvCommandId;
    private long exportJsonCommandId;
    private bool unloaded;
    private bool connected;
    private bool menuAdded;

    private List<ExportNode> exportTree;

    public string Name => "DebuggerDump";

    public string FeatureFlag => "";

    public void Initialise(out List<PluginFunction> pluginCommands)
    {
        pluginCommands = new List<PluginFunction>();

        ideLoadedCommandId = Command.add(
            new Action(OnIdeLoaded),
            "ide_loaded",
            "",
            "DebuggerDump: IDE loaded",
            "function");

        exportCsvCommandId = Command.add(
            new Action(OnExportCsvClicked),
            "debugger_dump_export_csv",
            "",
            "DebuggerDump: Export profiling data to CSV",
            "");

        exportJsonCommandId = Command.add(
            new Action(OnExportJsonClicked),
            "debugger_dump_export_json",
            "",
            "DebuggerDump: Export profiling data to JSON",
            "");

        YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: initialised");
    }

    public void Unload()
    {
        unloaded = true;
        if (ideLoadedCommandId != 0)
        {
            Command.remove(ideLoadedCommandId);
            ideLoadedCommandId = 0;
        }
        if (exportCsvCommandId != 0)
        {
            Command.remove(exportCsvCommandId);
            exportCsvCommandId = 0;
        }
        if (exportJsonCommandId != 0)
        {
            Command.remove(exportJsonCommandId);
            exportJsonCommandId = 0;
        }
    }

    public void Dispose()
    {
        Unload();
    }

    private void OnIdeLoaded()
    {
        if (unloaded)
        {
            return;
        }

        YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: loaded");
        DebuggerManager.OnProfilingDataRebuilt.DoEveryTime(OnProfilingDataRebuilt);
        DebuggerManager.OnConnectionChanged.DoEveryTime(OnConnectionChanged);
    }

    private void OnConnectionChanged(eConnectionState _connection)
    {
        YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: OnConnectionChanged state={0}", _connection);
        connected = _connection == eConnectionState.Connected;
    }

    private void OnProfilingDataRebuilt(List<ProfileNode> _nodes)
    {
        YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: OnProfilingDataRebuilt connected={0} menuAdded={1}", connected, menuAdded);
        if (connected && !menuAdded)
        {
            menuAdded = MenuInstaller.EnsureMenuEntry(OnExportCsvClicked, OnExportJsonClicked);
        }
    }

    private void OnExportCsvClicked()
    {
        if (unloaded)
        {
            return;
        }

        if (!PrepareExport())
        {
            return;
        }

        try
        {
            var sfd = FileSystem.SaveFileDialog();
            if (sfd == null)
            {
                YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: SaveFileDialog not available");
                return;
            }
            sfd.Filters.Add(new Tuple<string, string>("CSV Files (*.csv)", "*.csv"));
            sfd.ShowDialog(
                new Core.CoreOS.FileAPI.FileOpCompleteCallback(OnExportCsvComplete),
                new Core.CoreOS.FileAPI.FileOpErrorCallback(OnExportError));
        }
        catch (Exception ex)
        {
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: failed to show save dialog: {0}", ex.Message);
        }
    }

    private void OnExportJsonClicked()
    {
        if (unloaded)
        {
            return;
        }

        if (!PrepareExport())
        {
            return;
        }

        try
        {
            var sfd = FileSystem.SaveFileDialog();
            if (sfd == null)
            {
                YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: SaveFileDialog not available");
                return;
            }
            sfd.Filters.Add(new Tuple<string, string>("JSON Files (*.json)", "*.json"));
            sfd.ShowDialog(
                new Core.CoreOS.FileAPI.FileOpCompleteCallback(OnExportJsonComplete),
                new Core.CoreOS.FileAPI.FileOpErrorCallback(OnExportError));
        }
        catch (Exception ex)
        {
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: failed to show save dialog: {0}", ex.Message);
        }
    }

    private bool PrepareExport()
    {
        List<ProfileNode> profileData = DebuggerManager.GetProfileData();
        ProfileStats stats = DebuggerManager.GetProfileStats();

        if (profileData == null || profileData.Count == 0)
        {
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: no profile data to export");
            return false;
        }

        exportTree = ProfileExporter.TransformTree(profileData, stats);
        return true;
    }

    private void OnExportCsvComplete(object _result, object _userData)
    {
        string filePath = _result as string;
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }
        try
        {
            ProfileExporter.WriteCsvFile(filePath, exportTree);
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: exported CSV to {0}", filePath);
        }
        catch (Exception ex)
        {
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: failed to write CSV: {0}", ex.Message);
        }
    }

    private void OnExportJsonComplete(object _result, object _userData)
    {
        string filePath = _result as string;
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }
        try
        {
            ProfileExporter.WriteJsonFile(filePath, exportTree);
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: exported JSON to {0}", filePath);
        }
        catch (Exception ex)
        {
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: failed to write JSON: {0}", ex.Message);
        }
    }

    private void OnExportError(Core.CoreOS.FileAPI.FileError _result, string _message, object _userData)
    {
        if (_result != Core.CoreOS.FileAPI.FileError.Cancelled)
        {
            YoYoStudio.Core.Utils.Log.WriteLine(eLog.Default, "[DebuggerDump]: save error: {0}", _message);
        }
    }
}
