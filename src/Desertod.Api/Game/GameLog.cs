namespace Desertod.Api.Game;

internal static partial class GameLog
{
    [LoggerMessage(1000, LogLevel.Information, "Account {AccountId} founded village {Slug}.")]
    public static partial void VillageCreated(this ILogger logger, Guid accountId, string slug);

    [LoggerMessage(1001, LogLevel.Information, "Account {AccountId} joined village {Slug} as citizen {CitizenId}.")]
    public static partial void CitizenJoined(this ILogger logger, Guid accountId, string slug, Guid citizenId);

    [LoggerMessage(1002, LogLevel.Debug, "Governor of village {Slug}: {Action} {Detail}")]
    public static partial void GovernorActed(this ILogger logger, string slug, string action, string detail);

    [LoggerMessage(1003, LogLevel.Debug, "Citizen {CitizenId} of village {Slug}: {Action} {Detail}")]
    public static partial void CitizenActed(this ILogger logger, Guid citizenId, string slug, string action, string detail);

    [LoggerMessage(1010, LogLevel.Debug, "Settled {Settled} of {Count} due citizens of village {VillageId} in {ElapsedMilliseconds} ms.")]
    public static partial void VillageSettled(this ILogger logger, int settled, int count, Guid villageId, long elapsedMilliseconds);

    [LoggerMessage(1011, LogLevel.Debug,
        "Citizen {CitizenId} of village {Slug} finished {TaskId}: produced [{Produced}], carried [{Carried}], project contribution {ProjectContribution}, rare find {RareFind}.")]
    public static partial void CycleSettled(
        this ILogger logger,
        Guid citizenId,
        string slug,
        string taskId,
        string produced,
        string carried,
        int projectContribution,
        bool rareFind);

    [LoggerMessage(1012, LogLevel.Debug, "Village {Slug} started project {ProjectId}.")]
    public static partial void ProjectStarted(this ILogger logger, string slug, string projectId);

    [LoggerMessage(1013, LogLevel.Information, "Village {Slug} completed project {ProjectId}.")]
    public static partial void ProjectCompleted(this ILogger logger, string slug, string projectId);

    [LoggerMessage(1014, LogLevel.Debug, "Village {Slug} opened quest {QuestKind}.")]
    public static partial void QuestOpened(this ILogger logger, string slug, string questKind);

    [LoggerMessage(1015, LogLevel.Information, "Village {Slug} completed quest {QuestKind}.")]
    public static partial void QuestCompleted(this ILogger logger, string slug, string questKind);

    [LoggerMessage(1020, LogLevel.Debug, "Attempt {Attempt} of {MaximumAttempts} failed with {Reason}; retrying.")]
    public static partial void Retrying(this ILogger logger, int attempt, int maximumAttempts, string reason);

    [LoggerMessage(1021, LogLevel.Debug, "Waited {ElapsedMilliseconds} ms for the lock of village {VillageId}.")]
    public static partial void SlowLock(this ILogger logger, long elapsedMilliseconds, Guid villageId);

    [LoggerMessage(1030, LogLevel.Debug, "Found {Citizens} due citizens in {Villages} villages.")]
    public static partial void DueCitizensFound(this ILogger logger, int citizens, int villages);

    [LoggerMessage(1040, LogLevel.Debug, "Sent the events of {EventVillages} villages and the state of {StateVillages} villages in {ElapsedMilliseconds} ms.")]
    public static partial void Published(this ILogger logger, int eventVillages, int stateVillages, long elapsedMilliseconds);

    [LoggerMessage(1050, LogLevel.Debug, "Connection {ConnectionId} of account {AccountId} connected.")]
    public static partial void HubConnected(this ILogger logger, string connectionId, Guid? accountId);

    [LoggerMessage(1051, LogLevel.Debug, "Connection {ConnectionId} disconnected.")]
    public static partial void HubDisconnected(this ILogger logger, Exception? exception, string connectionId);

    [LoggerMessage(1052, LogLevel.Debug, "Connection {ConnectionId} joined village {Slug}.")]
    public static partial void HubJoined(this ILogger logger, string connectionId, string slug);

    [LoggerMessage(1053, LogLevel.Debug, "Connection {ConnectionId} left village {Slug}.")]
    public static partial void HubLeft(this ILogger logger, string connectionId, string slug);

    [LoggerMessage(1060, LogLevel.Debug, "{Method} {Path} of account {AccountId} rejected with {StatusCode} {Code}: {Detail}")]
    public static partial void RequestRejected(
        this ILogger logger,
        string method,
        string path,
        Guid? accountId,
        int statusCode,
        string code,
        string detail);
}
