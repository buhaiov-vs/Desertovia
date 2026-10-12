using Desertod.Domain.Projects;
using Desertod.Domain.Quests;
using Desertod.Domain.Rules;
using Microsoft.AspNetCore.SignalR;

namespace Desertod.Api.Game;

/// <summary>Public, read-only feed of a village. The stream overlay connects without login.</summary>
public sealed class VillageHub : Hub
{
    public static string GroupName(string slug) => $"village:{slug.ToLowerInvariant()}";

    public Task JoinVillage(string slug) => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(slug));

    public Task LeaveVillage(string slug) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(slug));
}

public sealed class VillageNotifier(IHubContext<VillageHub> hub)
{
    public Task VillageUpdated(VillageStateDto state) =>
        hub.Clients.Group(VillageHub.GroupName(state.Slug)).SendAsync("villageUpdated", state);

    public Task CitizenUpdated(string slug, CitizenSummaryDto citizen) =>
        hub.Clients.Group(VillageHub.GroupName(slug)).SendAsync("citizenUpdated", citizen);

    public Task CycleCompleted(string slug, CycleReport report) =>
        hub.Clients.Group(VillageHub.GroupName(slug)).SendAsync("cycleCompleted", report);

    public Task ProjectCompleted(string slug, ProjectCompletion completion) =>
        hub.Clients.Group(VillageHub.GroupName(slug)).SendAsync("projectCompleted", completion);

    public Task QuestCompleted(string slug, QuestCompletion completion) =>
        hub.Clients.Group(VillageHub.GroupName(slug)).SendAsync("questCompleted", completion);
}
