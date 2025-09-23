using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using System.Linq;

namespace WFBC.Client.Pages.Commish
{
    public class StandingsModel : ComponentBase
    {
        [Inject]
        public AuthorizedClient AuthorizedClient { get; set; }
        [Inject]
        public PublicClient PublicClient { get; set; }
        [Inject]
        public NavigationManager UrlNavigationManager { get; set; }
        
        protected Standings standings = new Standings();
    }
}
