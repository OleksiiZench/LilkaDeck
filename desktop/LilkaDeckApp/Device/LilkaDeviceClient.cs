using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LilkaDeckApp.Protocol;
using static LilkaDeckApp.Protocol.LilkaProtocol;

namespace LilkaDeckApp.Device;

public enum PingResult { Alive, Busy, NoAnswer }
public enum SyncStage { Started, FileStarted, FileFinished, Finishing }

public sealed record SyncFile(string Name, byte[] Content);
public sealed record SyncProgress(SyncStage Stage, string? FileName = null, int Percent = 0);

public sealed record DeviceTimeouts
{
    public TimeSpan Command { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan Chunk { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan FinishFile { get; init; } = TimeSpan.FromSeconds(8);
    public TimeSpan ProfileList { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan Download { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan Delete { get; init; } = TimeSpan.FromSeconds(5);
}

/// <summary>The operations the device supports, expressed as typed calls.</summary>
public sealed class LilkaDeviceClient : ILilkaDevice
{
    // A color preview is not worth waiting for: if the device is busy, the next one replaces it.
    private static readonly TimeSpan ColorPreviewWait = TimeSpan.FromMilliseconds(50);

    private readonly DeviceSession _session;
    private readonly DeviceTimeouts _timeouts;

    public LilkaDeviceClient(DeviceSession session, DeviceTimeouts? timeouts = null)
    {
        _session = session;
        _timeouts = timeouts ?? new DeviceTimeouts();
    }

    /// <summary>Busy means another request is running, which is proof enough that the device is alive.</summary>
    public async Task<PingResult> PingAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var result = PingResult.NoAnswer;
        bool ran = await _session.TryRunExclusiveAsync(async exchange =>
        {
            try
            {
                var reply = await exchange.RequestAsync(Commands.Ping, m => m is PongMessage, timeout, cancellationToken);
                if (((PongMessage)reply).Version == ExpectedVersion) result = PingResult.Alive;
            }
            catch (TimeoutException)
            {
                // No answer: the result stays NoAnswer.
            }
        }, TimeSpan.Zero, cancellationToken);

        return ran ? result : PingResult.Busy;
    }

    public Task<IReadOnlyList<int>> GetProfileIdsAsync(CancellationToken cancellationToken) =>
        _session.RunExclusiveAsync(async exchange =>
        {
            var reply = await Within(
                exchange.RequestAsync(Commands.GetProfiles, m => m is ProfileListMessage, _timeouts.ProfileList, cancellationToken),
                DeviceStep.ListProfiles);
            return ((ProfileListMessage)reply).ProfileIds;
        }, cancellationToken);

    public Task<int> CreateProfileAsync(CancellationToken cancellationToken) =>
        _session.RunExclusiveAsync(async exchange =>
        {
            var reply = await Within(
                exchange.RequestAsync(Commands.CreateProfile, m => m is ProfileCreatedMessage, _timeouts.Command, cancellationToken),
                DeviceStep.CreateProfile);
            return ((ProfileCreatedMessage)reply).ProfileId;
        }, cancellationToken);

    /// <summary>Returns false when the device refuses, for example for the last remaining profile.</summary>
    public Task<bool> DeleteProfileAsync(int profileId, CancellationToken cancellationToken) =>
        _session.RunExclusiveAsync(async exchange =>
        {
            var reply = await Within(
                exchange.RequestAsync(Commands.DeleteProfile(profileId), IsDeleteAnswer, _timeouts.Delete, cancellationToken),
                DeviceStep.DeleteProfile);
            return reply is AckMessage;
        }, cancellationToken);

    /// <summary>Returns null when the device has no such file.</summary>
    public Task<byte[]?> DownloadFileAsync(int profileId, string fileName, CancellationToken cancellationToken) =>
        _session.RunExclusiveAsync<byte[]?>(async exchange =>
        {
            var reply = await Within(
                exchange.RequestAsync(Commands.GetFile(profileId, fileName), IsDownloadAnswer, _timeouts.Download, cancellationToken),
                DeviceStep.DownloadFile, fileName);
            return reply is FileContentMessage content ? content.Data : null;
        }, cancellationToken);

    /// <summary>Sends a color preview unless the device is busy; returns whether it was sent.</summary>
    public Task<bool> TrySetColorAsync(string hex, CancellationToken cancellationToken) =>
        _session.TryRunExclusiveAsync(
            exchange => exchange.SendLineAsync(Commands.SetColor(hex), cancellationToken), ColorPreviewWait, cancellationToken);

    public Task SyncAsync(
        int profileId, SyncFile config, IReadOnlyList<SyncFile> icons, IProgress<SyncProgress> progress, CancellationToken cancellationToken) =>
        _session.RunExclusiveAsync(async exchange =>
        {
            var files = new List<SyncFile> { config };
            files.AddRange(icons);

            progress.Report(new SyncProgress(SyncStage.Started));
            await Within(
                exchange.RequestAsync(Commands.StartSync(profileId), Ack(AckKind.SyncStarted), _timeouts.Command, cancellationToken),
                DeviceStep.StartSync);

            for (int i = 0; i < files.Count; i++)
            {
                progress.Report(new SyncProgress(SyncStage.FileStarted, files[i].Name));
                await SendFileAsync(exchange, files[i], cancellationToken);
                progress.Report(new SyncProgress(SyncStage.FileFinished, files[i].Name, (i + 1) * 100 / files.Count));
            }

            progress.Report(new SyncProgress(SyncStage.Finishing));
            await Within(
                exchange.RequestAsync(Commands.EndSync, Ack(AckKind.SyncEnded), _timeouts.Command, cancellationToken),
                DeviceStep.EndSync);
        }, cancellationToken);

    // Every chunk is acknowledged by the device; the last one by "file received" instead of "chunk received".
    // An empty file has no chunks and no final acknowledgement, because the device sends none.
    private async Task SendFileAsync(DeviceSession.Exchange exchange, SyncFile file, CancellationToken cancellationToken)
    {
        await Within(
            exchange.RequestAsync(Commands.StartFile(file.Name, file.Content.Length), Ack(AckKind.FileAccepted), _timeouts.Command, cancellationToken),
            DeviceStep.AcceptFile, file.Name);

        int offset = 0;
        while (offset < file.Content.Length)
        {
            int size = Math.Min(ChunkSize, file.Content.Length - offset);
            bool isLast = offset + size == file.Content.Length;

            await Within(
                exchange.SendBlockAsync(
                    file.Content, offset, size,
                    Ack(isLast ? AckKind.FileReceived : AckKind.ChunkReceived),
                    isLast ? _timeouts.FinishFile : _timeouts.Chunk,
                    cancellationToken),
                isLast ? DeviceStep.FinishFile : DeviceStep.WriteChunk, file.Name, offset + size);

            offset += size;
        }
    }

    private static Func<DeviceMessage, bool> Ack(AckKind kind) =>
        message => message is AckMessage ack && ack.Kind == kind;

    private static bool IsDeleteAnswer(DeviceMessage message) =>
        message is AckMessage { Kind: AckKind.ProfileDeleted } or ErrorMessage { Code: ErrorCodes.CannotDelete };

    private static bool IsDownloadAnswer(DeviceMessage message) =>
        message is FileContentMessage or ErrorMessage { Code: ErrorCodes.FileNotFound };

    private static async Task<T> Within<T>(Task<T> exchange, DeviceStep step, string? fileName = null, int offset = 0)
    {
        try
        {
            return await exchange;
        }
        catch (TimeoutException)
        {
            throw new DeviceTimeoutException(step, fileName, offset);
        }
    }
}
