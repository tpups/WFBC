using WFBC.Shared.Models;
using System.Collections.Generic;

namespace WFBC.Server.Interface
{
    public interface ITeam
    {
        public List<Team> GetAllTeams();
        public Team GetTeam(string id);
        public void AddTeam(Team team);
        public void UpdateTeam(Team team);
        public void DeleteTeam(string id);
    }
}
