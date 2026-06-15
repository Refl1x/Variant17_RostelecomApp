using System.Text.Json.Serialization;

namespace RostelecomAppeals.Shared;

public sealed class AuthSession
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("token_type")] public string TokenType { get; set; } = "bearer";
    [JsonPropertyName("user")] public SupabaseUser? User { get; set; }
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsExpired => string.IsNullOrWhiteSpace(AccessToken) || DateTime.UtcNow > SavedAtUtc.AddSeconds(Math.Max(30, ExpiresIn - 60));
}

public sealed class SupabaseUser
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
}

public sealed class RoleDto
{
    [JsonPropertyName("role_id")] public short RoleId { get; set; }
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public sealed class ProfileDto
{
    [JsonPropertyName("profile_id")] public Guid ProfileId { get; set; }
    [JsonPropertyName("auth_user_id")] public Guid? AuthUserId { get; set; }
    [JsonPropertyName("full_name")] public string FullName { get; set; } = "";
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("role_id")] public short RoleId { get; set; }
    [JsonPropertyName("is_blocked")] public bool IsBlocked { get; set; }
    [JsonPropertyName("roles")] public RoleDto? Role { get; set; }

    public string RoleCode => Role?.Code ?? "";
    public string RoleName => Role?.Name ?? "";
}

public sealed class DictionaryItem
{
    [JsonPropertyName("type_id")] public short TypeId { get; set; }
    [JsonPropertyName("status_id")] public short StatusId { get; set; }
    [JsonPropertyName("priority_id")] public short PriorityId { get; set; }
    [JsonPropertyName("visibility_id")] public short VisibilityId { get; set; }
    [JsonPropertyName("flag_id")] public short FlagId { get; set; }
    [JsonPropertyName("role_id")] public short RoleId { get; set; }
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("sort_order")] public short SortOrder { get; set; }
    [JsonPropertyName("visible_for_mobile")] public bool VisibleForMobile { get; set; }
    [JsonPropertyName("max_length")] public short MaxLength { get; set; }
}

public sealed class AppealDto
{
    [JsonPropertyName("appeal_id")] public Guid AppealId { get; set; }
    [JsonPropertyName("public_number")] public string? PublicNumber { get; set; }
    [JsonPropertyName("applicant_name")] public string ApplicantName { get; set; } = "";
    [JsonPropertyName("contact_phone")] public string ContactPhone { get; set; } = "";
    [JsonPropertyName("connection_address")] public string ConnectionAddress { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("type_id")] public short TypeId { get; set; }
    [JsonPropertyName("status_id")] public short StatusId { get; set; }
    [JsonPropertyName("priority_id")] public short PriorityId { get; set; }
    [JsonPropertyName("registered_at")] public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("assigned_specialist_id")] public Guid? AssignedSpecialistId { get; set; }
    [JsonPropertyName("created_by")] public Guid? CreatedBy { get; set; }
    [JsonPropertyName("updated_by")] public Guid? UpdatedBy { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("deleted_at")] public DateTime? DeletedAt { get; set; }

    [JsonPropertyName("appeal_types")] public AppealTypeJoin? Type { get; set; }
    [JsonPropertyName("appeal_statuses")] public AppealStatusJoin? Status { get; set; }
    [JsonPropertyName("priorities")] public PriorityJoin? Priority { get; set; }
    [JsonPropertyName("profiles")] public ProfileJoin? AssignedSpecialist { get; set; }

    [JsonIgnore] public string TypeName => Type?.Name ?? TypeId.ToString();
    [JsonIgnore] public string StatusName => Status?.Name ?? StatusId.ToString();
    [JsonIgnore] public string PriorityName => Priority?.Name ?? PriorityId.ToString();
    [JsonIgnore] public string SpecialistName => AssignedSpecialist?.FullName ?? "Не назначен";
}

public sealed class AppealTypeJoin { [JsonPropertyName("name")] public string Name { get; set; } = ""; [JsonPropertyName("code")] public string Code { get; set; } = ""; }
public sealed class AppealStatusJoin { [JsonPropertyName("name")] public string Name { get; set; } = ""; [JsonPropertyName("code")] public string Code { get; set; } = ""; }
public sealed class PriorityJoin { [JsonPropertyName("name")] public string Name { get; set; } = ""; [JsonPropertyName("code")] public string Code { get; set; } = ""; }
public sealed class ProfileJoin { [JsonPropertyName("full_name")] public string FullName { get; set; } = ""; }

public sealed class AppealCommentDto
{
    [JsonPropertyName("comment_id")] public Guid CommentId { get; set; }
    [JsonPropertyName("appeal_id")] public Guid AppealId { get; set; }
    [JsonPropertyName("author_id")] public Guid AuthorId { get; set; }
    [JsonPropertyName("visibility_id")] public short VisibilityId { get; set; }
    [JsonPropertyName("comment_text")] public string CommentText { get; set; } = "";
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("comment_visibility")] public CommentVisibilityJoin? Visibility { get; set; }
    [JsonPropertyName("profiles")] public ProfileJoin? Author { get; set; }
    [JsonIgnore] public string VisibilityName => Visibility?.Name ?? VisibilityId.ToString();
    [JsonIgnore] public string AuthorName => Author?.FullName ?? "";
}

public sealed class CommentVisibilityJoin
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("visible_for_mobile")] public bool VisibleForMobile { get; set; }
}

public sealed class NotificationDto
{
    [JsonPropertyName("notification_id")] public Guid NotificationId { get; set; }
    [JsonPropertyName("recipient_id")] public Guid RecipientId { get; set; }
    [JsonPropertyName("appeal_id")] public Guid? AppealId { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
    [JsonPropertyName("is_read")] public bool IsRead { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
}

public sealed class AppealStatsDto
{
    [JsonPropertyName("period_day")] public DateTime PeriodDay { get; set; }
    [JsonPropertyName("appeal_type")] public string AppealType { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("priority")] public string Priority { get; set; } = "";
    [JsonPropertyName("total_count")] public int TotalCount { get; set; }
}

public sealed class LocalOperation
{
    public Guid OperationId { get; set; } = Guid.NewGuid();
    public string Table { get; set; } = "";
    public string Method { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public DateTime ClientUpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
