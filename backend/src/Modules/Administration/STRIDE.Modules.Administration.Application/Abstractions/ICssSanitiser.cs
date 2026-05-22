using STRIDE.Modules.Administration.Application.DTOs;

namespace STRIDE.Modules.Administration.Application.Abstractions;

/// <summary>
/// Deny-by-default CSS sanitiser.  Extracts only <c>--stride-*</c> custom properties
/// assigned inside <c>:root {}</c> and rejects everything else.
/// </summary>
public interface ICssSanitiser
{
    SanitisedCssResult Sanitise(string rawCss);
}
