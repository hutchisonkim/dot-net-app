// This script illustrates using the shared ApiClient (IHealthStatusProvider) in Unity without a DI container.
// Place this file in a Unity project's Assets/Scripts folder (not compiled as part of the .NET solution).

using System;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetApp.Client.Shared.Contracts; // Generated PlatformApiClient
using Microsoft.Extensions.Configuration;

#if UNITY_2021_1_OR_NEWER
using UnityEngine;
public class SampleHealthStatusBehaviour : MonoBehaviour
{
    [SerializeField]
    private string apiBaseAddress = "http://localhost:5000/"; // Set in Inspector

    private PlatformApiClient? _api;
    private float _nextPoll;
    public float PollIntervalSeconds = 5f;

    void Start()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new System.Collections.Generic.KeyValuePair<string,string?>("ApiBaseAddress", apiBaseAddress) });
        var configuration = configBuilder.Build();

        var http = new HttpClient { BaseAddress = new Uri(apiBaseAddress) };
    _api = new PlatformApiClient(http);
        Debug.Log("SampleHealthStatusBehaviour initialized");
    }

    async void Update()
    {
    if (Time.time < _nextPoll || _api is null) return;
        _nextPoll = Time.time + PollIntervalSeconds;
        try
        {
            var dto = await _api.GetHealthStatusAsync();
            Debug.Log($"[Health] {(dto?.status ?? "unknown")} @ {DateTime.UtcNow:O}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Health poll failed: {ex.Message}");
        }
    }
}
#endif
