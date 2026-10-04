# Developer Guide: Agent Api Services in MerchantSDK

Welcome to the **Agent Api Services** integration guide. This SDK utilizes a unified command-based architecture to streamline financial operations like Deposits, Withdrawals, and Transfers ..etc.

## 1. Core Concept

The SDK is built on a **Command/Request architecture**:
*   **Commands:** Represent the specific action you want to perform (e.g., `SendRemittanceWithLoginCommand`).
*   **ISender:** The central engine. You simply pass your command to the `ISender` interface, and it handles routing, headers, and execution automatically.

---

## 2. Getting Started

### 1. Installation

You can install the **Tharwat.Purchases.Merchant.SDK** via NuGet Package Manager.

### Package Manager Console
If you are using the Visual Studio Package Manager Console, run:
```powershell
Install-Package Tharwat.Purchases.Merchant.SDK
```

### 2. Configuration
Add the following section to your `appsettings.json`. These URIs define the endpoints for the gateway.

```json
  "UnifiedUriSettings": {
    "TransactionRequestsUri": "/v1/transactionrequests",
    "TransactionsUri": "/v1/transactions",
    "LoginUri": "/accounts/v1/authenticate/token",
    "BaseUrl": "https://api-dev.tharwatt.com:51000/gateway",
    "ConnectionsUri": "/v1/accounts",
    "ProfilesUri": "/accounts/v1/profiles",
    "AccountLinksUri": "/accounts/accountlinks/initiate",
    "ExternalAccountUri": "/accounts/externalaccounts/initiate",
    "BatchProfilesUri": "/accounts/v1/batchprofiles"
  },
```

### 3. Registering the SDK
In your `Program.cs`, register the configuration using the Options pattern and initialize the SDK services.

```csharp

var builder = WebApplication.CreateBuilder(args);

// 1. Bind the settings section
var settings = builder.Configuration.GetSection("UnifiedUriSettings");
builder.Services.AddOptions<UnifiedUriSettings>().Bind(settings);

// 2. Register Unified link services
builder.Services.AddLinkServices();
```


## 3.1 Authentication Services

### Description
**Login:** Before interacting with the financial services, an application must authenticate itself to obtain an access token. This token is used to authorize subsequent API calls. The system uses the OAuth2 `client_credentials` flow.

### 1.1 Login Command
| Property | Type | Description |
| :--- | :--- | :--- |
| `Grant_type` | `string` | The type of authentication flow (usually "client_credentials"). |
| `Client_id` | `string` | The unique identifier for your application. |
| `Client_secret` | `string` | The secret key assigned to your application. |

### 3.1.1 Example: Authentication Execution

```csharp
public class LoginExample 
{
    private readonly ILoginService _loginService;

    public LoginExample(ILoginService loginService)
    {
        _loginService = loginService;
    }

    public async Task RunLoginAsync(CancellationToken cancellationToken = default)
    {
        // 1. Prepare the Request
        LoginRequest request = new()
        {
            Grant_type = "client_credentials",
            Client_id = "dev90-app",
            Client_secret = "123456"
        };

        // 2. Execute Login
        // The result is returned inside a ServiceResult wrapper
        var result = await _loginService.LoginAsync(request);

        // 3. Handle the Response
        if (result.IsSuccess)
        {
            var response = result.Entity;
            Console.WriteLine($"Access Token: {response.Access_token}");
            Console.WriteLine($"Expires In: {response.Expires_in} seconds");
        }
        else
        {
            Console.WriteLine($"Login Failed: {result.Message}");
        }
    }
}
```

---

## 3.1.2 Response Models

When the login call is successful, the `result.Entity` contains the following structure:

### LoginResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Access_token` | `string` | The JWT or Bearer token used for authorized requests. |
| `Expires_in` | `int` | The duration (in seconds) for which the token remains valid. |
| `Token_type` | `string` | The type of token issued (e.g., "Bearer"). |
| `Scope` | `string` | The permissions granted to this token. |
| `Error` | `string` | Contains error details if the authentication failed. |

> **Note:** All responses are wrapped in a `ServiceResult<T>` object, which includes metadata such as success status, error messages, and the response code.

---

### Integration Hint
In your **Deposit Services (4.1.1)**, the `LoginRequest` object can be embedded directly into the `SendRemittanceWithLoginCommand` to perform authentication and the deposit in a single logical step (In SDK), or you can use the standalone Login service shown above to manage sessions independently and call the `SendRemittanceWithTokenCommand` and pass for it the `AccessToken` instead of `LoginRequest`.



