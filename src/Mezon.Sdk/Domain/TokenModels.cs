namespace Mezon.Sdk.Domain;


public sealed record SendTokenData
{
    public required string SenderId { get; init; }
    public required string SenderName { get; init; }
    public required string ReceiverId { get; init; }
    public required int Amount { get; init; }
    public string? Note { get; init; }
    public string? ExtraAttribute { get; init; }
    public long? Timestamp { get; init; }
}

public sealed record TokenSentEvent
{
    public string? SenderId { get; init; }
    public string? SenderName { get; init; }
    public required string ReceiverId { get; init; }
    public required int Amount { get; init; }
    public string? Note { get; init; }
    public string? ExtraAttribute { get; init; }
    public string? TransactionId { get; init; }
}

public sealed record ZkProofRequest
{
    public required string UserId { get; init; }
    public required string EphemeralPublicKey { get; init; }
    public required string Jwt { get; init; }
    public required string Address { get; init; }
}

public sealed record ZkProofResponse
{
    public string? ZkProof { get; init; }
    public string? ZkPub { get; init; }
}

public sealed record SendTokenResult
{
    public string? TxHash { get; init; }
    public bool? Ok { get; init; }
    public string? Error { get; init; }
}

public sealed record EphemeralKeyPair
{
    public required string PublicKey { get; init; }
    public required string PrivateKey { get; init; }
}
