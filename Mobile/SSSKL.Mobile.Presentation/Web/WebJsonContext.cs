using System.Text.Json.Serialization;

namespace SSSKL.Mobile.Presentation.Web;

/// <summary>
/// Source-generated (reflection-free, Native AOT) JSON for the WebView scripts: quoting strings as JavaScript
/// literals and reading the values the page returns. Presentation's own, so it doesn't depend on Infrastructure.
/// </summary>
[JsonSerializable(typeof(string))]
internal sealed partial class WebJsonContext : JsonSerializerContext;