---
## Shared Request Objects (Sub-Models)

### LinkSourceRequest
| Property | Type | Description |
| :--- | :--- | :--- |
| `OrganizationCode` | `string` | Code of the bank/wallet (e.g., "Link"). |
| `AccountType` | `AccountType` | Enum: ProfileId, AccountId, etc. |
| `AccountId` | `string` | The actual account identifier/number. |

### LinkCustomerKYC
| Property | Type | Description |
| :--- | :--- | :--- |
| `FirstName` | `string` | Person's first name. |
| `SecondName` | `string` | Middle name. |
| `ThirdName` | `string` | Third/Grandfather name. |
| `FamilyName` | `string` | Last/Family name. |
| `MobileNumber` | `string` | Phone number (used for OTPs). |

### Terminal
| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `int` | Terminal unique identifier. |
| `TerminalAlias` | `string` | Short name or alias for the terminal. |
| `Country` | `string` | Country where the terminal is located. |
| `City` | `string` | City where the terminal is located. |
| `Region` | `string` | Specific region or district. |
| `Channel` | `int` | Communication channel ID (e.g., 1 for Mobile). |
| `TerminalName` | `string` | Full display name of the terminal. |

### LoginRequest
| Property | Type | Description |
| :--- | :--- | :--- |
| `Grant_type` | `string` | Auth type ("client_credentials"). |
| `Client_id` | `string` | Your Application ID. |
| `Client_secret` | `string` | Your Application Secret. |

### 1.1 Send Remittance
*   **Send Remittance:** This service enables the sending of money transfers (remittances) to be collected or deposited through a provider (e.g., "Swaid"). It requires comprehensive KYC information for both the sender and the receiver, a specified expiration date for the transfer, and specific remittance metadata such as the purpose and target of the transfer.

### 1.1 SendRemittanceWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the request (GUID). |
| `ReferenceNumber` | `string` | External reference number for tracking the remittance. |
| `CompanyCode` | `string` | The code of the remittance provider (e.g., "swaid"). |
| `CaptureMode` | `string` | Settlement mode (e.g., "AUTO" or "MANUAL"). |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The total remittance amount. |
| `AmountType` | `int` | The identifier for the type of amount. |
| `IsBeneficiaryInitiated` | `bool` | Indicates if the receiver started the transaction request. |
| `Notes` | `string` | Optional descriptive notes or memo. |
| `ExpireDate` | `string` | The ISO timestamp when the remittance expires if not claimed. |
| `SenderKYC` | `LinkCustomerKYC` | Identity details of the person sending the money. |
| `ReceiverKYC` | `LinkCustomerKYC` | Identity details of the person intended to receive the money. |
| `Terminal` | `Terminal` | Details of the terminal used to initiate the remittance. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service. |
| `MetaData` | `List<LinkMetaData>` | **Required:** Purpose of transfer, Target, and charge details. |

#### 1.1 Example: Send Remittance Execution

