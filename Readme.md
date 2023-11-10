# aspdotnetcore-auth-sample

2023.11.09

Demonstrates WS-Federation issue with change introduced for https://github.com/dotnet/aspnetcore/issues/49469

Based on example at https://learn.microsoft.com/en-us/aspnet/core/security/authentication/ws-federation?view=aspnetcore-8.0#use-ws-federation-without-aspnet-core-identity

Build and run.  Home page uses `[Authorize]` attribute, authentication begins when loading.

Dependency: ADFS server required. Server version tested: 10.0.17763.4644

To reproduce, need to be requesting a new token, cached token will bypass the error.

Results in error regardless of new `UseSecurityTokenHandlers` setting:

```cs
options.UseSecurityTokenHandlers = true;
```

> SecurityTokenInvalidIssuerException: IDX10204: Unable to validate issuer. validationParameters.ValidIssuer is null or whitespace AND validationParameters.ValidIssuers is null or empty.

Expected: behavior prior to change introduced with issue 49469.

```cs
options.UseSecurityTokenHandlers = false;
```

> XmlReadException: IDX30011: Unable to read XML. Expecting XmlReader to be at ns.element: 'urn:oasis:names:tc:SAML:2.0:assertion.Assertion', found: 'urn:oasis:names:tc:SAML:1.0:assertion.Assertion'.

Expected: to be able to handle SAML 1.0 assertion emitted by WsFed server.
