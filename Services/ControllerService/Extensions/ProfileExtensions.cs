using System;
using System.Collections.Generic;
using System.Linq;
using RegexExtensions;
using SportDJ.Commons.Models.Records;
using SportDJ.Services.ControllerService.Records;

namespace SportDJ.Services.ControllerService.Extensions;

internal static class ProfileExtensions
{
    #region Public Methods

    public static Profile GetProfile(this IEnumerable<Profile> profiles, Controller controller)
    {
        var result = profiles
            .Where(p => controller.Name.IsMatchOrEmptyPattern(p?.Match?.ProductNameIncluded)
                && !controller.Name.IsMatch(p?.Match?.ProductNameExcluded)).FirstOrDefault();

        return result;
    }

    public static byte[] Parse(this string hex) => hex
        .Split(' ')
        .Select(b => Convert.ToByte(b, 16)).ToArray();

    #endregion Public Methods
}