```csharp
public class RemittanceExample
{
    private readonly ISender _sender;

    public RemittanceExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunSendRemittanceAsync(CancellationToken ct = default)
    {
        var command = new SendRemittanceWithLoginCommand(
            RequestId: "1be56d2-ca65-4ffe-9f24-9a3a1f4053f5",
            ReferenceNumber: "98745632269",
            CompanyCode: "swaid",
            CaptureMode: "AUTO",
            CurrencyCode: "YER",
            Amount: 2000.0m,
            AmountType: 2,
            IsBeneficiaryInitiated: false,
            Notes: "Family Support",
            ExpireDate: "2026-04-27T00:46:35.328Z",
            SenderKYC: new LinkCustomerKYC
            {
                FirstName = "Mohammed",
                SecondName = "Talaat",
                FamilyName = "Hawash",
                MobileNumber = "772524472",
                IdType = "ID-1"
            },
            ReceiverKYC: new LinkCustomerKYC
            {
                FirstName = "Ahmed",
                SecondName = "Mohammed",
                FamilyName = "Al-Jaafari",
                MobileNumber = "775819841",
                IdType = "ID-1"
            },
            Terminal: new Terminal { Channel = 1, TerminalName = "Branch-01" },
            LoginRequest: new LoginRequest { /* app credentials */ },
            MetaData: new List<LinkMetaData>
            {
                new() { Key = "Purpose", Value = "P-1" }, // Personal
                new() { Key = "Target", Value = "Ta-1" },  // Family
                new() { Key = "ChargesAmount", Value = "0" },
                new() { Key = "CommisionInculded", Value = "0" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Remittance Sent. ID: {response.TransactionId}");
            Console.WriteLine($"Unified Token: {response.UnifiedToken}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 1.1 Response Model: SendRemittanceResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `UnifiedToken` | `string` | The primary secure token for the remittance (e.g., used for claiming). |
| `ResourceToken` | `string` | Secondary resource identifier token. |
| `RequestId` | `string` | The unique request ID echoed back. |
| `Fees` | `decimal` | Service fees applied to the remittance. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Commission amount earned on the transaction. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `Balance` | `decimal` | The remaining account balance after the remittance. |
| `TransactionStatus` | `int` | Numerical status of the transaction. |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date funds are scheduled for settlement. |
| `TransactionDate` | `string` | The actual date and time the transfer was created. |
| `ProviderReference` | `string` | The reference identifier from the remittance provider (e.g. Swaid ID). |

---
### 2.1 Find Remittance
*   **Find Remittance:** This service is used to search for and verify an existing remittance that has been sent but not yet claimed. By providing the `TokenNumber` (the unique remittance identifier) and the receiver's KYC details, the system validates the transaction's existence, returns the sender's information, and provides the tokens required to proceed with receiving or settling the funds.

### 2.1 FindRemittanceWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | The external reference number associated with the search request. |
| `TokenNumber` | `string` | The unique Remittance ID or Token provided by the sender. |
| `CaptureMode` | `string` | Settlement mode (e.g., "AUTO" or "MANUAL"). |
| `CompanyCode` | `string` | The code of the remittance provider (e.g., "swaid"). |
| `CurrencyCode` | `string` | The ISO currency code of the remittance (e.g., "YER"). |
| `Amount` | `decimal` | The total amount of the remittance to be found. |
| `ReceiverKYC` | `LinkCustomerKYC` | Identity details of the person claiming the remittance. |
| `Terminal` | `Terminal` | Details of the terminal used to perform the search. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service. |
| `MetaData` | `List<LinkMetaData>` | Optional custom key-value pairs for the search. |

#### 2.1 Example: Find Remittance Execution

```csharp
public class FindRemittanceExample
{
    private readonly ISender _sender;

