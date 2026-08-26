namespace University_Agent_System.Services
{
    public interface IWhatsAppService
    {
        Task SendAgentCredentialsWhatsAppAsync(
            string toPhone,
            string agentName,
            int? agentCode,
            string password,
            string loginUrl);
        Task<string> SendTestMessageAsync(string toPhone);
    }


}