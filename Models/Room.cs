using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WatchParty.Backend.Models;

public class Room
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("code")]
    public string Code { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("creatorId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatorId { get; set; } = null!;

    [BsonElement("creatorUsername")]
    public string CreatorUsername { get; set; } = null!;

    [BsonElement("currentMovie")]
    public MovieInfo? CurrentMovie { get; set; }

    [BsonElement("videoUrl")]
    public string? VideoUrl { get; set; }

    [BsonElement("videoPosition")]
    public double VideoPosition { get; set; } = 0;

    [BsonElement("isPlaying")]
    public bool IsPlaying { get; set; } = false;

    [BsonElement("isPrivate")]
    public bool IsPrivate { get; set; } = false;

    [BsonElement("password")]
    public string? Password { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("maxViewers")]
    public int MaxViewers { get; set; } = 50;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

public class MovieInfo
{
    [BsonElement("id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("thumbnail")]
    public string? Thumbnail { get; set; }
}
