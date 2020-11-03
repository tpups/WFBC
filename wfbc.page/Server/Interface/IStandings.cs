using wfbc.page.Shared.Models;
using System;

namespace wfbc.page.Server.Interface
{
    public interface IStandings
    {
        public Standings GetStandingsByDate(DateTime date);
    }
}
