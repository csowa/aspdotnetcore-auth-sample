using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.WsFederation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Added https://learn.microsoft.com/en-us/aspnet/core/security/authentication/ws-federation?view=aspnetcore-8.0#use-ws-federation-without-aspnet-core-identity
builder.Services.AddAuthentication(sharedOptions =>
    {
        sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        sharedOptions.DefaultChallengeScheme = WsFederationDefaults.AuthenticationScheme;
    })
    .AddWsFederation(options =>
    {
        options.Wtrealm = builder.Configuration["wsfed:realm"];
        options.MetadataAddress = builder.Configuration["wsfed:metadata"];

        // Results in error regardless of setting.
        // See https://github.com/dotnet/aspnetcore/issues/49469
        options.UseSecurityTokenHandlers = true;

        // true:
        // SecurityTokenInvalidIssuerException: IDX10204: Unable to validate issuer. validationParameters.ValidIssuer is null or whitespace AND validationParameters.ValidIssuers is null or empty.
        // Expected: behavior prior to change introduced with issue 49469.

        // false: 
        // XmlReadException: IDX30011: Unable to read XML. Expecting XmlReader to be at ns.element: 'urn:oasis:names:tc:SAML:2.0:assertion.Assertion', found: 'urn:oasis:names:tc:SAML:1.0:assertion.Assertion'.
        // Expected: to be able to handle SAML 1.0 assertion emitted by WsFed server.

        // PS C:\Windows\system32> (Get-Item C:\Windows\ADFS\Microsoft.IdentityServer.ServiceHost.exe).VersionInfo.ProductVersion 
        // 10.0.17763.4644

        // Initialize config to "resolve" issue:
        SetupTokenValidationParametersAsync(options);

    })
    .AddCookie();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

/// <summary>
/// Add config manager and valid issuers from config.
/// *** NOT production code. ***
/// </summary>
/// <remarks>
/// See https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/2406;
/// See <see cref="Microsoft.AspNetCore.Authentication.WsFederation.WsFederationHandler.SetupTokenValidationParametersAsync()"/> 
/// Verifying missing configuration / initialization as a possible cause.
/// </remarks>
static async void SetupTokenValidationParametersAsync(WsFederationOptions options)
{
    // Operate on params themselves for this (one-time) initialization.
    var tokenValidationParameters = options.TokenValidationParameters;

    // ConfigurationManager not present at point this is called, so add here, *** with metadata configuration set in caller ***.
    options.ConfigurationManager = new ConfigurationManager<WsFederationConfiguration>(options.MetadataAddress, new WsFederationConfigurationRetriever());

    var _configuration = await options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);

    var issuers = new[] { _configuration.Issuer };

    tokenValidationParameters.ValidIssuers = tokenValidationParameters.ValidIssuers == null ? issuers : tokenValidationParameters.ValidIssuers.Concat(issuers);
    tokenValidationParameters.IssuerSigningKeys = tokenValidationParameters.IssuerSigningKeys == null ? _configuration.SigningKeys : tokenValidationParameters.IssuerSigningKeys.Concat(_configuration.SigningKeys);
}