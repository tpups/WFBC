#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using WFBC.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;
using System.Net.Http.Json;

namespace WFBC.Client.Pages.Commish
{
    public class BuildUpdateStandingsModel : ComponentBase, IAsyncDisposable
    {
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected IAccessTokenProvider AccessTokenProvider { get; set; } = default!;
        [Inject] protected AuthorizedClient AuthorizedClient { get; set; } = default!;
        
        protected string Title = "Build Season Standings";
        protected List<string> availableYears = new List<string>();
        protected string selectedYear = "";
        protected bool isCalculating = false;
        protected string calculationMessage = "";
        protected string errorMessage = "";
        private HubConnection? hubConnection;
        protected object formModel = new object(); // Simple model for EditForm
        protected string? currentProgressGroupId = null;
        protected bool showConfirmationDialog = false;
        protected string confirmationMessage = "";

        protected override async Task OnInitializedAsync()
        {
            await LoadAvailableYears();
        }

        public async ValueTask DisposeAsync()
        {
            if (hubConnection is not null)
            {
                await hubConnection.DisposeAsync();
            }
        }

        private async Task LoadAvailableYears()
        {
            try
            {
                availableYears = await AuthorizedClient.Client.GetFromJsonAsync<List<string>>("/api/RotisserieStandings/years");
                selectedYear = availableYears.LastOrDefault() ?? "";
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading available years: {ex.Message}";
            }
        }

