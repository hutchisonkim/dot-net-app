using System;

namespace DotNetApp.CodeGen;

[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class ApiContractAttribute : Attribute
{
    public string BasePath { get; }
    public ApiContractAttribute(string basePath = "") => BasePath = basePath?.Trim('/') ?? string.Empty;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class GetAttribute : Attribute { public string Path { get; } public GetAttribute(string path) => Path = path; }

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class PostAttribute : Attribute { public string Path { get; } public PostAttribute(string path) => Path = path; }

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class PutAttribute : Attribute { public string Path { get; } public PutAttribute(string path) => Path = path; }

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class DeleteAttribute : Attribute { public string Path { get; } public DeleteAttribute(string path) => Path = path; }

[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class BodyAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class RetryAttribute : Attribute
{
    public int Attempts { get; }
    public int DelayMs { get; }
    public RetryAttribute(int attempts = 3, int delayMs = 200) { Attempts = attempts; DelayMs = delayMs; }
}
