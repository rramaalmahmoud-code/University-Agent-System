using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using PhoneNumbers;

public static class PhoneCountryService
{
    public static List<SelectListItem> GetPhoneCountries()
    {
        var phoneUtil = PhoneNumberUtil.GetInstance();

        var items = phoneUtil.GetSupportedRegions()
            .Select(regionCode =>
            {
                string countryName;
                try
                {
                    countryName = new RegionInfo(regionCode).EnglishName;
                }
                catch
                {
                    countryName = regionCode;
                }

                int countryCode = phoneUtil.GetCountryCodeForRegion(regionCode);

                return new SelectListItem
                {
                    Value = regionCode,
                    Text = $"{countryName} (+{countryCode})"
                };
            })
            .OrderBy(x => x.Text)
            .ToList();

        return items;
    }
}