using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Models.Records
{
    public readonly record struct Key(
        int Channel,
        KeyType Type,
        int Number);
}