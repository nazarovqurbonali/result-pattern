// Polyfills for compiler-recognized types that ship with net6.0+ but are absent on netstandard2.0.
// Defining them ourselves lets the same source compile against the older target.
// Gated to NETSTANDARD2_0 so the real BCL types are used on modern targets.
#if NETSTANDARD2_0

namespace System.Runtime.CompilerServices
{
    /// <summary>Enables <c>init</c>-only setters and records on netstandard2.0.</summary>
    internal static class IsExternalInit;

    /// <summary>Polyfill for <c>CallerArgumentExpressionAttribute</c>.</summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
    {
        public string ParameterName { get; } = parameterName;
    }
}

namespace System.Diagnostics
{
    /// <summary>Polyfill for <c>StackTraceHiddenAttribute</c> (no runtime effect on netstandard2.0).</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Struct)]
    internal sealed class StackTraceHiddenAttribute : Attribute;
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Polyfill for <c>NotNullAttribute</c> — marks a checked argument as non-null on return.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    internal sealed class NotNullAttribute : Attribute;
}

#endif
