using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using DamirLut.DebuggerDump.Models;
using YoYoStudio.GMDebug;
using YoYoStudio.GMDebug.Debugger;
using YoYoStudio.Plugins.ErrorParsing;

namespace DamirLut.DebuggerDump.Services;

internal static class ProfileExporter
{
    public static List<ExportNode> TransformTree(List<ProfileNode> nodes, ProfileStats stats)
    {
        if (nodes == null)
        {
            return new List<ExportNode>();
        }

        var result = new List<ExportNode>(nodes.Count);
        foreach (var node in nodes)
        {
            result.Add(TransformNode(node, stats));
        }
        return result;
    }

    private static ExportNode TransformNode(ProfileNode node, ProfileStats stats)
    {
        string name = ResolveNodeName(node);

        float stepPercent = 0f;
        if (stats != null && stats.FrameTimeTotal != 0f)
        {
            stepPercent = node.TimeMilliseconds / stats.FrameTimeTotal * 100f;
        }

        List<ExportNode> children = null;
        if (node.children != null && node.children.Count > 0)
        {
            children = new List<ExportNode>(node.children.Count);
            foreach (var child in node.children)
            {
                children.Add(TransformNode(child, stats));
            }
        }

        return new ExportNode
        {
            Name = name,
            CallCount = node.CallCount,
            TimeMilliseconds = node.TimeMilliseconds,
            StepPercent = stepPercent,
            Children = children,
        };
    }

    private static string ResolveNodeName(ProfileNode node)
    {
        string name = node.Name;
        if (name.StartsWith("gml_"))
        {
            string resourceName = CommonErrorParsing.GetResourceInfoFromRunnerName(name, out var subinfo, out var _);
            name = string.IsNullOrEmpty(subinfo)
                ? resourceName
                : string.Format("{0} ({1})", resourceName, subinfo);
        }
        return name;
    }

    public static void WriteCsvFile(string filePath, List<ExportNode> nodes)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("\"Name\",\"CallCount\",\"TimeMilliseconds\",\"StepPercent\",\"Level\"");
        FlattenAndWriteCsv(writer, nodes, 0);
    }

    private static void FlattenAndWriteCsv(StreamWriter writer, List<ExportNode> nodes, int level)
    {
        if (nodes == null)
        {
            return;
        }

        foreach (var node in nodes)
        {
            writer.WriteLine("\"{0}\",{1},{2},{3},{4}",
                node.Name.Replace("\"", "\"\""),
                node.CallCount,
                node.TimeMilliseconds.ToString("F3", CultureInfo.InvariantCulture),
                node.StepPercent.ToString("F3", CultureInfo.InvariantCulture),
                level);

            if (node.Children != null)
            {
                FlattenAndWriteCsv(writer, node.Children, level + 1);
            }
        }
    }

    public static void WriteJsonFile(string filePath, List<ExportNode> nodes)
    {
        string json = JsonSerializer.Serialize(nodes, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}
