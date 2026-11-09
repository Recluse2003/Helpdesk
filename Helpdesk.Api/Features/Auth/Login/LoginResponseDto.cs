namespace Helpdesk.Api.Features.Auth.Login
{
    /// <summary>
    /// Represents the output of the login command handler. 
    /// </summary>
    /// <param name="AccessToken">The newly generated access token to be sent to the frontend.</param>
    /// <param name="RefreshToken">The newly generated refresh token to be sent to the frontend.</param>
    public record LoginResponseDto(string AccessToken, string RefreshToken);
}
