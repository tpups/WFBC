using WFBC.Shared.Models;
using System;

namespace WFBC.Server.Interface
{
    public interface IStandings
    {
        public Standings GetStandingsByDate(DateTime date);
    }
}
