export interface TenantSettingsDto {
  id:                 string;
  tenantId:           string;
  displayName:        string;
  defaultPalette:     string;
  timezone:           string;
  customCssTokensJson: string | null;
}

export interface UpdateTenantSettingsRequest {
  displayName:    string;
  defaultPalette: string;
  timezone:       string;
  customCss?:     string | null;
}

export interface SanitisedCssResult {
  acceptedTokens:  Record<string, string>;
  rejectedEntries: string[];
}
