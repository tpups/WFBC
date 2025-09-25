using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using System.Linq;
using System;

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
        
        protected Standings? standings;
        protected SeasonSettings? seasonSettings;
        protected bool isEditingSeasonSettings = false;
        protected bool isSavingSettings = false;
        protected string saveMessage = string.Empty;
        protected int selectedYear = DateTime.Now.Year;

        protected override async Task OnInitializedAsync()
        {
            // Initialize objects here instead of at field level to avoid circular references
            standings = new Standings();
            seasonSettings = new SeasonSettings();
            
            await LoadSeasonSettings();
        }

        protected async Task LoadSeasonSettings()
        {
            try
            {
                var response = await AuthorizedClient.Client.GetAsync($"api/SeasonSettings/{selectedYear}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        seasonSettings = await response.Content.ReadFromJsonAsync<SeasonSettings>();
                    }
                    else
                    {
                        // Empty response, create default settings
                        seasonSettings = new SeasonSettings(selectedYear);
                    }
                }
                else
                {
                    // API returned non-success status, create default settings
                    seasonSettings = new SeasonSettings(selectedYear);
                    saveMessage = $"No settings found for {selectedYear}. Using defaults (March 1 - October 31).";
                }
            }
            catch (Exception ex)
            {
                saveMessage = $"Error loading settings: {ex.Message}";
                seasonSettings = new SeasonSettings(selectedYear);
            }
        }

        protected async Task SaveSeasonSettings()
        {
            if (seasonSettings == null) return;
            
            try
            {
                isSavingSettings = true;
                saveMessage = string.Empty;

                seasonSettings.Year = selectedYear;
                var response = await AuthorizedClient.Client.PutAsJsonAsync("api/SeasonSettings", seasonSettings);
                
                if (response.IsSuccessStatusCode)
                {
                    saveMessage = "Season settings saved successfully!";
                    isEditingSeasonSettings = false;
                }
                else
                {
                    saveMessage = "Error saving settings. Please try again.";
                }
            }
            catch (Exception ex)
            {
                saveMessage = $"Error saving settings: {ex.Message}";
            }
            finally
            {
                isSavingSettings = false;
                StateHasChanged();
            }
        }

        protected void ToggleEditSeasonSettings()
        {
            isEditingSeasonSettings = !isEditingSeasonSettings;
            if (!isEditingSeasonSettings)
            {
                saveMessage = string.Empty;
            }
        }

        protected async Task OnYearChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int year))
            {
                selectedYear = year;
                await LoadSeasonSettings();
                StateHasChanged();
            }
        }
    }
}
