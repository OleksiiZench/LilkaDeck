using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static LilkaDeckApp.Protocol.LilkaProtocol;

namespace LilkaDeckApp.Protocol;

public static class DeviceMessageParser
{
    private static readonly Dictionary<string, AckKind> AckNames = new()
    {
        { Replies.SyncStarted, AckKind.SyncStarted },
        { Replies.FileAccepted, AckKind.FileAccepted },
        { Replies.ChunkReceived, AckKind.ChunkReceived },
        { Replies.FileReceived, AckKind.FileReceived },
        { Replies.SyncEnded, AckKind.SyncEnded },
        { Replies.ProfileDeleted, AckKind.ProfileDeleted },
    };

    /// <summary>Returns null for blank lines. Anything not recognized is passed on as a log line.</summary>
    public static DeviceMessage? Parse(string rawLine)
    {
        string line = rawLine.Trim();
        if (line.Length == 0) return null;

        return ParseKnown(line) ?? new LogMessage(line);
    }

    private static DeviceMessage? ParseKnown(string line)
    {
        if (TryTakePrefix(line, Replies.Execute, out var target)) return new ExecuteMessage(target.Trim());
        if (line.StartsWith(Replies.Pong, StringComparison.Ordinal)) return new PongMessage(VersionOf(line));
        if (TryTakePrefix(line, Replies.ProfileList, out var ids)) return new ProfileListMessage(ParseIds(ids));
        if (TryTakePrefix(line, Replies.Error, out var code)) return new ErrorMessage(code.Trim());

        if (TryTakePrefix(line, Replies.ProfileCreated, out var created))
            return TryParseNumber(created, out var profileId) ? new ProfileCreatedMessage(profileId) : null;

        if (TryTakePrefix(line, Replies.FileSendStart, out var size))
            return TryParseNumber(size, out var bytes) ? new FileSendStartMessage(bytes) : null;

        return AckNames.TryGetValue(line, out var kind) ? new AckMessage(kind) : null;
    }

    private static bool TryTakePrefix(string line, string prefix, out string rest)
    {
        bool matches = line.StartsWith(prefix, StringComparison.Ordinal);
        rest = matches ? line.Substring(prefix.Length) : "";
        return matches;
    }

    private static string VersionOf(string pongLine)
    {
        int colon = pongLine.IndexOf(':');
        return colon < 0 ? "" : pongLine.Substring(colon + 1).Trim();
    }

    // Entries that are not plain numbers are skipped rather than failing the whole list.
    private static List<int> ParseIds(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(entry => TryParseNumber(entry, out _))
            .Select(entry => int.Parse(entry, NumberStyles.None, CultureInfo.InvariantCulture))
            .ToList();

    private static bool TryParseNumber(string text, out int value) =>
        int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
