using WFBC.Shared.Models;
using System.Collections.Generic;

namespace WFBC.Server.Interface
{
    public interface ISeasonTeam
    {
        List<SeasonTeam> GetTeamsForYear(string year);
        SeasonTeam GetSeasonTeam(string year, string id);
        void AddSeasonTeam(string year, SeasonTeam team);
        void UpdateSeasonTeam(string year, SeasonTeam team);
        void DeleteSeasonTeam(string year, string id);
    }
}