    public FindRemittanceExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunFindRemittanceAsync(CancellationToken ct = default)
    {
        var command = new FindRemittanceWithLoginCommand(
            ReferenceNumber: "65432165488",
            TokenNumber: "30993700307", // The Remittance ID
            CaptureMode: "AUTO",
            CompanyCode: "swaid",
            CurrencyCode: "YER",
            Amount: 2000.0m,
            ReceiverKYC: new LinkCustomerKYC
            {
                FirstName = "Ahmed",
                SecondName = "Mohammed",
                ThirdName = "Abdullah",
                FamilyName = "Al-Jaafari",
                MobileNumber = "775819841",
                IdType = "ID-1"
            },
            Terminal: new Terminal { Channel = 1, TerminalName = "Branch-05" },
            LoginRequest: new LoginRequest { /* credentials */ },
            MetaData: new List<LinkMetaData> { new() { Key = "key", Value = "value" } }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Remittance Found.");
            Console.WriteLine($"Sender: {response.SenderKYC.FirstName} {response.SenderKYC.FamilyName}");
            Console.WriteLine($"Remittance Date: {response.RemittanceDate}");
            Console.WriteLine($"Unified Token: {response.UnifiedToken}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 2.1 Response Model: FindRemittanceResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `UnifiedToken` | `string` | The secure session token required for receiving the remittance. |
| `ResourceToken` | `string` | Secondary resource identifier token. |
| `OneTimeCode` | `string` | A verification code (OTP) if required for the pickup process. |
| `RequestId` | `string` | The unique request ID associated with this search. |
| `Amount` | `decimal` | The actual amount found in the remittance record. |
| `CurrencyCode` | `string` | The currency code of the found remittance. |
| `Fees` | `decimal` | Any fees associated with receiving the remittance. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Commission amount associated with the transaction. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `SenderKYC` | `LinkCustomerKYC` | Comprehensive identity details of the original sender. |
| `ReceiverKYC` | `LinkCustomerKYC` | Echoed identity details of the intended receiver. |
| `RemittanceDate` | `string` | The date and time when the remittance was originally sent. |

---

### 3.1 Receive Remittance
*   **Receive Remittance:** This service is the final execution step in the remittance lifecycle. After finding the remittance in the previous step, this service uses the `OneTimeCode` (obtained from the Find response) and the `TokenNumber` to officially payout or settle the funds to the receiver. 

### 3.1 ReceiveRemittanceWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | External reference number for the receipt transaction. |
| `TokenNumber` | `string` | The unique Remittance ID or Token being claimed. |
| `CaptureMode` | `string` | Settlement mode (e.g., "AUTO" or "MANUAL"). |
| `CompanyCode` | `string` | The code of the remittance provider (e.g., "swaid"). |
| `CurrencyCode` | `string` | The ISO currency code of the remittance (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be received. |
| `OneTimeCode` | `string` | The secure OTP or verification token obtained from the **Find Remittance** response. |
| `ReceiverKYC` | `LinkCustomerKYC` | Identity details of the person receiving the funds. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service. |
| `MetaData` | `List<LinkMetaData>` | Optional custom key-value pairs. |

#### 3.1 Example: Receive Remittance Execution

```csharp
public class ReceiveRemittanceExample
{
    private readonly ISender _sender;

    public ReceiveRemittanceExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunReceiveRemittanceAsync(CancellationToken ct = default)
    {
        var command = new ReceiveRemittanceWithLoginCommand(
            ReferenceNumber: "65465498808",
            TokenNumber: "30993700307",
            CaptureMode: "AUTO",
            CompanyCode: "swaid",
            CurrencyCode: "YER",
            Amount: 2000m,
            OneTimeCode: "ceb33686-a0cb-4928-a823-bc3be609dc9d", // From Find response
            ReceiverKYC: new LinkCustomerKYC
            {
                FirstName = "Ahmed",
                SecondName = "Mohammed",
                ThirdName = "Abdullah",
                FamilyName = "Al-Jaafari",
                MobileNumber = "775819841",
                IdType = "ID-1"
            },
            LoginRequest: new LoginRequest { /* credentials */ },
            MetaData: new List<LinkMetaData> { new() { Key = "key", Value = "value" } }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Remittance Received Successfully.");
            Console.WriteLine($"Transaction ID: {response.TransactionId}");
            Console.WriteLine($"Provider Reference: {response.ProviderReference}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 3.1 Response Model: ReceiveRemittanceResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `Fees` | `decimal` | Service fees applied to receiving the remittance. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Commission amount associated with the payout. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `Balance` | `decimal` | The updated account balance after receiving the funds. |
| `TransactionStatus` | `int` | Numerical status of the transaction. |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date the funds are scheduled for settlement. |
| `TransactionDate` | `string` | The actual date and time the receipt was finalized. |
| `ProviderReference` | `string` | The reference identifier from the external remittance provider. |

---

### 4.1 Get Commissions
*   **Get Commissions:** This query service is used to pre-calculate the costs associated with a transaction before execution. It provides a detailed breakdown of charges, service fees, and commissions based on the provider, amount, currency, and specific service code. This is essential for displaying accurate totals to users before they authorize a payment or remittance.

### 4.1 GetCommissionsWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The code of the service provider (e.g., "Easy"). |
| `PayoutCurrencyCode` | `string` | The ISO currency code for the payout (e.g., "YER"). |
| `PayoutAmount` | `decimal` | The base amount for which commissions are being calculated. |
| `Target` | `string` | The transaction target identifier (e.g., "Ta-1"). |
| `ServiceCode` | `string` | The unique identifier for the specific service (e.g., "821"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 4.1 Example: Get Commissions Execution

```csharp
public class CommissionWithLoginQueryExample
{
    private readonly ISender _sender;

    public CommissionWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetCommissionsAsync(CancellationToken ct = default)
    {
        var query = new GetCommissionsWithLoginQuery(
            CompanyCode: "Easy",
            PayoutCurrencyCode: "YER",
            PayoutAmount: 10000m,
            Target: "Ta-1",
            ServiceCode: "821",
            LoginRequest: new LoginRequest { /* credentials */ }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Commission Calculation:");
            Console.WriteLine($"Fees: {response.Fees} {response.FeesCurrencyCode}");
            Console.WriteLine($"Commission: {response.Commission} {response.CommissionCurrencyCode}");
            Console.WriteLine($"Total Charges: {response.ChargesAmount}");
            
            // Detailed breakdown
            Console.WriteLine($"Switch Fees: {response.Details.SwitchFees}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 4.1 Response Model: GetCommissionsResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ChargesAmount` | `decimal` | The total additional charges applied to the transaction. |
| `ChargesCurrencyCode`| `string` | Currency of the charges. |
| `Fees` | `decimal` | Total service fees. |
| `FeesCurrencyCode` | `string` | Currency of the service fees. |
| `Commission` | `decimal` | Total commission calculated. |
| `CommissionCurrencyCode`| `string` | Currency of the commission. |
| `Details` | `CommissionDetails` | A granular breakdown of where the fees are distributed. |

#### 4.1 Sub-Model: CommissionDetails
| Property | Type | Description |
| :--- | :--- | :--- |
| `SourceFees` | `decimal` | Fees charged at the origin of the transaction. |
| `SwitchFees` | `decimal` | Fees charged by the internal transaction switcher. |
| `GwFees` | `decimal` | Fees charged by the payment gateway. |
| `BeneficiaryFees` | `decimal` | Fees associated with the payout to the beneficiary. |

---

### 5.1 Get Companies
*   **Get Companies:** This query service retrieves a list of all active companies and providers registered in the system. It can also be used to fetch the details of a specific provider by passing an optional `CompanyCode`. This information is typically used to populate provider selection lists for remittances, bill payments, or wallet services.

### 5.1 GetCompaniesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string?` | Optional. The unique code of a specific company to retrieve. If null, all companies are returned. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 5.1 Example: Get Companies Execution

```csharp
public class CompanyWithLoginQueryExample
{
    private readonly ISender _sender;

    public CompanyWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetCompaniesAsync(CancellationToken ct = default)
    {
        // WithLoginQuerying all available companies
        var query = new GetCompaniesWithLoginQuery(
            LoginRequest: new LoginRequest { /* credentials */ },
            CompanyCode: null 
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            var companies = result.Entities;
            Console.WriteLine("[SUCCESS] Companies retrieved.");
            
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 5.1 Response Model: List<GetCompaniesResponse>
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique system identifier for the company (e.g., "Easy", "Swaid"). |
| `Name` | `string` | The localized name of the company. |
| `NameEn` | `string` | The English name of the company. |
| `IsActive` | `bool` | Indicates if the company is currently operational and accepting transactions. |
| `ServiceType` | `int` | Numerical category for the type of service provided. |
| `SectorType` | `int` | Numerical category for the business sector (e.g., Finance, Telecom). |
| `BusinessType` | `int` | Numerical category for the specific business model. |

---

### 6.1 Get Currencies
*   **Get Currencies:** This query service retrieves the list of supported and active currencies for a specific company or provider. It is used to ensure that transactions (such as remittances or bill payments) are initiated using a currency that the selected provider can process.

### 6.1 GetCurrenciesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for supported currencies (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 6.1 Example: Get Currencies Execution

```csharp
public class CurrencyWithLoginQueryExample
{
    private readonly ISender _sender;

    public CurrencyWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetCurrenciesAsync(CancellationToken ct = default)
    {
        var query = new GetCurrenciesWithLoginQuery(
            CompanyCode: "EASY", 
            LoginRequest: new LoginRequest { /* credentials */ }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            var currencies = result.Entity;
            Console.WriteLine("[SUCCESS] Currencies retrieved.");
            
            // Example output: YER, USD, etc.
            // Console.WriteLine($"Available Currency: {currencies.Code}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 6.1 Response Model: GetCurrenciesResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The ISO currency code (e.g., "YER", "USD", "SAR"). |
| `IsActive` | `bool` | Indicates if this currency is currently enabled for the specified provider. |

---

### 7.1 Get Regions
*   **Get Regions:** This query service retrieves a list of supported geographical or administrative regions associated with a specific company or provider. These region codes are essential for services like customer registration, KYC verification, and identifying specific service areas for remittances.

### 7.1 GetRegionsWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for supported regions (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 7.1 Example: Get Regions Execution

```csharp
public class RegionWithLoginQueryExample
{
    private readonly ISender _sender;

    public RegionWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetRegionsAsync(CancellationToken ct = default)
    {
        var query = new GetRegionsWithLoginQuery(
            CompanyCode: "EASY", 
            LoginRequest: new LoginRequest { /* credentials */ }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            // Note: Use result.Entities if the response returns a list
            var regions = result.Entity; 
            Console.WriteLine("[SUCCESS] Regions retrieved.");
            
            // Example output: YEM-SN-R10, YEM-BA-R6, etc.
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 7.1 Response Model: GetRegionsResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique system identifier for the region (e.g., "YEM-BA-R6"). |
| `IsActive` | `bool` | Indicates if this region is currently enabled and valid for use in the system. |

---

### 8.1 Get Provinces
*   **Get Provinces:** This query service retrieves a list of provinces (or governorates) associated with a specific company or provider. This is a higher-level administrative division than regions and is used to provide structured address selection for customer registration and identity verification.

### 8.1 GetProvincesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for supported provinces (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 8.1 Example: Get Provinces Execution

```csharp
public class ProvinceWithLoginQueryExample
{
    private readonly ISender _sender;

    public ProvinceWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetProvincesAsync(CancellationToken ct = default)
    {
        var query = new GetProvincesWithLoginQuery(
            CompanyCode: "EASY", 
            LoginRequest: new LoginRequest { /* credentials */ }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            // Note: Provinces are typically returned as a list (result.Entities)
            var provinces = result.Entities; 
            Console.WriteLine("[SUCCESS] Provinces retrieved.");
            
            foreach(var province in provinces)
            {
                Console.WriteLine($"Province: {province.NameEn} ({province.Code})");
            }
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 8.1 Response Model: GetProvincesResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique system identifier for the province. |
| `Name` | `string` | The localized name of the province. |
| `NameEn` | `string` | The English name of the province. |
| `IsActive` | `bool` | Indicates if this province is currently enabled for the specified provider. |

---

### 9.1 Get Countries
*   **Get Countries:** This query service retrieves a list of supported and active countries associated with a specific company or provider. This is primarily used for cross-border services, international remittances, or determining the nationality options available for customer onboarding.

### 9.1 GetCountriesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for supported countries (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 9.1 Example: Get Countries Execution

```csharp
public class CountryWithLoginQueryExample
{
    private readonly ISender _sender;

    public CountryWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetCountriesAsync(CancellationToken ct = default)
    {
        var query = new GetCountriesWithLoginQuery(
            CompanyCode: "EASY", 
            LoginRequest: new LoginRequest 
            { 
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            // Countries are typically returned as a list
            var countries = result.Entities; 
            Console.WriteLine("[SUCCESS] Countries retrieved.");
            
            foreach(var country in countries)
            {
                Console.WriteLine($"Country Code: {country.Code}, Active: {country.IsActive}");
            }
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 9.1 Response Model: GetCountriesResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The ISO country code (e.g., "YE", "US", "SA"). |
| `IsActive` | `bool` | Indicates if this country is currently enabled for the specified provider. |

---

### 10.1 Get Services
*   **Get Services:** This query service retrieves a list of available business services provided by a specific company or provider. Each service is identified by a unique code, and the response includes the provider's friendly name and the operational status of the service. This is commonly used to dynamically render available transaction types (like specific billers or payment products) in a user interface.

### 10.1 GetServicesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for available services (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 10.1 Example: Get Services Execution

```csharp
public class ServiceWithLoginQueryExample
{
    private readonly ISender _sender;

    public ServiceWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetServicesAsync(CancellationToken ct = default)
    {
        var query = new GetServicesWithLoginQuery(
            CompanyCode: "EASY", 
            LoginRequest: new LoginRequest 
            { 
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            // Services are typically returned as a list (result.Entities)
            var services = result.Entities; 
            Console.WriteLine("[SUCCESS] Services retrieved.");
            
            foreach(var service in services)
            {
                Console.WriteLine($"Service: {service.ProvName} (Code: {service.Code}), Active: {service.IsActive}");
            }
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 10.1 Response Model: GetServicesResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique system identifier for the specific service (e.g., "821"). |
| `IsActive` | `bool` | Indicates if this specific service is currently enabled for transactions. |
| `ProvName` | `string` | The display name of the provider or the specific service category. |

---
### 11.1 Get Targets
*   **Get Targets:** This query service retrieves a list of destination targets, branches, or collection points associated with a specific company code. This is essential for identifying where a transaction can be routed or picked up. Each target includes localized names (both primary and English) and an operational status to ensure the selected target is currently active.

### 11.1 GetTargetsWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company/provider whose targets you wish to retrieve (e.g., "hitar"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 11.1 Example: Get Targets Execution

```csharp
public class TargetWithLoginQueryExample
{
    private readonly ISender _sender;

    public TargetWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetTargetsAsync(CancellationToken ct = default)
    {
        var query = new GetTargetsWithLoginQuery(
            CompanyCode: "hitar", 
            LoginRequest: new LoginRequest 
            { 
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            // Targets are returned as a collection (result.Entity or result.Entities)
            Console.WriteLine("[SUCCESS] Targets retrieved.");
            Console.WriteLine("Response Data: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
            Console.WriteLine("Error Details: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
```

#### 11.1 Response Model: GetTargetsResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique identifier or short-code for the target/branch. |
| `Name` | `string` | The localized name of the target (e.g., in Arabic). |
| `NameEn` | `string` | The English translation of the target's name. |
| `IsActive` | `bool` | Indicates whether the target is currently operational and accepting transactions. |

---

### 12.1 Get Purposes
*   **Get Purposes:** This query service retrieves a list of valid transaction purposes (remittance purposes) associated with a specific company or provider. Transaction purposes are often required for regulatory compliance and Anti-Money Laundering (AML) reporting, defining the reason for the transfer (e.g., "Family Support" or "Business Investment"). The response provides localized names and status flags to ensure only active purposes are presented to the end user.

### 12.1 GetPurposesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for available transaction purposes (e.g., "hitar"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 12.1 Example: Get Purposes Execution

```csharp
public class PurposeWithLoginQueryExample
{
    private readonly ISender _sender;

    public PurposeWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetPurposesAsync(CancellationToken ct = default)
    {
        var query = new GetPurposesWithLoginQuery(
            CompanyCode: "hitar", 
            LoginRequest: new LoginRequest 
            { 
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            Console.WriteLine("[SUCCESS] Transaction purposes retrieved.");
            // Response data is serialized for logging/debugging
            Console.WriteLine("Response Data: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
            Console.WriteLine("Error Details: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
```

#### 12.1 Response Model: GetPurposesResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique identifier or regulatory code for the purpose (e.g., "01"). |
| `Name` | `string` | The primary localized name of the purpose (e.g., in Arabic). |
| `NameEn` | `string` | The English translation of the purpose name. |
| `IsActive` | `bool` | Indicates if this purpose is currently valid for use in transactions. |

### 13.1 Get Transaction
*   **Get Transaction:** This query service retrieves the full details of a specific transaction using its reference number. It is primarily used for status reconciliation, verifying transaction success, and retrieving financial breakdown details such as fees, commissions, and the system-generated public number (MTCN). This service ensures that the integrator can verify the state of a transaction at any point after its creation.

### 13.1 GetTransactionWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | The unique reference number assigned to the transaction at the time of creation. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |
| `RequestDate` | `DateTime?` | (Optional) The specific date the transaction was requested, used to optimize search performance. |

#### 13.1 Example: Get Transaction Execution

```csharp
public class TransactionWithLoginQueryExample
{
    private readonly ISender _sender;

    public TransactionWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetTransactionAsync(CancellationToken ct = default)
    {
        var query = new GetTransactionWithLoginQuery(
            ReferenceNumber: "98745632480", 
            LoginRequest: new LoginRequest 
            { 
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            Console.WriteLine("[SUCCESS] Transaction details retrieved.");
            // Comprehensive transaction data including status and financial breakdown
            Console.WriteLine("Response Data: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
            Console.WriteLine("Error Details: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
```

#### 13.1 Response Model: GetTransactionResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique UUID associated with the original API request. |
| `TransactionTime` | `string` | The timestamp indicating when the transaction was finalized in the system. |
| `Amount` | `decimal` | The principal amount of the transaction. |
| `Currency` | `string` | The ISO currency code for the transaction amount (e.g., "YER"). |
| `ServiceId` | `string` | The identifier of the specific service/product used. |
| `SourceReference` | `string` | The original reference provided by the client during the initiation phase. |
| `RequestTime` | `string` | The timestamp indicating when the initial request was received. |
| `Details` | `string` | A descriptive status message or summary of the transaction processing. |
| `ClientId` | `string` | The identifier of the application or client that initiated the request. |
| `Sender` | `string` | Information or identifier (e.g., phone number) of the person sending the funds. |
| `Receiver` | `string` | Information or identifier (e.g., phone number) of the person receiving the funds. |
| `Fees` | `decimal` | The total service fees charged to the customer. |
| `FeesCurrency` | `string` | The currency in which the fees were calculated. |
| `Commission` | `decimal` | The commission amount calculated for this transaction. |
| `CommissionCurrency` | `string` | The currency in which the commission was calculated. |
| `PublicNumber` | `string` | The consumer-facing transaction number (often used as the MTCN or pick-up code). |
| `TransactionCommand` | `int` | An integer code representing the current state or type of the transaction. |
| `TransactionId` | `string` | The internal system-wide unique identifier for the transaction record. |

---
### 13.2 Get Transactions (History)
*   **Get Transactions:** This query service retrieves a paginated list of transaction records based on specific filter criteria. It allows developers to fetch transaction history using date ranges (`StartDate` and `EndDate`), filter by transaction status (e.g., "success"), and manage large datasets using `Take` and `Skip` parameters. This is ideal for building transaction history screens or performing end-of-day reconciliations.

### 13.2 GetTransactionsWithLoginWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |
| `Take` | `int` | The number of records to retrieve (Default: 10). |
| `Skip` | `int` | The number of records to bypass for pagination (Default: 0). |
| `StartDate` | `DateTime?` | The beginning of the date range for the search. |
| `EndDate` | `DateTime?` | The end of the date range for the search. |
| `Status` | `string` | Filter transactions by status (e.g., "success"). |

#### 13.2 Example: Get Transactions Execution

```csharp
public class TransactionsHistoryExample
{
    private readonly ISender _sender;

    public TransactionsHistoryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetTransactionsAsync(CancellationToken ct = default)
    {
        // Fetching the last 10 successful transactions
        var query = new GetTransactionsWithLoginWithLoginQuery(
            LoginRequest: new LoginRequest 
            { 
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            Take: 10,
            Skip: 0,
            Status: "success"
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            Console.WriteLine("[SUCCESS] Transaction history retrieved.");
            // Data is usually returned as a list within the result entity
            Console.WriteLine("Response Data: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
            Console.WriteLine("Error Details: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
```

#### 13.2 Response Model: GetTransactionsResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique UUID associated with the specific API request. |
| `TransactionTime` | `DateTime` | The date and time the transaction was completed. |
| `Amount` | `decimal` | The principal transaction amount. |
| `Currency` | `string` | The ISO currency code (e.g., "YER"). |
| `SourceId` | `string` | Internal identifier for the transaction source. |
| `CompanyId` | `string` | The identifier of the company providing the service. |
| `ServiceId` | `string` | The unique identifier for the specific service used. |
| `SourceReference` | `string` | The client-side reference number provided during initiation. |
| `RequestTime` | `DateTime` | The timestamp when the request was initially logged. |
| `Details` | `string` | A detailed description or log of the transaction status. |
| `ClientId` | `string` | The identifier of the application that sent the request. |
| `CustomerId` | `string` | The unique identifier of the customer in the provider's system. |
| `Sender` | `string` | The identifier or phone number of the sender. |
| `Receiver` | `string` | The identifier or phone number of the receiver. |
| `Fees` | `decimal` | The service fee applied to the transaction. |
| `FeesCurrency` | `string` | The currency of the applied fees. |
| `Commission` | `decimal` | The commission earned/applied for this transaction. |
| `CommissionCurrency` | `string` | The currency of the commission. |
| `PublicNumber` | `string` | The transaction reference number (MTCN) provided to the customer. |
| `TransactionStatus` | `int` | Numeric code representing the current status (e.g., 1 for Success). |
| `TransactionId` | `string` | The unique system-wide transaction ID. |

---

### 13.3 Get Transaction Status
*   **Get Transaction Status:** This query service is a lightweight alternative to the full transaction query, specifically designed to retrieve the current state of a transaction. It returns essential identifiers and the status code, making it ideal for high-frequency polling or quick verification of whether a transaction has been processed, is pending, or has failed.

### 13.3 GetTransactionStatusWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | The unique reference number assigned to the transaction at the time of creation. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |
| `RequestDate` | `DateTime?` | (Optional) The specific date the transaction was requested, used to narrow the search scope. |

#### 13.3 Example: Get Transaction Status Execution

```csharp
public class TransactionStatusWithLoginQueryExample
{
    private readonly ISender _sender;

    public TransactionStatusWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetTransactionStatusAsync(CancellationToken ct = default)
    {
        var query = new GetTransactionStatusWithLoginQuery(
            ReferenceNumber: "98745632480", 
            LoginRequest: new LoginRequest 
            { 
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            Console.WriteLine("[SUCCESS] Transaction status retrieved.");
            // Displaying specific status info
            var status = result.Entity;
            Console.WriteLine($"Status Code: {status.TransactionStatus}, MTCN: {status.PublicNumber}");
            
            Console.WriteLine("Response Data: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
            Console.WriteLine("Error Details: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
```

#### 13.3 Response Model: GetTransactionStatusResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `Guid` | The unique UUID associated with the API request. |
| `TransactionTime` | `DateTime` | The timestamp indicating when the transaction reached its current state. |
| `PublicNumber` | `string` | The consumer-facing transaction number (MTCN) used for tracking or pick-up. |
| `RequestTime` | `DateTime` | The timestamp indicating when the initial request was received by the system. |
| `TransactionStatus` | `int` | The numeric code representing the status (e.g., `1` for Success, `2` for Pending). |
| `TransactionId` | `string` | The internal unique system identifier for the transaction record. |