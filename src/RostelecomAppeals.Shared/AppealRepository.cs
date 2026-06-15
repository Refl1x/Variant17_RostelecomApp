using System.Text.Json;

namespace RostelecomAppeals.Shared;

public sealed class AppealRepository
{
    private readonly SupabaseClient _client;
    private readonly LocalJsonStore _store;
    private readonly AppLogger _logger;

    public AppealRepository(SupabaseClient client, LocalJsonStore store, AppLogger logger)
    {
        _client = client;
        _store = store;
        _logger = logger;
    }

    public Task<List<DictionaryItem>> GetTypesAsync(CancellationToken ct = default) =>
        _client.GetListAsync<DictionaryItem>("appeal_types", "select=*&order=name.asc", ct);

    public Task<List<DictionaryItem>> GetStatusesAsync(CancellationToken ct = default) =>
        _client.GetListAsync<DictionaryItem>("appeal_statuses", "select=*&order=status_id.asc", ct);

    public Task<List<DictionaryItem>> GetPrioritiesAsync(CancellationToken ct = default) =>
        _client.GetListAsync<DictionaryItem>("priorities", "select=*&order=sort_order.asc", ct);

    public Task<List<DictionaryItem>> GetCommentVisibilitiesAsync(CancellationToken ct = default) =>
        _client.GetListAsync<DictionaryItem>("comment_visibility", "select=*&order=visibility_id.asc", ct);

    public Task<List<RoleDto>> GetRolesAsync(CancellationToken ct = default) =>
        _client.GetListAsync<RoleDto>("roles", "select=*&order=role_id.asc", ct);

    public Task<List<ProfileDto>> GetProfilesAsync(CancellationToken ct = default) =>
        _client.GetListAsync<ProfileDto>("profiles", "select=*,roles(*)&order=full_name.asc", ct);

    public async Task<List<ProfileDto>> GetSpecialistsAsync(CancellationToken ct = default)
    {
        var all = await GetProfilesAsync(ct);
        return all.Where(x => x.RoleCode == "specialist" && !x.IsBlocked).ToList();
    }

    public async Task<List<AppealDto>> GetAppealsAsync(CancellationToken ct = default)
    {
        var select = "select=*,appeal_types(name,code),appeal_statuses(name,code),priorities(name,code),profiles!appeals_assigned_specialist_id_fkey(full_name)&deleted_at=is.null&order=registered_at.desc";
        try
        {
            var data = await _client.GetListAsync<AppealDto>("appeals", select, ct);
            await _store.SaveAsync("appeals_cache.json", data, ct);
            return data;
        }
        catch (Exception ex)
        {
            await _logger.WarnAsync("OFFLINE_CACHE", ex.Message);
            return await _store.LoadAsync<List<AppealDto>>("appeals_cache.json", ct) ?? new List<AppealDto>();
        }
    }

    public async Task<List<AppealStatsDto>> GetStatsAsync(CancellationToken ct = default)
    {
        try { return await _client.GetListAsync<AppealStatsDto>("v_appeal_stats", "select=*&order=period_day.desc", ct); }
        catch { return new List<AppealStatsDto>(); }
    }

    public Task<List<AppealCommentDto>> GetCommentsAsync(Guid appealId, CancellationToken ct = default) =>
        _client.GetListAsync<AppealCommentDto>("appeal_comments", $"select=*,comment_visibility(*),profiles(full_name)&appeal_id=eq.{appealId}&deleted_at=is.null&order=created_at.asc", ct);

    public async Task<AppealDto?> SaveAppealAsync(AppealDto appeal, CancellationToken ct = default)
    {
        var errors = Validators.ValidateAppeal(appeal);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

        appeal.UpdatedAt = DateTime.UtcNow;
        appeal.UpdatedBy = _client.CurrentProfile?.ProfileId;
        if (appeal.AppealId == Guid.Empty)
        {
            appeal.AppealId = Guid.NewGuid();
            appeal.CreatedBy = _client.CurrentProfile?.ProfileId;
            appeal.RegisteredAt = DateTime.UtcNow;
            await _logger.InfoAsync("CREATE_APPEAL", appeal.ApplicantName);
            return await WithQueueFallbackAsync(
                () => _client.InsertAsync<AppealDto>("appeals", appeal, ct),
                "appeals", "POST", "appeals", appeal, ct);
        }

        await _logger.InfoAsync("UPDATE_APPEAL", appeal.PublicNumber ?? appeal.AppealId.ToString());
        var payload = new
        {
            applicant_name = appeal.ApplicantName,
            contact_phone = appeal.ContactPhone,
            connection_address = appeal.ConnectionAddress,
            description = appeal.Description,
            type_id = appeal.TypeId,
            status_id = appeal.StatusId,
            priority_id = appeal.PriorityId,
            assigned_specialist_id = appeal.AssignedSpecialistId,
            updated_by = _client.CurrentProfile?.ProfileId,
            updated_at = appeal.UpdatedAt
        };
        return await WithQueueFallbackAsync(
            () => _client.PatchAsync<AppealDto>("appeals", $"appeal_id=eq.{appeal.AppealId}", payload, ct),
            "appeals", "PATCH", $"appeals?appeal_id=eq.{appeal.AppealId}", payload, ct);
    }

