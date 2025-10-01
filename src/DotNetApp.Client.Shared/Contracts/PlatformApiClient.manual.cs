using System.Net.Http;
using DotNetApp.Client.Shared.Contracts;

namespace DotNetApp.Client.Shared.Contracts;

// Manual partial to allow compilation before source generator runs; methods generated in *.g.cs
public sealed partial class PlatformApiClient : IPlatformApi { }
