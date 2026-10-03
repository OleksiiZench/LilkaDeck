using System.Collections.Generic;

namespace LilkaDeckApp.Protocol;

public enum AckKind
{
    SyncStarted, FileAccepted, ChunkReceived, FileReceived, SyncEnded, ProfileDeleted
}

/// <summary>A line sent by the device, classified by what it means.</summary>
public abstract record DeviceMessage;

public sealed record ExecuteMessage(string Target) : DeviceMessage;
public sealed record PongMessage(string Version) : DeviceMessage;
public sealed record AckMessage(AckKind Kind) : DeviceMessage;
public sealed record ProfileCreatedMessage(int ProfileId) : DeviceMessage;
public sealed record ProfileListMessage(IReadOnlyList<int> ProfileIds) : DeviceMessage;

/// <summary>Announces a file; exactly <see cref="Size"/> raw bytes follow the line.</summary>
public sealed record FileSendStartMessage(int Size) : DeviceMessage;

public sealed record ErrorMessage(string Code) : DeviceMessage;
public sealed record LogMessage(string Text) : DeviceMessage;
