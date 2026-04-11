using WFBC.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WFBC.Server.Interface
{
    public interface IBoxScore
    {
        Task<BoxScoreImportResult> ImportBoxScores(string year, List<BoxScoreEntry> entries, string type);
    }
}