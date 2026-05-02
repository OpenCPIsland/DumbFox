namespace WebAPI.Api.Jgc.routes;

public class CategoryTypes
{
    public const string FAILURE_BY_DESIGN = "FAILURE_BY_DESIGN";
    public const string ACTIONABLE_INPUT = "ACTIONABLE_INPUT";
}

public class InputNameTypes
{
    public const string ProfileEmail = "profile.email";
    public const string ProfileParentEmail = "profile.parentEmail";
    public const string ProfileFirstName = "profile.firstName";
    public const string ProfileLastName = "profile.lastName";
    public const string ProfileUsername = "profile.username";
    public const string ProfileAuthCredentialPassword = "profile.authCredential.password";
    public const string ProfilePassword = "password";
    public const string DisplayNameProposedDisplayName = "displayName.proposedDisplayName";
    public const string pGTOU = "GTOU";
    public const string pPPV2 = "ppV2";
}

public class GenericCode
{
    public const string CLIENT_NOT_FOUND = "CLIENT_NOT_FOUND";
    public const string API_KEY_INVALID = "API_KEY_INVALID";
    public const string PPU_MARKETING = "PPU_MARKETING";
    public const string PPU_LEGAL = "PPU_LEGAL";
    public const string PPU_SECURITY = "PPU_SECURITY";
    public const string MISSING_VALUE = "MISSING_VALUE";
    public const string MALFORMED_INPUT = "MALFORMED_INPUT";
    public const string SERVICE_ERROR = "SERVICE_ERROR";
    public const string AUTHORIZATION_MASE_ERROR = "AUTHORIZATION_MASE_ERROR";
    public const string AUTHORIZATION_INVALID_REFRESH_TOKEN = "AUTHORIZATION_INVALID_REFRESH_TOKEN";
    public const string AUTHORIZATION_INVALID_OR_EXPIRED_TOKEN = "AUTHORIZATION_INVALID_OR_EXPIRED_TOKEN";
    public const string GUEST_GATED_EMBARGOED = "GUEST_GATED_EMBARGOED";
    public const string GUEST_GATED_LOCATION = "GUEST_GATED_LOCATION";
    public const string RATE_LIMITED = "RATE_LIMITED";
}

public class RegisterValidationCode
{
    public const string INVALID_OR_MALFORMED_REQUEST = "INVALID_OR_MALFORMED_REQUEST";
    public const string UNABLE_TO_SEND_PARENT_NOTIFICATION = "UNABLE_TO_SEND_PARENT_NOTIFICATION";
    public const string PARENT_EMAIL_BLACKLISTED = "PARENT_EMAIL_BLACKLISTED";
    public const string INVALID_PASSWORD_USING_PROFILE_INFORMATION = "INVALID_PASSWORD_USING_PROFILE_INFORMATION";
    public const string ACCOUNT_FOUND = "ACCOUNT_FOUND";
    public const string NRT_ACCOUNT = "NRT_ACCOUNT";
    public const string INVALID_VALUE_PASSWORD_LIKE_PHONE_NUMBER = "INVALID_VALUE_PASSWORD_LIKE_PHONE_NUMBER";
    public const string INVALID_VALUE_PASSWORD_MISSING_EXPECTED_CHARS = "INVALID_VALUE_PASSWORD_MISSING_EXPECTED_CHARS";
    public const string INVALID_VALUE_PASSWORD_SIZE = "INVALID_VALUE_PASSWORD_SIZE";
    public const string INUSE_VALUE = "INUSE_VALUE";
    public const string DISALLOWED_VALUE = "DISALLOWED_VALUE";
    public const string INVALID_VALUE = "INVALID_VALUE";
    public const string INVALID_VALUE_FILTHY = "INVALID_VALUE_FILTHY";
    public const string INVALID_VALUE_USERNAME_SIZE = "INVALID_VALUE_USERNAME_SIZE";
}

public class LoginCode
{
    public const string AUTHORIZATION_PARENTAL_CONSENT_REQUIRED = "AUTHORIZATION_PARENTAL_CONSENT_REQUIRED";
    public const string AUTHORIZATION_ACCOUNT_LOCKED_OUT = "AUTHORIZATION_ACCOUNT_LOCKED_OUT";
    public const string AUTHORIZATION_CREDENTIALS = "AUTHORIZATION_CREDENTIALS";
    public const string PROFILE_USER_BANNED = "PROFILE_USER_BANNED";
    public const string PROFILE_DISABLED = "PROFILE_DISABLED";
}