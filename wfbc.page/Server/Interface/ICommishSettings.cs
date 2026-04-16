using WFBC.Shared.Models;

namespace WFBC.Server.Interface
{
    public interface ICommishSettings
    {
        CommishSettings GetCommishSettings();
        void SaveCommishSettings(CommishSettings settings);
    }
}