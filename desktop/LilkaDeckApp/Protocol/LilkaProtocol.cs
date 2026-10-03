using System.Globalization;

namespace LilkaDeckApp.Protocol;

/// <summary>
/// Wire format shared with the firmware (SyncProtocol.h on that side).
/// Changing any of it requires a matching change in the firmware.
/// </summary>
public static class LilkaProtocol
{
    public const int BaudRate = 115200;
    public const int ChunkSize = 256;
    public const string ExpectedVersion = "v1.0";

    public static class Commands
    {
        public const string Ping = "PING";
        public const string GetProfiles = "GET_PROFILES";
        public const string CreateProfile = "PROFILE_CREATE";
        public const string EndSync = "SYNC_END";

        public static string DeleteProfile(int profileId) => $"PROFILE_DELETE:{Number(profileId)}";
        public static string SetColor(string hex) => $"SET_COLOR:{hex}";
        public static string StartSync(int profileId) => $"SYNC_START:{Number(profileId)}";
        public static string GetFile(int profileId, string fileName) => $"FILE_GET:{Number(profileId)}:{fileName}";
        public static string StartFile(string fileName, int size) => $"FILE_START:{fileName}:{Number(size)}";

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public static class ErrorCodes
    {
        public const string FileNotFound = "FILE_NOT_FOUND";
        public const string CannotDelete = "CANNOT_DELETE";
    }

    internal static class Replies
    {
        public const string Execute = "EXECUTE:";
        public const string Pong = "LILKA_PONG";
        public const string ProfileList = "PROFILES:";
        public const string ProfileCreated = "ACK_PROFILE_CREATE:";
        public const string FileSendStart = "FILE_SEND_START:";
        public const string Error = "ERR:";

        public const string SyncStarted = "ACK_SYNC";
        public const string FileAccepted = "ACK_FILE";
        public const string ChunkReceived = "ACK_CHUNK";
        public const string FileReceived = "ACK_DONE";
        public const string SyncEnded = "ACK_END";
        public const string ProfileDeleted = "ACK_PROFILE_DELETE";
    }
}
