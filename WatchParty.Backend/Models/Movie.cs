using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WatchParty.Backend.Models;

public class Movie
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("videoUrl")]
    public string VideoUrl { get; set; } = null!;

    [BsonElement("thumbnail")]
    public string? Thumbnail { get; set; }

    [BsonElement("duration")]
    public int Duration { get; set; }

    [BsonElement("genre")]
    public string? Genre { get; set; }

    [BsonElement("year")]
    public int? Year { get; set; }

    [BsonElement("addedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AddedBy { get; set; } = null!;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isApproved")]
    public bool IsApproved { get; set; } = false;

    [BsonElement("viewCount")]
    public int ViewCount { get; set; } = 0;
}
