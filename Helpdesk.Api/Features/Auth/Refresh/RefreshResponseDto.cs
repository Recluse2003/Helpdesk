namespace Helpdesk.Api.Features.Auth.Refresh
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="AccessToken"></param>
    /// <param name="RefreshToken"></param>
    public record RefreshResponseDto(string AccessToken, string RefreshToken);
}