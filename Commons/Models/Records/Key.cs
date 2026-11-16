using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Models.Records;

public readonly record struct Key(
    KeyType Type,
    int Channel,
    int Number);