    public async Task SoftDeleteAppealAsync(AppealDto appeal, CancellationToken ct = default)
    {
        await _logger.InfoAsync("DELETE_APPEAL", appeal.PublicNumber ?? appeal.AppealId.ToString());
        var payload = new { deleted_at = DateTime.UtcNow, updated_by = _client.CurrentProfile?.ProfileId };
        await WithQueueFallbackAsync<object>(
            async () => await _client.PatchAsync<object>("appeals", $"appeal_id=eq.{appeal.AppealId}", payload, ct),
            "appeals", "PATCH", $"appeals?appeal_id=eq.{appeal.AppealId}", payload, ct);
    }

    public async Task AddCommentAsync(Guid appealId, string text, short visibilityId, bool internalComment, CancellationToken ct = default)
    {
        var errors = Validators.ValidateComment(text, internalComment);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

        var payload = new
        {
            appeal_id = appealId,
            author_id = _client.CurrentProfile?.ProfileId,
            visibility_id = visibilityId,
            comment_text = text
        };
        await _logger.InfoAsync("ADD_COMMENT", appealId.ToString());
        await WithQueueFallbackAsync(
            () => _client.InsertAsync<AppealCommentDto>("appeal_comments", payload, ct),
            "appeal_comments", "POST", "appeal_comments", payload, ct);
    }

    public async Task SetProfileBlockedAsync(ProfileDto profile, bool blocked, CancellationToken ct = default)
    {
        await _client.PatchAsync<ProfileDto>("profiles", $"profile_id=eq.{profile.ProfileId}", new { is_blocked = blocked }, ct);
        await _logger.InfoAsync(blocked ? "BLOCK_USER" : "UNBLOCK_USER", profile.Email ?? profile.FullName);
    }

    public async Task SetProfileRoleAsync(ProfileDto profile, short roleId, CancellationToken ct = default)
    {
        await _client.PatchAsync<ProfileDto>("profiles", $"profile_id=eq.{profile.ProfileId}", new { role_id = roleId }, ct);
        await _logger.InfoAsync("CHANGE_ROLE", profile.Email ?? profile.FullName);
    }

    public async Task<ProfileDto?> CreateUserProfileAsync(string email, string password, string fullName, string phone, short roleId, CancellationToken ct = default)
    {
        var session = await _client.SignUpAsync(email, password, ct);
        var payload = new
        {
            auth_user_id = session.User?.Id,
            full_name = fullName,
            email,
            phone,
            role_id = roleId
        };
        await _logger.InfoAsync("CREATE_USER", email);
        return await _client.InsertAsync<ProfileDto>("profiles", payload, ct);
    }

    public async Task RetryQueuedAsync(CancellationToken ct = default)
    {
        var queue = await _store.LoadAsync<List<LocalOperation>>("sync_queue_local.json", ct) ?? new List<LocalOperation>();
        if (queue.Count == 0) return;

        var done = new List<LocalOperation>();
        foreach (var op in queue.OrderBy(x => x.ClientUpdatedAtUtc))
        {
            try
            {
                using var doc = JsonDocument.Parse(op.PayloadJson);
                var payload = JsonSerializer.Deserialize<object>(op.PayloadJson);
                if (payload == null) continue;
                if (op.Method == "POST") await _client.InsertAsync<object>(op.Table, payload, ct);
                if (op.Method == "PATCH") await _client.PatchAsync<object>(op.Table, op.Endpoint.Contains('?') ? op.Endpoint.Split('?')[1] : string.Empty, payload, ct);
                done.Add(op);
            }
            catch (Exception ex)
            {
                await _logger.WarnAsync("SYNC_RETRY_FAILED", ex.Message);
            }
        }
        queue.RemoveAll(x => done.Any(d => d.OperationId == x.OperationId));
        await _store.SaveAsync("sync_queue_local.json", queue, ct);
    }

    private async Task<T?> WithQueueFallbackAsync<T>(Func<Task<T?>> action, string table, string method, string endpoint, object payload, CancellationToken ct) where T : class
    {
        try { return await action(); }
        catch (Exception ex)
        {
            await _logger.WarnAsync("QUEUE_OPERATION", ex.Message);
            var queue = await _store.LoadAsync<List<LocalOperation>>("sync_queue_local.json", ct) ?? new List<LocalOperation>();
            queue.Add(new LocalOperation
            {
                Table = table,
                Method = method,
                Endpoint = endpoint,
                PayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }),
                ClientUpdatedAtUtc = DateTime.UtcNow
            });
            await _store.SaveAsync("sync_queue_local.json", queue, ct);
            return default;
        }
    }
}
