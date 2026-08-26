namespace University_Agent_System.Services
{
    using PhoneNumbers;

    public static class PhoneNumberHelper
    {
        public static bool TryNormalizeToE164(
            string iso2CountryCode,
            string nationalPhone,
            out string e164Phone,
            out string errorMessage)
        {
            e164Phone = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(iso2CountryCode))
            {
                errorMessage = "Country is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(nationalPhone))
            {
                errorMessage = "Phone number is required.";
                return false;
            }

            try
            {
                var phoneUtil = PhoneNumberUtil.GetInstance();

                string cleaned = nationalPhone.Trim();

                var parsed = phoneUtil.Parse(cleaned, iso2CountryCode.ToUpper());

                if (!phoneUtil.IsValidNumberForRegion(parsed, iso2CountryCode.ToUpper()))
                {
                    errorMessage = "Phone number is not valid for the selected country.";
                    return false;
                }

                e164Phone = phoneUtil.Format(parsed, PhoneNumberFormat.E164);
                return true;
            }
            catch (NumberParseException ex)
            {
                errorMessage = "Invalid phone number format: " + ex.Message;
                return false;
            }
            catch
            {
                errorMessage = "Invalid phone number.";
                return false;
            }
        }
    }
}
