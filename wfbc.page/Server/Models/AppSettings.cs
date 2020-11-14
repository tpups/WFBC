namespace wfbc.page.Server.Models
{
    public interface IDatabaseSettings
    {
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }

    public class DatabaseSettings : IDatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IOktaSettings
    {
        string OktaDomain { get; set; }
        string Token { get; set; }
    }

    public class OktaSettings : IOktaSettings
    {
        public string OktaDomain { get; set; }
        public string Token { get; set; }
    }
}
