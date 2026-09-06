using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

public static class SegmentMapping
{
    public static bool UsesLegacyBriefing(Segment? segment)
    {
        return SegmentUiConfig.FromJson(segment?.DefaultConfigJson).UsesLegacyBriefing;
    }

    public static SegmentResponse ToResponse(Segment segment)
    {
        var config = SegmentUiConfig.FromJson(segment.DefaultConfigJson);
        return new SegmentResponse(
            segment.Id,
            segment.Name,
            segment.Slug,
            segment.Description,
            segment.TemplateKey,
            config.UsesLegacyBriefing,
            config.DefaultCampaignGoal);
    }

    public static AdminSegmentResponse ToAdminResponse(Segment segment, int campaignsCount)
    {
        return new AdminSegmentResponse(
            segment.Id,
            segment.Name,
            segment.Slug,
            segment.Description,
            segment.TemplateKey,
            segment.DefaultConfigJson,
            segment.IsActive,
            campaignsCount,
            segment.CreatedAt,
            segment.UpdatedAt);
    }

    private sealed record SegmentUiConfig(bool UsesLegacyBriefing, string? DefaultCampaignGoal)
    {
        public static SegmentUiConfig FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new SegmentUiConfig(false, null);
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var legacy = ReadBool(root, "legacyCompatibility")
                    || (root.TryGetProperty("ui", out var ui) && ReadBool(ui, "legacyBriefing"));
                var goal = ReadString(root, "defaultGoal")
                    ?? (root.TryGetProperty("ui", out ui) ? ReadString(ui, "defaultCampaignGoal") : null);
                return new SegmentUiConfig(legacy, goal);
            }
            catch (JsonException)
            {
                return new SegmentUiConfig(false, null);
            }
        }

        private static bool ReadBool(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.True;
        }

        private static string? ReadString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }
}