        protected async Task BuildStandings()
        {
            if (string.IsNullOrEmpty(selectedYear))
            {
                errorMessage = "Please select a year";
                return;
            }

            // First check if standings already exist for this year
            try
            {
                calculationMessage = $"Checking existing standings for {selectedYear}...";
                StateHasChanged();

                var checkResponse = await AuthorizedClient.Client.GetFromJsonAsync<StandingsCheckResponse>($"/api/RotisserieStandings/check/{selectedYear}");
                
                if (checkResponse?.Exist == true)
                {
                    var lastUpdatedText = checkResponse.LastUpdated.HasValue 
                        ? checkResponse.LastUpdated.Value.ToString("MMMM dd, yyyy 'at' h:mm tt")
                        : "Unknown date";
                    
                    confirmationMessage = $"Standings for {selectedYear} already exist (last updated: {lastUpdatedText}). " +
                                        $"Found {checkResponse.RecordCount} records. Do you want to recalculate and overwrite them?";
                    showConfirmationDialog = true;
                    calculationMessage = "";
                    StateHasChanged();
                    return;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error checking existing standings: {ex.Message}";
                calculationMessage = "";
                StateHasChanged();
                return;
            }

            // If no existing standings, proceed with calculation
            await StartCalculation();
        }

        protected async Task ConfirmOverwrite()
        {
            showConfirmationDialog = false;
            StateHasChanged();
            await StartCalculation();
        }

        protected void CancelOverwrite()
        {
            showConfirmationDialog = false;
            confirmationMessage = "";
            StateHasChanged();
        }

        private async Task StartCalculation()
        {
            isCalculating = true;
            calculationMessage = $"Initializing calculation for {selectedYear}...";
            errorMessage = "";
            StateHasChanged();

            try
            {
                // Create SignalR connection
                var baseUrl = Navigation.BaseUri.TrimEnd('/');
                hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{baseUrl}/progressHub", options =>
                    {
                        options.AccessTokenProvider = async () =>
                        {
                            var tokenResult = await AccessTokenProvider.RequestAccessToken();
                            if (tokenResult.TryGetToken(out var token))
                            {
                                return token.Value;
                            }
                            return null;
                        };
                    })
                    .Build();

                // Set up progress update handler
                hubConnection.On<string>("ProgressUpdate", (message) =>
                {
                    calculationMessage = message;
                    InvokeAsync(StateHasChanged);
                });

                // Set up completion handler
                hubConnection.On<object>("CalculationComplete", (result) =>
                {
                    calculationMessage = $"Successfully calculated daily standings for {selectedYear}!";
                    isCalculating = false;
                    InvokeAsync(StateHasChanged);
                });

                // Set up error handler
                hubConnection.On<object>("CalculationError", (error) =>
                {
                    errorMessage = $"Error calculating standings: {error}";
                    calculationMessage = "";
                    isCalculating = false;
                    InvokeAsync(StateHasChanged);
                });

                // Start connection
                await hubConnection.StartAsync();

                // Start calculation with progress reporting
                var response = await AuthorizedClient.Client.PostAsync($"/api/RotisserieStandings/calculate-with-progress/{selectedYear}", null);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    calculationMessage = $"API response received, parsing JSON...";
                    StateHasChanged();
                    
                    using var document = JsonDocument.Parse(jsonContent);
                    
                    if (document.RootElement.TryGetProperty("progressGroupId", out var groupIdElement))
                    {
                        var progressGroupId = groupIdElement.GetString();
                        calculationMessage = $"ProgressGroupId received: {progressGroupId}";
                        StateHasChanged();
                        
                        if (!string.IsNullOrEmpty(progressGroupId))
                        {
                            // Store the progress group ID for cancellation
                            currentProgressGroupId = progressGroupId;
                            
                            // Join the progress group
                            await hubConnection.InvokeAsync("JoinProgressGroup", progressGroupId);
                            calculationMessage = $"Connected to real-time progress for {selectedYear}...";
                            StateHasChanged();
                        }
                        else
                        {
                            errorMessage = "ProgressGroupId was empty";
                            isCalculating = false;
                            StateHasChanged();
                        }
                    }
                    else
                    {
                        errorMessage = $"ProgressGroupId not found in response: {jsonContent}";
                        isCalculating = false;
                        StateHasChanged();
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    errorMessage = $"Error starting calculation (Status: {response.StatusCode}): {error}";
                    calculationMessage = "";
                    isCalculating = false;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error calculating standings: {ex.Message}";
                calculationMessage = "";
                isCalculating = false;
                StateHasChanged();
            }
        }

        protected async Task PreviewStandings()
        {
            if (string.IsNullOrEmpty(selectedYear))
            {
                errorMessage = "Please select a year";
                return;
            }

            try
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var previewStandings = await AuthorizedClient.Client.GetFromJsonAsync<List<Standings>>($"/api/RotisserieStandings/preview/{selectedYear}/{today}");
                
                if (previewStandings?.Any() == true)
                {
                    calculationMessage = $"Preview: Found {previewStandings.Count} teams for {selectedYear}";
                    errorMessage = "";
                }
                else
                {
                    calculationMessage = $"No data found for {selectedYear}";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error previewing standings: {ex.Message}";
                calculationMessage = "";
            }
        }

        protected void OnYearChanged(ChangeEventArgs e)
        {
            selectedYear = e.Value?.ToString() ?? "";
            calculationMessage = "";
            errorMessage = "";
        }

        protected async Task CancelCalculation()
        {
            if (!string.IsNullOrEmpty(currentProgressGroupId))
            {
                try
                {
                    var response = await AuthorizedClient.Client.PostAsync($"/api/RotisserieStandings/cancel/{currentProgressGroupId}", null);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        calculationMessage = "Cancellation requested...";
                        isCalculating = false;
                        currentProgressGroupId = null;
                        StateHasChanged();
                    }
                    else
                    {
                        errorMessage = "Failed to cancel calculation";
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error cancelling calculation: {ex.Message}";
                }
            }
            else
            {
                // If no progress group ID, just reset the UI state
                isCalculating = false;
                calculationMessage = "";
                StateHasChanged();
            }
        }

        private async Task ShowProgressAsync(CancellationToken cancellationToken)
        {
            var progressMessages = new[]
            {
                $"Initializing calculation for {selectedYear}...",
                $"Loading team data for {selectedYear}...",
                $"Processing hitting statistics...",
                $"Processing pitching statistics...", 
                $"Calculating quality appearances...",
                $"Computing rotisserie points...",
                $"Building standings rankings...",
                $"Saving standings data...",
                $"Finalizing calculation for {selectedYear}..."
            };

            int messageIndex = 0;
            var startTime = DateTime.Now;

            try
            {
                while (!cancellationToken.IsCancellationRequested && isCalculating)
                {
                    var elapsed = DateTime.Now - startTime;
                    var currentMessage = progressMessages[messageIndex % progressMessages.Length];
                    calculationMessage = $"{currentMessage} ({elapsed:mm\\:ss} elapsed)";
                    
                    StateHasChanged();
                    
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    messageIndex++;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
    }

    public class StandingsCheckResponse
    {
        public string Year { get; set; } = "";
        public bool Exist { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int RecordCount { get; set; }
    }
}
