using wfbc.page.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfbc.page.Server.Interface
{
    public interface IStandings
    {
        public Standings GetStandingsByDate(DateTime date);
    }
}
