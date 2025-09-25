using System.Collections.Generic;
using WFBC.Shared.Models;

namespace WFBC.Server.Interface
{
    public interface ISeasonSettings
    {
        List<SeasonSettings> GetAllSeasonSettings();
        SeasonSettings GetSeasonSettings(int year);
        void AddSeasonSettings(SeasonSettings seasonSettings);
        void UpdateSeasonSettings(SeasonSettings seasonSettings);
        void DeleteSeasonSettings(int year);
    }
}
