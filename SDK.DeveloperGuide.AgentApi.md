# Developer Guide: Agent Api Services in MerchantSDK

Welcome to the **Agent Api Services** integration guide. This SDK utilizes a unified command-based architecture to streamline financial operations like Deposits, Withdrawals, and Transfers ..etc.

## 1. Core Concept

The SDK is built on a **WithLoginCommand/Request architecture**:
*   **WithLoginCommands:** Represent the specific action you want to perform (e.g., `InitiateInstantLinkDepositWithLoginCommand`).
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
In your **Deposit Services (4.1.1)**, the `LoginRequest` object can be embedded directly into the `AskingMoneyWithLoginCommand` to perform authentication and the deposit in a single logical step (In SDK), or you can use the standalone Login service shown above to manage sessions independently and call the `AskingMoneyWithTokenCommand` and pass for it the `AccessToken` instead of `LoginRequest`.


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


### 1.1 Asking Money
*   **Asking Money:** This service allows a user or system to initiate a payment request (pull transaction). It generates a `UnifiedToken` which can be used to fulfill the payment. It requires KYC details for both the requester and the payer, along with terminal information to identify the source of the request.

### 1.1 AskingMoneyWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the request (GUID recommended). |
| `ReferenceNumber` | `string` | External reference number for the transaction. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `CompanyCode` | `string` | The code of the target company/provider (e.g., "easy"). |
| `Amount` | `decimal` | The total amount requested. |
| `AmountType` | `int` | The identifier for the type of amount. |
| `Notes` | `string` | Descriptive notes for the request. |
| `CaptureMode` | `CaptureModeEnums` | Enum: `AUTO` or `MANUAL`. |
| `SenderKYC` | `LinkCustomerKYC` | Identity details of the person asking for money. |
| `ReceiverKYC` | `LinkCustomerKYC` | Identity details of the person intended to pay. |
| `Terminal` | `Terminal` | Details of the hardware or software terminal used. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service. |
| `MetaData` | `List<LinkMetaData>` | Optional custom key-value pairs. |


#### 1.1 Example: Asking Money Execution

```csharp
public class AskingMoneyExample
{
    private readonly ISender _sender;

    public AskingMoneyExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunAskingMoneyAsync(CancellationToken ct = default)
    {
        var command = new AskingMoneyWithLoginCommand(
            RequestId: "dd483dc2-4fcf-450f-9749-29335778cc51",
            ReferenceNumber: "21354587425",
            CurrencyCode: "YER",
            CompanyCode: "easy",
            Amount: 1000m,
            AmountType: 2,
            Notes: "Test Request",
            CaptureMode: CaptureModeEnums.AUTO,
            SenderKYC: new LinkCustomerKYC
            {
                FirstName = "Maan",
                SecondName = "Abdulraqeb",
                ThirdName = "Abduljabbar",
                FamilyName = "Dammaj",
                MobileNumber = "777777777"
            },
            ReceiverKYC: new LinkCustomerKYC
            {
                FirstName = "Recipient",
                SecondName = "User",
                ThirdName = "Example",
                FamilyName = "Name",
                MobileNumber = "1231381"
            },
            Terminal: new Terminal
            {
                Id = 0,
                Channel = 1,
                TerminalName = "Main Terminal"
            },
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "statement", Value = "Invoice Payment" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Unified Token: {response.UnifiedToken}");
            Console.WriteLine($"Fees: {response.Fees}, Total: {response.TotalFees}");
        }
    }
}
```

#### 1.1 Response Model: AskingMoneyResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID associated with the response. |
| `UnifiedToken` | `string` | The secure token generated for this request, used for fulfillment. |
| `Fees` | `decimal` | The base service fees for the request. |
| `Amount` | `decimal` | The original amount requested. |
| `Commission` | `decimal` | The commission portion of the fees. |
| `TotalFees` | `decimal` | The total sum of fees applied. |
| `TotalFeesCommission` | `decimal` | The total combined commission and fees. |


### 2.1 Covering Money
*   **Covering Money:** This service is used to fulfill or settle a specific financial obligation. It is typically the "Push" counterpart to an Asking Money request, where the sender proactively provides the funds to cover a required amount. It includes full KYC and terminal tracking to ensure transaction transparency.

### 2.1 CoveringMoneyWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the settlement. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `CompanyCode` | `string` | The code of the target company/provider (e.g., "easy"). |
| `Amount` | `decimal` | The total amount to be covered. |
| `AmountType` | `int` | The identifier for the type of amount. |
| `Notes` | `string` | Descriptive notes for the transaction. |
| `CaptureMode` | `CaptureModeEnums` | Settlement mode: `AUTO` (1) or `MANUAL` (2). |
| `SenderKYC` | `LinkCustomerKYC` | Identity details of the person providing the money. |
| `ReceiverKYC` | `LinkCustomerKYC` | Identity details of the person receiving the money. |
| `Terminal` | `Terminal` | Details of the terminal used to execute the covering. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service. |
| `MetaData` | `List<LinkMetaData>` | Optional custom key-value pairs. |

#### 2.1 Enum: CaptureModeEnums
| Name | Value | Description |
| :--- | :--- | :--- |
| `AUTO` | 1 | Transaction is captured automatically upon authorization. |
| `MANUAL` | 2 | Transaction requires a separate capture step. |

#### 2.1 Example: Covering Money Execution

```csharp
public class CoveringMoneyExample
{
    private readonly ISender _sender;

    public CoveringMoneyExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunCoveringMoneyAsync(CancellationToken ct = default)
    {
        var command = new CoveringMoneyWithLoginCommand(
            RequestId: "dd483dc2-4fcf-450f-9749-29335778cc51",
            ReferenceNumber: "21354587425",
            CurrencyCode: "YER",
            CompanyCode: "easy",
            Amount: 1000m,
            AmountType: 2,
            Notes: "Covering Invoice #123",
            CaptureMode: CaptureModeEnums.AUTO,
            SenderKYC: new LinkCustomerKYC
            {
                FirstName = "Maan",
                SecondName = "Abdulraqeb",
                FamilyName = "Dammaj",
                MobileNumber = "777777777"
            },
            ReceiverKYC: new LinkCustomerKYC
            {
                FirstName = "Recipient",
                SecondName = "User",
                MobileNumber = "1231381"
            },
            Terminal: new Terminal
            {
                Id = 0,
                Channel = 1,
                TerminalName = "Office-POS-01"
            },
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "statement", Value = "covering-payment" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Money Covered. Unified Token: {response.UnifiedToken}");
            Console.WriteLine($"Total Amount: {response.Amount} {result.Entity.TotalFees}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 2.1 Response Model: CoveringMoneyResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed from the command. |
| `UnifiedToken` | `string` | The secure token generated for this covering transaction. |
| `Fees` | `decimal` | The base service fees for the transaction. |
| `Amount` | `decimal` | The amount that was covered. |
| `Commission` | `decimal` | The commission earned on this transaction. |
| `TotalFees` | `decimal` | The total sum of fees charged. |
| `TotalFeesCommission` | `decimal` | The total combined commission and fees value. |


### 3.1 Money Order Execution (Confirm)
*   **Money Order Execution:** This service serves as the final confirmation and execution step for transactions initiated via the *Asking Money* or *Covering Money* services. It uses an authorization ID (often the token generated in previous steps) to finalize the fund transfer and returns the final transaction status, internal ID, and updated account balance.

### 3.1 MoneyOrderExecutionWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | The external reference number for the transaction. |
| `RequestId` | `string` | Unique identifier for this execution request (GUID). |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be executed. |
| `Notes` | `string` | Descriptive notes for the execution (e.g., "approve"). |
| `OrganizationCode` | `string` | The code of the organization (e.g., "Easy"). |
| `AutherizationType` | `AuthorizationType` | Enum: `Feed` or `Coverage`. |
| `AutherizationId` | `string` | The ID or Token obtained from the previous request step. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `MetaData` | `List<LinkMetaData>` | Optional custom key-value metadata. |

#### 3.1 Enum: AuthorizationType
| Name | Value | Description |
| :--- | :--- | :--- |
| `Feed` | 0 | Used when executing an "Asking Money" (Pull) request. |
| `Coverage` | 1 | Used when executing a "Covering Money" (Push) request. |

#### 3.1 Example: Money Order Execution

```csharp
public class MoneyOrderExecutionExample
{
    private readonly ISender _sender;

    public MoneyOrderExecutionExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunExecuteMoneyOrderAsync(CancellationToken ct = default)
    {
        var command = new MoneyOrderExecutionWithLoginCommand(
            ReferenceNumber: "87235622211",
            RequestId: "3c34e7c2-5146-406a-aecf-6cc186a5a100",
            CurrencyCode: "YER",
            Amount: 1000m,
            Notes: "approve and execute",
            OrganizationCode: "Easy",
            AutherizationType: AuthorizationType.Feed,
            AutherizationId: "1777288641086011",
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "isApprove", Value = "true" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Transaction Finalized.");
            Console.WriteLine($"Transaction ID: {response.TransactionId}");
            Console.WriteLine($"New Balance: {response.Balance} {response.CurrencyCode}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 3.1 Response Model: MoneyOrderExecutionResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReferenceNumber` | `string` | The transaction reference number. |
| `UnifiedToken` | `string` | The secure token used for this execution. |
| `Fees` | `decimal` | Base fees applied to this transaction. |
| `TotalFees` | `decimal` | The sum of all fees charged. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Commission earned on the transaction. |
| `TotalFeesCommission`| `decimal` | Combined total of fees and commission. |
| `CurrencyCode` | `string` | ISO currency code of the transaction. |
| `Amount` | `decimal` | The total executed amount. |
| `Balance` | `decimal` | The remaining balance after the transaction. |
| `TransactionStatus` | `int` | The numerical status of the transaction (e.g., 1). |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date the funds are scheduled for settlement. |
| `TransactionDate` | `string` | The actual date and time of the transaction. |
| `ProviderReference` | `string` | The reference identifier from the external provider. |

### 4.1 Direct Cash In
*   **Direct Cash In:** This service allows for the direct deposit of funds into a beneficiary's account (e.g., a mobile wallet or bank account). It combines the initiation and execution into a single step, requiring a `TokenNumber` and the target `Beneficiary` details. It is commonly used for agent-assisted deposits or top-ups.

### 4.1 DirectCashInWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the cash-in transaction. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `CompanyCode` | `string` | The code of the target provider (e.g., "easy"). |
| `Amount` | `decimal` | The total amount to be deposited. |
| `AmountType` | `int` | The identifier for the type of amount. |
| `TokenNumber` | `string` | A specific token number associated with the cash-in source. |
| `CaptureMode` | `string` | Settlement mode (e.g., "AUTO" or "MANUAL"). |
| `Beneficiary` | `LinkSourceRequest` | The details of the account receiving the funds. |
| `ReceiverKYC` | `LinkCustomerKYC` | Identity details of the recipient. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `Terminal` | `Terminal` | Optional terminal information where the cash-in is performed. |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata. |
| `Notes` | `string` | Optional descriptive notes (e.g., "Service payment"). |

#### 4.1 Example: Direct Cash In Execution

```csharp
public class CashInExample
{
    private readonly ISender _sender;

    public CashInExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunDirectCashInAsync(CancellationToken ct = default)
    {
        var command = new DirectCashInWithLoginCommand(
            RequestId: Guid.NewGuid().ToString(),
            ReferenceNumber: "65465498746",
            CurrencyCode: "YER",
            CompanyCode: "easy",
            Amount: 2000m,
            AmountType: 2,
            TokenNumber: "2221121",
            CaptureMode: "AUTO",
            Beneficiary: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.WalletId,
                AccountId = "772524472"
            },
            ReceiverKYC: new LinkCustomerKYC
            {
                FirstName = "Maan",
                SecondName = "Abdulraqeb",
                ThirdName = "Abduljabbar",
                FamilyName = "Dammaj",
                MobileNumber = "770000000"
            },
            Terminal: new Terminal
            {
                Id = 101,
                Channel = 1,
                TerminalName = "Sana'a Branch 01"
            },
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            Notes: "Service payment"
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Cash-In Completed.");
            Console.WriteLine($"Transaction ID: {response.TransactionId}");
            Console.WriteLine($"New Balance: {response.Balance} {response.CurrencyCode}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 4.1 Response Model: DirectCashInResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReferenceNumber` | `string` | The transaction reference number. |
| `UnifiedToken` | `string` | The secure token generated for this transaction. |
| `Fees` | `decimal` | Service fees applied to this cash-in. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Commission earned on the transaction. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `CurrencyCode` | `string` | ISO currency code of the transaction. |
| `Amount` | `decimal` | The total deposited amount. |
| `Balance` | `decimal` | The updated balance of the beneficiary account. |
| `TransactionStatus` | `int` | The numerical status of the transaction. |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date the funds are scheduled for settlement. |
| `TransactionDate` | `string` | The actual date and time of the deposit. |
| `ProviderReference` | `string` | The reference identifier from the external provider. |


### 5.1 Verify Cash In By Token
*   **Verify Cash In By Token:** This is the first step in a two-stage token-based cash-in process. It validates the provided `TokenNumber` against the provider's system to retrieve the receiver's identity and calculate the final fees and commissions. This ensures the details are correct before any funds are actually moved.

### 5.1 VerifyCashInByTokenWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the transaction. |
| `TokenNumber` | `string` | The specific cash-in token to be verified. |
| `CompanyCode` | `string` | The code of the target provider (e.g., "easy"). |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The transaction amount to be verified. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `Notes` | `string?` | Optional descriptive notes. |
| `MetaData` | `List<LinkMetaData>` | Optional list of custom metadata. |

#### 5.1 Example: Verify Cash In Execution

```csharp
public class CashInVerificationExample
{
    private readonly ISender _sender;

    public CashInVerificationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunVerifyCashInAsync(CancellationToken ct = default)
    {
        var command = new VerifyCashInByTokenWithLoginCommand(
            RequestId: Guid.NewGuid().ToString(),
            ReferenceNumber: "34548376117",
            TokenNumber: "33915036",
            CompanyCode: "easy",
            CurrencyCode: "YER",
            Amount: 100m,
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            Notes: "Verifying token for cash-in",
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "transactionType", Value = "1" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var verification = result.Entity;
            Console.WriteLine($"[VERIFIED]");
            Console.WriteLine($"Receiver Name: {verification.ReceiverKYC.FullName}");
            Console.WriteLine($"Unified Token: {verification.UnifiedToken}");
            Console.WriteLine($"Calculated Fees: {verification.Fees}");
        }
        else
        {
            Console.WriteLine($"[VERIFICATION FAILED] {result.Message}");
        }
    }
}
```

#### 5.1 Response Model: VerifyCashInByTokenResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReceiverKYC` | `CashReceiverKYC` | Object containing the verified receiver's identity. |
| `UnifiedToken` | `string` | The secure token generated for this session, **required for the confirmation step.** |
| `Fees` | `decimal` | The service fees that will be applied. |
| `Commission` | `decimal` | The commission that will be earned/charged. |
| `Note` | `string` | Any relevant notes from the provider. |
| `CurrencyCode` | `string` | The currency code for the transaction. |
| `Amount` | `decimal` | The original amount verified. |
| `MetaData` | `List<LinkMetaData>` | Echoed or provider-supplemented metadata. |

#### 5.1 Sub-Model: CashReceiverKYC
| Property | Type | Description |
| :--- | :--- | :--- |
| `FullName` | `string` | The full legal name of the person receiving the funds. |

### 6.1 Confirm Cash In By Token
*   **Confirm Cash In By Token:** This is the second and final step of the token-based cash-in process. After verifying the token in the previous step, this service executes the actual fund transfer. It requires the `AuthorizationId` (received from the Verify response) and a `OneTimeCode` (OTP) if applicable, to securely finalize the transaction.

### 6.1 ConfirmCashInByTokenWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the confirmation request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the transaction. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be confirmed. |
| `OrganizationCode` | `string` | The code of the organization processing the request (e.g., "Easy"). |
| `AuthorizationId` | `string` | The `UnifiedToken` or ID obtained from the **Verify** step. |
| `OneTimeCode` | `string` | The verification code or OTP required to authorize the transaction. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `MetaData` | `List<LinkMetaData>?` | Optional custom metadata for the confirmation. |

#### 6.1 Example: Confirm Cash In Execution

```csharp
public class CashInConfirmationExample
{
    private readonly ISender _sender;

    public CashInConfirmationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunConfirmCashInAsync(CancellationToken ct = default)
    {
        var command = new ConfirmCashInByTokenWithLoginCommand(
            RequestId: Guid.NewGuid().ToString(),
            ReferenceNumber: "87235622098",
            CurrencyCode: "YER",
            Amount: 1000m,
            OrganizationCode: "Easy",
            AuthorizationId: "33317599", // From Verify step
            OneTimeCode: "12354",
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "transactionType", Value = "CASHIN" },
                new LinkMetaData { Key = "accountNumber", Value = "778888888" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Transaction ID: {response.TransactionId}");
            Console.WriteLine($"Balance: {response.Balance} {response.CurrencyCode}");
            Console.WriteLine($"Status: {response.TransactionStatus}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 6.1 Response Model: ConfirmCashInByTokenResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReferenceNumber` | `string` | The transaction reference number. |
| `UnifiedToken` | `string` | The secure token associated with the completed transaction. |
| `Fees` | `decimal` | The final service fees applied. |
| `FeesCurrency` | `string` | The currency of the applied fees. |
| `Commission` | `decimal` | The final commission earned/charged. |
| `CommissionCurrency` | `string` | The currency of the commission. |
| `CurrencyCode` | `string` | ISO currency code of the transaction. |
| `Amount` | `decimal` | The total amount that was processed. |
| `Balance` | `decimal` | The updated account balance after completion. |
| `TransactionStatus` | `int` | The status code (e.g., 1 for success). |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date the funds are settled. |
| `TransactionDate` | `string` | The actual date and time the transaction occurred. |
| `ProviderReference` | `string` | The reference identifier provided by the external organization. |


### 7.1 Initiate Direct Cash Out
*   **Initiate Direct Cash Out:** This service initiates the process of withdrawing funds from a user's account (e.g., a mobile wallet) to be received as physical cash or transferred out. It identifies the source via a `TokenNumber` and the `Beneficiary` details. This is the first step in a direct withdrawal flow, returning the calculated fees and a session token.

### 7.1 InitiateDirectCashOutWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the cash-out transaction. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `CompanyCode` | `string` | The code of the target provider (e.g., "easy"). |
| `Amount` | `decimal` | The total amount to be withdrawn. |
| `AmountType` | `int` | The identifier for the type of amount. |
| `TokenNumber` | `string` | The specific token number associated with the withdrawal source. |
| `CaptureMode` | `string` | Settlement mode (e.g., "AUTO" or "MANUAL"). |
| `Beneficiary` | `LinkSourceRequest` | The details of the account from which funds are being withdrawn. |
| `Terminal` | `Terminal` | Details of the terminal/POS where the cash-out is initiated. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `Notes` | `string?` | Optional descriptive notes (e.g., "Test Cash Out"). |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata (e.g., extra charges). |

#### 7.1 Example: Initiate Direct Cash Out Execution

```csharp
public class CashOutExample
{
    private readonly ISender _sender;

    public CashOutExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunDirectCashOutAsync(CancellationToken ct = default)
    {
        var command = new InitiateDirectCashOutWithLoginCommand(
            RequestId: Guid.NewGuid().ToString(),
            ReferenceNumber: "21354587432",
            CurrencyCode: "YER",
            CompanyCode: "easy",
            Amount: 1000m,
            AmountType: 2,
            TokenNumber: "2221121",
            CaptureMode: "AUTO",
            Beneficiary: new LinkSourceRequest
            {
                AccountType = AccountType.WalletId,
                AccountId = "772524472",
            },
            Terminal: new Terminal
            {
                Id = 0,
                Channel = 1,
                TerminalName = "POS-01"
            },
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            Notes: "Test Cash Out",
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "ChargesAmount", Value = "10" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Cash-Out Initiated.");
            Console.WriteLine($"Unified Token: {response.UnifiedToken}");
            Console.WriteLine($"Fees: {response.Fees} {response.FeesCurrency}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 7.1 Response Model: InitiateDirectCashOutResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReferenceNumber` | `string` | The transaction reference number. |
| `UnifiedToken` | `string` | The secure token generated for this session, used for subsequent steps. |
| `Fees` | `decimal` | Calculated service fees for this withdrawal. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Commission amount associated with this transaction. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `CurrencyCode` | `string` | ISO currency code of the withdrawal. |
| `Amount` | `decimal` | The total amount requested for cash-out. |

### 8.1 Confirm Direct Cash Out
*   **Confirm Direct Cash Out:** This is the second and final step of the direct withdrawal process. It finalizes the transaction initiated by the *Initiate Direct Cash Out* service. By providing the `AuthorizationId` (obtained from the initiation response) and the `OneTimeCode` (OTP), the funds are officially deducted from the source account and the withdrawal is recorded.

### 8.1 ConfirmDirectCashOutWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the confirmation request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the transaction. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be confirmed for withdrawal. |
| `OrganizationCode` | `string` | The code of the organization processing the request (e.g., "Easy"). |
| `AuthorizationId` | `string` | The `UnifiedToken` or ID received from the **Initiate** step. |
| `OneTimeCode` | `string` | The secure OTP required to authorize the withdrawal. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata (e.g., transaction type, account number). |

#### 8.1 Example: Confirm Direct Cash Out Execution

```csharp
public class CashOutConfirmationExample
{
    private readonly ISender _sender;

    public CashOutConfirmationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunConfirmDirectCashOutAsync(CancellationToken ct = default)
    {
        var command = new ConfirmDirectCashOutWithLoginCommand(
            RequestId: "3c34e7c2-5146-406a-aecf-6cc186a5ad5f",
            ReferenceNumber: "87235622099",
            CurrencyCode: "YER",
            Amount: 1000m,
            OrganizationCode: "Easy",
            AuthorizationId: "44416546", // Obtained from Initiation Response
            OneTimeCode: "65451",
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "transactionType", Value = "CASHOUT" },
                new LinkMetaData { Key = "accountNumber", Value = "778888888" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Cash-Out Confirmed.");
            Console.WriteLine($"Transaction ID: {response.TransactionId}");
            Console.WriteLine($"Remaining Balance: {response.Balance} {response.CurrencyCode}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 8.1 Response Model: ConfirmDirectCashOutResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReferenceNumber` | `string` | The transaction reference number. |
| `UnifiedToken` | `string` | The secure token associated with the completed transaction. |
| `Fees` | `decimal` | Final service fees applied to the withdrawal. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Final commission amount. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `CurrencyCode` | `string` | ISO currency code of the transaction. |
| `Amount` | `decimal` | The total amount withdrawn. |
| `Balance` | `decimal` | The updated account balance after the withdrawal. |
| `TransactionStatus` | `int` | The numerical status of the transaction (e.g., 1 for Success). |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date the funds are scheduled for settlement. |
| `TransactionDate` | `string` | The actual date and time the withdrawal was finalized. |
| `ProviderReference` | `string` | The reference identifier from the external provider. |

### 9.1 Verify Cash Out By Token
*   **Verify Cash Out By Token:** This is the first step in a two-stage token-based withdrawal process. It validates a specific `TokenNumber` (typically generated by a user via their app or wallet) to verify the receiver's identity and calculate the relevant fees before the cash is handed over. This ensures the token is valid and the amount matches the system's records.

### 9.1 VerifyCashOutByTokenWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the verification request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the transaction. |
| `TokenNumber` | `string` | The specific cash-out token provided by the customer. |
| `CompanyCode` | `string` | The code of the target provider (e.g., "easy"). |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The withdrawal amount to be verified. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `Notes` | `string?` | Optional descriptive notes. |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata (e.g., transaction type). |

#### 9.1 Example: Verify Cash Out Execution

```csharp
public class CashOutVerificationExample
{
    private readonly ISender _sender;

    public CashOutVerificationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunVerifyCashOutAsync(CancellationToken ct = default)
    {
        var command = new VerifyCashOutByTokenWithLoginCommand(
            RequestId: "6769b9f1-e191-4e20-b38d-a392ca1tr2f5",
            ReferenceNumber: "34548376117",
            TokenNumber: "44870991",
            CompanyCode: "easy",
            CurrencyCode: "YER",
            Amount: 100m,
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            Notes: "",
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "transactionType", Value = "2" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var verification = result.Entity;
            Console.WriteLine($"[VERIFIED]");
            Console.WriteLine($"Receiver Name: {verification.ReceiverKYC.FullName}");
            Console.WriteLine($"Unified Token for Confirmation: {verification.UnifiedToken}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 9.1 Response Model: VerifyCashOutByTokenResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReceiverKYC` | `CashReceiverKYC` | Contains the full name of the verified receiver. |
| `UnifiedToken` | `string` | The secure session token **required for the Confirmation step.** |
| `Fees` | `decimal` | The service fees associated with this withdrawal. |
| `Commission` | `decimal` | The commission associated with this withdrawal. |
| `Note` | `string` | Relevant notes or messages from the provider. |
| `CurrencyCode` | `string` | The currency of the transaction. |
| `Amount` | `decimal` | The original amount verified. |
| `MetaData` | `List<LinkMetaData>` | Custom metadata associated with the verification. |

#### 9.1 Sub-Model: CashReceiverKYC
| Property | Type | Description |
| :--- | :--- | :--- |
| `FullName` | `string` | The full legal name of the person attempting to withdraw the cash. |

### 10.1 Confirm Cash Out By Token
*   **Confirm Cash Out By Token:** This is the second and final step of the token-based withdrawal process. After verifying the token and the receiver's identity in the previous step, this service executes the actual payout. It requires the `AuthorizationId` (obtained from the Verify response) and a `OneTimeCode` (OTP) to securely authorize the deduction of funds and complete the transaction.

### 10.1 ConfirmCashOutByTokenWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the confirmation request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the transaction. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be confirmed for withdrawal. |
| `OrganizationCode` | `string` | The code of the organization processing the request (e.g., "Easy"). |
| `AuthorizationId` | `string` | The `UnifiedToken` or ID received from the **Verify** step. |
| `OneTimeCode` | `string` | The secure OTP provided by the customer to authorize the payout. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata (e.g., transaction type, account number). |

#### 10.1 Example: Confirm Cash Out By Token Execution

```csharp
public class CashOutTokenConfirmationExample
{
    private readonly ISender _sender;

    public CashOutTokenConfirmationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunConfirmCashOutByTokenAsync(CancellationToken ct = default)
    {
        var command = new ConfirmCashOutByTokenWithLoginCommand(
            RequestId: "3c34e7c2-5146-406a-aecf-6cc186a5dd2f",
            ReferenceNumber: "87235522098",
            CurrencyCode: "YER",
            Amount: 2000m,
            OrganizationCode: "Easy",
            AuthorizationId: "44416546", // From the Verify response
            OneTimeCode: "12354",
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "transactionType", Value = "CASHOUT" },
                new LinkMetaData { Key = "accountNumber", Value = "778888888" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Token-based Cash-Out Finalized.");
            Console.WriteLine($"Transaction ID: {response.TransactionId}");
            Console.WriteLine($"Settlement Date: {response.SettlementDate}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 10.1 Response Model: ConfirmCashOutByTokenResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReferenceNumber` | `string` | The transaction reference number. |
| `UnifiedToken` | `string` | The secure token associated with the completed transaction. |
| `Fees` | `decimal` | Final service fees applied to the withdrawal. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Final commission amount. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `CurrencyCode` | `string` | ISO currency code of the transaction. |
| `Amount` | `decimal` | The total amount withdrawn. |
| `Balance` | `decimal` | The updated account balance after the withdrawal. |
| `TransactionStatus` | `int` | The numerical status of the transaction (e.g., 1 for Success). |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date the funds are scheduled for settlement. |
| `TransactionDate` | `string` | The actual date and time the withdrawal was finalized. |
| `ProviderReference` | `string` | The reference identifier from the external provider. |

### 11.1 Verify Cash Out By OTP
*   **Verify Cash Out By OTP:** This service is the verification phase of a withdrawal process that relies on One-Time Password (OTP) validation. It takes a `TokenNumber` (typically the customer's identifier or phone number) and the desired withdrawal amount to retrieve the receiver's KYC information and calculate transaction costs. The `UnifiedToken` returned in this response must be used in the subsequent confirmation step.

### 11.1 VerifyCashOutByOTPWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the verification request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the transaction. |
| `TokenNumber` | `string` | The customer identifier used to trigger the OTP flow. |
| `CompanyCode` | `string` | The code of the target provider (e.g., "easy"). |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The withdrawal amount to be verified. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `Notes` | `string?` | Optional descriptive notes. |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata for the transaction. |

#### 11.1 Example: Verify Cash Out By OTP Execution

```csharp
public class CashOutOTPVerificationExample
{
    private readonly ISender _sender;

    public CashOutOTPVerificationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunVerifyCashOutByOTPAsync(CancellationToken ct = default)
    {
        var command = new VerifyCashOutByOTPWithLoginCommand(
            RequestId: "6769b9f1-e191-4e20-b38d-a392ca1tr2f5",
            ReferenceNumber: "34548376117",
            TokenNumber: "44870991",
            CompanyCode: "easy",
            CurrencyCode: "YER",
            Amount: 100m,
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            Notes: "",
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "transactionType", Value = "2" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var verification = result.Entity;
            Console.WriteLine($"[VERIFIED]");
            Console.WriteLine($"Receiver Name: {verification.ReceiverKYC.FullName}");
            Console.WriteLine($"Unified Token for Confirmation: {verification.UnifiedToken}");
            Console.WriteLine($"Estimated Fees: {verification.Fees}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 11.1 Response Model: VerifyCashOutByOTPResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReceiverKYC` | `CashReceiverKYC` | Object containing the verified receiver's identity. |
| `UnifiedToken` | `string` | The secure session token **required for the Confirmation step.** |
| `Fees` | `decimal` | The service fees associated with this withdrawal. |
| `Commission` | `decimal` | The commission associated with this withdrawal. |
| `Note` | `string` | Relevant notes or messages from the provider. |
| `CurrencyCode` | `string` | The currency of the transaction. |
| `Amount` | `decimal` | The original amount verified. |
| `MetaData` | `List<LinkMetaData>` | Custom metadata associated with the verification. |

#### 11.1 Sub-Model: CashReceiverKYC
| Property | Type | Description |
| :--- | :--- | :--- |
| `FullName` | `string` | The full legal name of the person attempting the withdrawal. |

### 12.1 Confirm Cash Out By OTP
*   **Confirm Cash Out By OTP:** This is the final step in the OTP-based withdrawal flow. After verifying the withdrawal details in the previous step, the system sends a One-Time Password (OTP) to the customer. This service consumes that `OneTimeCode` along with the `AuthorizationId` (obtained from the Verify step) to finalize the deduction from the customer's account and complete the transaction.

### 12.1 ConfirmCashOutByOTPWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the confirmation request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the transaction. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be confirmed for withdrawal. |
| `OrganizationCode` | `string` | The code of the organization processing the request (e.g., "Easy"). |
| `AuthorizationId` | `string` | The `UnifiedToken` or ID received from the **Verify** step. |
| `OneTimeCode` | `string` | The OTP sent to the customer that authorizes the withdrawal. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata (e.g., transaction type, account number). |

#### 12.1 Example: Confirm Cash Out By OTP Execution

```csharp
public class CashOutOTPConfirmationExample
{
    private readonly ISender _sender;

    public CashOutOTPConfirmationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunConfirmCashOutByOTPAsync(CancellationToken ct = default)
    {
        var command = new ConfirmCashOutByOTPWithLoginCommand(
            RequestId: "3c34e7c2-5146-406a-aecf-6cc186a5dd2f",
            ReferenceNumber: "87235522098",
            CurrencyCode: "YER",
            Amount: 2000m,
            OrganizationCode: "Easy",
            AuthorizationId: "44416546", // From the Verify response
            OneTimeCode: "12354",
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-app-id",
                Client_secret = "your-app-secret"
            },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "transactionType", Value = "CASHOUT" },
                new LinkMetaData { Key = "accountNumber", Value = "778888888" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] OTP Cash-Out Confirmed.");
            Console.WriteLine($"Transaction ID: {response.TransactionId}");
            Console.WriteLine($"Status: {response.TransactionStatus}");
            Console.WriteLine($"Remaining Balance: {response.Balance} {response.CurrencyCode}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 12.1 Response Model: ConfirmCashOutByOTPResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReferenceNumber` | `string` | The transaction reference number. |
| `UnifiedToken` | `string` | The secure token associated with the completed transaction. |
| `Fees` | `decimal` | Final service fees applied to the withdrawal. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Final commission amount. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `CurrencyCode` | `string` | ISO currency code of the transaction. |
| `Amount` | `decimal` | The total amount withdrawn. |
| `Balance` | `decimal` | The updated account balance after the withdrawal. |
| `TransactionStatus` | `int` | The numerical status of the transaction (e.g., 1 for Success). |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date the funds are scheduled for settlement. |
| `TransactionDate` | `string` | The actual date and time the withdrawal was finalized. |
| `ProviderReference` | `string` | The reference identifier from the external provider. |

---

## Common Customer Sub-Models

These models are used across various customer-related services (Verify, Register, etc.) to maintain consistency in identity and naming data.

| Model | Property | Type | Description |
| :--- | :--- | :--- | :--- |
| **CustomerIdentity** | `IdentityTypeId` | `string` | The ID of the identity type (e.g., Passport, National ID). |
| | `IdNumber` | `string` | The unique number on the identity document. |
| | `ExpireDate` | `DateTime` | Expiration date of the document. |
| | `ReleaseDate` | `DateTime` | Date the document was issued. |
| | `DateOfBirth` | `DateTime` | The customer's birth date. |
| | `PlaceOfBirth` | `string` | City/Country of birth. |
| | `PlacOfIssue` | `string` | The office or city where the ID was issued. |
| **SubjectName** | `FirstName` | `string` | Customer's first name. |
| | `SecondName` | `string` | Father's name. |
| | `ThirdName` | `string` | Grandfather's name. |
| | `FamilyName` | `string` | Family or Last name. |

---

### 13.1 Verify Customer
*   **Verify Customer:** This service is used to validate a customer's identity and documentation before account activation or higher-tier service access. It submits comprehensive KYC data, including personal names, identity document details, professional information, and supporting file references for system approval.

### 13.1 VerifyCustomerWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The code of the service provider (e.g., "easy"). |
| `IsCompalte` | `bool` | Flag indicating if the profile information is fully completed. |
| `UserName` | `string` | Desired username for the customer. |
| `Password` | `string` | Desired password for the customer account. |
| `PhoneNumber` | `string` | The customer's primary mobile number. |
| `Address` | `string` | Detailed physical address. |
| `Nationality` | `string` | The customer's nationality. |
| `Files` | `List<string>` | List of file identifiers/paths for uploaded documents. |
| `Identity` | `CustomerIdentity` | Detailed identity document information. |
| `Gender` | `int` | Customer gender (e.g., 1 for Male, 2 for Female). |
| `Job` | `string` | Customer's occupation or job title. |
| `SubjectName` | `SubjectName` | The customer's full four-part name. |
| `Region` | `string` | State or Province. |
| `City` | `string` | City of residence. |
| `AccountType` | `int` | Primary account category ID. |
| `AccountSubtype` | `int` | Specific account sub-category ID. |
| `Terminal` | `Terminal` | Details of the terminal used for verification. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata. |

#### 13.1 Example: Verify Customer Execution

```csharp
public class CustomerVerificationExample
{
    private readonly ISender _sender;

    public CustomerVerificationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunVerifyCustomerAsync(CancellationToken ct = default)
    {
        var command = new VerifyCustomerWithLoginCommand(
            CompanyCode: "easy",
            IsCompalte: true,
            UserName: "maan_dammaj",
            Password: "SecurePassword123",
            PhoneNumber: "777777777",
            Address: "Sana'a, Al-Zubairy St",
            Nationality: "Yemeni",
            Files: new List<string> { "id_front.jpg", "id_back.jpg" },
            Identity: new CustomerIdentity(
                IdentityTypeId: "1", // National ID
                IdNumber: "01010055443",
                ExpireDate: DateTime.Now.AddYears(5),
                ReleaseDate: DateTime.Now.AddYears(-2),
                DateOfBirth: new DateTime(1990, 1, 1),
                PlaceOfBirth: "Sana'a",
                PlacOfIssue: "Civil Registry Office"
            ),
            Gender: 1,
            Job: "Software Engineer",
            SubjectName: new SubjectName(
                FirstName: "Maan",
                SecondName: "Abdulraqeb",
                ThirdName: "Abduljabbar",
                FamilyName: "Dammaj"
            ),
            Region: "Sana'a City",
            City: "Sana'a",
            AccountType: 1,
            AccountSubtype: 2,
            Terminal: new Terminal { Id = 101, Channel = 1, TerminalName = "Web-Portal" },
            LoginRequest: new LoginRequest { /* app credentials */ },
            MetaData: null
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            Console.WriteLine($"[SUCCESS] Customer Verification Submitted: {result.Message}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.ResponseCode}: {result.Message}");
        }
    }
}
```

#### 13.1 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the verification submission was accepted. |
| `ResponseCode` | `string` | The system-specific response or error code. |
| `Message` | `string` | Descriptive message regarding the result. |
| `Entity` | `object` | Usually null for verification requests unless specific profile data is returned. |

### 14.1 Register Customer
*   **Register Customer:** This service is used to create a new customer account in the wallet system. It requires comprehensive data including validated phone numbers, physical address, identity documentation, and personal details. Additionally, it requires a valid OTP (One-Time Password) ID and code to authorize and complete the registration process.

### 14.1 RegisterCustomerWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `PhoneNumber` | `string` | The customer's mobile number (primary account identifier). |
| `CompanyCode` | `string` | The code of the service provider (e.g., "Easy"). |
| `Address` | `string` | General physical address description. |
| `Nationality` | `string` | Customer's country of nationality. |
| `Files` | `List<string>` | Base64 strings or identifiers for uploaded documents (Front/Back ID, etc.). |
| `Identity` | `CustomerIdentity` | Detailed identity document data (Type, Number, Expire Date). |
| `SubjectName` | `SubjectName` | The customer's full four-part legal name. |
| `Person` | `PersonDetails` | Specific personal info (DOB, Place of birth, Gender, Job). |
| `Region` | `RegionInfo` | Geographic details including coordinates and region identifiers. |
| `CustomerOtp` | `CustomerOtpInfo` | The OTP session ID and the code received by the customer. |
| `City` | `string` | City of residence. |
| `AccountType` | `int` | Category of the account. |
| `AccountSubtype`| `int` | Specific sub-tier or subtype for the account. |
| `Terminal` | `Terminal` | Source terminal information. |
| `MetaData` | `List<LinkMetaData>` | Optional custom key-value pairs. |
| `LoginRequest` | `LoginRequest` | Admin/Operator credentials to authorize the registration. |

#### 14.1 Sub-Models (Registration Specific)
| Model | Property | Type | Description |
| :--- | :--- | :--- | :--- |
| **PersonDetails** | `DateOfBirth` | `DateTime` | Date of birth. |
| | `PlaceOfBirth` | `string` | Place of birth. |
| | `Gender` | `int` | 1 for Male, 2 for Female. |
| | `Job` | `string` | Occupation. |
| **RegionInfo** | `RegionId` | `string` | Unique identifier for the region. |
| | `Longitude` | `string` | GPS Longitude. |
| | `Latitude` | `string` | GPS Latitude. |
| | `Address` | `string` | Detailed region address. |
| **CustomerOtpInfo**| `Id` | `string` | The unique ID for the OTP session. |
| | `Code` | `string` | The numeric code entered by the user. |

#### 14.1 Example: Register Customer Execution

```csharp
public class CustomerRegistrationExample
{
    private readonly ISender _sender;

    public CustomerRegistrationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunRegisterCustomerAsync(CancellationToken ct = default)
    {
        var command = new RegisterCustomerWithLoginCommand(
            PhoneNumber: "777595455",
            CompanyCode: "Easy",
            Address: "YEM-BA-R6",
            Nationality: "Yemen",
            Files: new List<string> {
                "IlF6b3ZWWE5sY25NdlpHVjJNemd2VUdsamRIVnlaWE12UTJGd2RIVnlaVEV5TWpFekxsQk9Sdz09Ig==",
                "IlF6b3ZWWE5sY25NdlpHVjJNemd2VUdsamRIVnlaWE12UTJGd2RIVnlaVEV5TWpFekxsQk9Sdz09Ig=="
            },
            Identity: new CustomerIdentity(
                IdentityTypeId: "National",
                IdNumber: "02010065874",
                ExpireDate: DateTime.Parse("2030-01-07T01:37:34.281Z"),
                ReleaseDate: DateTime.Parse("2022-04-01T21:56:17.053Z"),
                DateOfBirth: DateTime.Parse("1980-01-07T01:37:34.281Z"),
                PlaceOfBirth: "Sana'a Office",
                PlacOfIssue: "Sana'a"
            ),
            SubjectName: new SubjectName("Hothaifaa", "Qaid", "Mohammed", "Alawi"),
            Person: new PersonDetails(
                DateOfBirth: DateTime.Parse("1980-01-07T01:37:34.281Z"),
                PlaceOfBirth: "Sana'a",
                Gender: 1,
                Job: "Engineer"
            ),
            Region: new RegionInfo("YEM-SN-R10", "2561", "2065", "Main St"),
            CustomerOtp: new CustomerOtpInfo("6dd885f4-a2ca-42c8-9063-b8a489d27370", "123658"),
            City: "Sana'a",
            AccountType: 2,
            AccountSubtype: 1,
            Terminal: new Terminal { Channel = 1 },
            MetaData: new List<LinkMetaData> { new LinkMetaData { Key = "keyName", Value = "value" } },
            LoginRequest: new LoginRequest { /* credentials */ }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            Console.WriteLine("[SUCCESS] Customer Registered successfully.");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.ResponseCode}: {result.Message}");
        }
    }
}
```

#### 14.1 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the registration was successful. |
| `ResponseCode` | `string` | System-specific code (e.g., "200" for success). |
| `Message` | `string` | Descriptive message regarding the registration result. |
| `Entity` | `object` | Returns additional context or the created user identifier if available. |

---
### 15.1 Bulk Register Customers
*   **Bulk Register Customers:** This service allows for the simultaneous registration of multiple customer profiles in a single batch request. It is designed for high-volume onboarding where many individual KYC records need to be processed at once. Each customer entry contains comprehensive identity, contact, and regional data.

### 15.1 BulkRegisterCustomersWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The code of the service provider (e.g., "Easy"). |
| `Customers` | `List<CustomerKycEntry>` | A list of individual customer registration objects. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the administrative user performing the bulk action. |

#### 15.1 Sub-Models (Bulk Specific)
| Model | Property | Type | Description |
| :--- | :--- | :--- | :--- |
| **CustomerKycEntry** | `SenderKYC` | `SenderKyc` | The individual KYC container for a customer. |
| **SenderKyc** | `MobileNumber` | `string` | Customer's mobile number. |
| | `FirstName` | `string` | Legal first name. |
| | `SecondName` | `string` | Legal father's name. |
| | `ThirdName` | `string` | Legal grandfather's name. |
| | `FamilyName` | `string` | Legal family name. |
| | `IdNumber` | `string` | Unique identity document number. |
| | `IdType` | `string` | Type of identity document (e.g., "Id-Card"). |
| | `ExpireDate` | `string` | Document expiration date (YYYY-MM-DD). |
| | `ReleaseDate` | `string` | Document issuance date (YYYY-MM-DD). |
| | `PlacOfIssue` | `string` | Office or city where the ID was issued. |
| | `DateOfBirth` | `string` | Customer's birth date (YYYY-MM-DD). |
| | `PlaceOfBirth` | `string` | City/Country of birth. |
| | `Nationality` | `string` | Customer's nationality. |
| | `Gendar` | `string` | Gender description (e.g., "Male", "Female"). |
| | `Address` | `KycAddress` | Regional address object. |
| **KycAddress** | `Region` | `string` | Unique identifier for the region (e.g., "YEM-BA-R6"). |

#### 15.1 Example: Bulk Register Customers Execution

```csharp
public class BulkRegistrationExample
{
    private readonly ISender _sender;

    public BulkRegistrationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunBulkRegisterAsync(CancellationToken ct = default)
    {
        var command = new BulkRegisterCustomersWithLoginCommand(
            CompanyCode: "Easy",
            Customers: new List<CustomerKycEntry>
            {
                new CustomerKycEntry(
                    SenderKYC: new SenderKyc(
                        MobileNumber: "778541315",
                        FirstName: "Mohammed",
                        SecondName: "Talaat",
                        ThirdName: "Ali",
                        FamilyName: "Hawash",
                        IdNumber: "01234447910",
                        IdType: "Id-Card",
                        ExpireDate: "2029-01-07",
                        ReleaseDate: "2019-04-01",
                        PlacOfIssue: "Sana'a",
                        DateOfBirth: "2001-09-26",
                        PlaceOfBirth: "Taiz",
                        Nationality: "Yemen",
                        Gendar: "Male",
                        Address: new KycAddress(Region: "YEM-BA-R6")
                    )
                ),
                new CustomerKycEntry(
                    SenderKYC: new SenderKyc(
                        MobileNumber: "778541406",
                        FirstName: "Ahmed",
                        SecondName: "Saleh",
                        ThirdName: "Salem",
                        FamilyName: "Omar",
                        IdNumber: "01234447911",
                        IdType: "Id-Card",
                        ExpireDate: "2029-01-07",
                        ReleaseDate: "2019-04-01",
                        PlacOfIssue: "Sana'a",
                        DateOfBirth: "1995-05-15",
                        PlaceOfBirth: "Aden",
                        Nationality: "Yemen",
                        Gendar: "Male",
                        Address: new KycAddress(Region: "YEM-BA-R6")
                    )
                )
            },
            LoginRequest: new LoginRequest { /* credentials */ }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            Console.WriteLine("[SUCCESS] Bulk registration processed successfully.");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.ResponseCode}: {result.Message}");
        }
    }
}
```

#### 15.1 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the bulk batch was accepted and processed. |
| `ResponseCode` | `string` | System response code for the operation. |
| `Message` | `string` | Descriptive feedback regarding the batch status. |
| `Entity` | `object` | May contain a summary of processed/failed records if applicable. |

---
### 16.1 Peer to Peer (P2P) Transfer
*   **Peer to Peer (P2P) Transfer:** This service enables the direct transfer of funds from one user's wallet or account to another. It facilitates instant "wallet-to-wallet" transactions within the same provider or ecosystem. The recipient's details (such as account ID and type) are typically passed through the `MetaData` collection.

### 16.1 PeerToPeerWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the request (GUID). |
| `ReferenceNumber` | `string` | External reference number for the P2P transaction. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be transferred. |
| `OrganizationCode` | `string` | The code of the organization/provider (e.g., "Easy"). |
| `AuthorizationType` | `string` | Authorization mode (e.g., "AUTO"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the sender. |
| `MetaData` | `List<LinkMetaData>` | **Required:** Contains recipient info (e.g., `accountId`, `accountType`). |

#### 16.1 Example: P2P Transfer Execution

```csharp
public class P2PTransferExample
{
    private readonly ISender _sender;

    public P2PTransferExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunP2PTransferAsync(CancellationToken ct = default)
    {
        var command = new PeerToPeerWithLoginCommand(
            RequestId: "3c34e7c2-5146-406a-aecf-6cc186a5a127",
            ReferenceNumber: "87235722364",
            CurrencyCode: "YER",
            Amount: 500m,
            OrganizationCode: "Easy",
            AuthorizationType: "AUTO",
            LoginRequest: new LoginRequest { /* credentials */ },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "accountId", Value = "1231381" },
                new LinkMetaData { Key = "accountType", Value = "walletId" },
                new LinkMetaData { Key = "chargesAmount", Value = "10" }
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] P2P Transfer Completed.");
            Console.WriteLine($"Transaction ID: {response.TransactionId}");
            Console.WriteLine($"Remaining Balance: {response.Balance} {response.CurrencyCode}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 16.1 Response Model: PeerToPeerResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back. |
| `ReferenceNumber` | `string` | The transaction reference number. |
| `Fees` | `decimal` | Base service fees applied to the transfer. |
| `TotalFees` | `decimal` | Total fees charged for the transaction. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Commission earned on the transaction. |
| `TotalFeesCommission`| `decimal` | Combined total of fees and commission. |
| `CurrencyCode` | `string` | ISO currency code of the transaction. |
| `Amount` | `decimal` | The total transferred amount. |
| `Balance` | `decimal` | The sender's remaining balance after the transfer. |
| `TransactionStatus` | `int` | The numerical status of the transaction. |
| `TransactionId` | `string` | The system's unique transaction identifier. |
| `SettlementDate` | `string` | The date the funds are settled. |
| `TransactionDate` | `string` | The actual date and time the transfer occurred. |
| `ProviderReference` | `string` | The reference identifier from the external provider. |

---

### 21.1 Get Companies
*   **Get Companies:** This query service retrieves a list of all active companies and providers registered in the system. It can also be used to fetch the details of a specific provider by passing an optional `CompanyCode`. This information is typically used to populate provider selection lists for remittances, bill payments, or wallet services.

### 21.1 GetCompaniesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string?` | Optional. The unique code of a specific company to retrieve. If null, all companies are returned. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 21.1 Example: Get Companies Execution

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

#### 21.1 Response Model: List<GetCompanyResponse>
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

### 22.1 Get Currencies
*   **Get Currencies:** This query service retrieves the list of supported and active currencies for a specific company or provider. It is used to ensure that transactions (such as remittances or bill payments) are initiated using a currency that the selected provider can process.

### 22.1 GetCurrenciesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for supported currencies (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 22.1 Example: Get Currencies Execution

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

#### 22.1 Response Model: GetCurrencyResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The ISO currency code (e.g., "YER", "USD", "SAR"). |
| `IsActive` | `bool` | Indicates if this currency is currently enabled for the specified provider. |

---

### 23.1 Get Regions
*   **Get Regions:** This query service retrieves a list of supported geographical or administrative regions associated with a specific company or provider. These region codes are essential for services like customer registration, KYC verification, and identifying specific service areas for remittances.

### 23.1 GetRegionsWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for supported regions (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 23.1 Example: Get Regions Execution

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

#### 23.1 Response Model: GetRegionResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique system identifier for the region (e.g., "YEM-BA-R6"). |
| `IsActive` | `bool` | Indicates if this region is currently enabled and valid for use in the system. |

---

### 24.1 Get Provinces
*   **Get Provinces:** This query service retrieves a list of provinces (or governorates) associated with a specific company or provider. This is a higher-level administrative division than regions and is used to provide structured address selection for customer registration and identity verification.

### 24.1 GetProvincesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for supported provinces (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 24.1 Example: Get Provinces Execution

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

#### 24.1 Response Model: GetProvinceResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique system identifier for the province. |
| `Name` | `string` | The localized name of the province. |
| `NameEn` | `string` | The English name of the province. |
| `IsActive` | `bool` | Indicates if this province is currently enabled for the specified provider. |

### 25.1 Get Money Requests
*   **Get Money Requests:** This query service retrieves a list of money requests (pull payments or "Asking Money" records) associated with a specific identifier, such as a mobile number or account ID. It is used to track the history and status of requests, showing when they were created, updated, and the total amount requested.

### 25.1 GetMoneyRequestsWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The code of the service provider (e.g., "EASY"). |
| `Identifier` | `string` | The unique customer identifier (e.g., phone number or account ID). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 25.1 Example: Get Money Requests Execution

```csharp
public class MoneyRequestWithLoginQueryExample
{
    private readonly ISender _sender;

    public MoneyRequestWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetMoneyRequestsAsync(CancellationToken ct = default)
    {
        var query = new GetMoneyRequestsWithLoginQuery(
            CompanyCode: "EASY", 
            Identifier: "12121212", 
            LoginRequest: new LoginRequest { /* credentials */ }
        );

        var result = await _sender.SendAsync(query, ct);

        if (result.Success)
        {
            // Note: Money requests are usually returned as a list (result.Entities)
            var requests = result.Entities; 
            Console.WriteLine("[SUCCESS] Money requests retrieved.");
            
            foreach(var req in requests)
            {
                Console.WriteLine($"Request: {req.Amount} - Created: {req.CreatedAt}");
            }
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 25.1 Response Model: GetMoneyRequestsResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `CreatedAt` | `DateTime` | The date and time when the money request was initially created. |
| `UpdateAt` | `DateTime` | The date and time when the request status was last updated. |
| `Amount` | `decimal` | The total amount requested in the transaction. |

---

### 26.1 Get Countries
*   **Get Countries:** This query service retrieves a list of supported and active countries associated with a specific company or provider. This is primarily used for cross-border services, international remittances, or determining the nationality options available for customer onboarding.

### 26.1 GetCountriesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for supported countries (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 26.1 Example: Get Countries Execution

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

#### 26.1 Response Model: GetCountryResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The ISO country code (e.g., "YE", "US", "SA"). |
| `IsActive` | `bool` | Indicates if this country is currently enabled for the specified provider. |

---

### 27.1 Get Services
*   **Get Services:** This query service retrieves a list of available business services provided by a specific company or provider. Each service is identified by a unique code, and the response includes the provider's friendly name and the operational status of the service. This is commonly used to dynamically render available transaction types (like specific billers or payment products) in a user interface.

### 27.1 GetServicesWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `CompanyCode` | `string` | The unique code of the company to check for available services (e.g., "EASY"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 27.1 Example: Get Services Execution

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

#### 27.1 Response Model: GetServiceResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique system identifier for the specific service (e.g., "821"). |
| `IsActive` | `bool` | Indicates if this specific service is currently enabled for transactions. |
| `ProvName` | `string` | The display name of the provider or the specific service category. |

---
### 28.1 Get Customer Info
*   **Get Customer Info:** This query service retrieves the full KYC (Know Your Customer) profile and current account status for a specific customer using their account number and the provider's company code. It is essential for verifying recipient details before initiating transfers, checking account eligibility, and reviewing the customer's specific fee configuration.

### 28.1 GetCustomerWithLoginQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `AccountNumber` | `string` | The unique account or mobile number of the customer to retrieve. |
| `CompanyCode` | `string` | The code of the service provider (e.g., "Easy"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the query. |

#### 28.1 Example: Get Customer Info Execution

```csharp
public class CustomerWithLoginQueryExample
{
    private readonly ISender _sender;

    public CustomerWithLoginQueryExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunGetCustomerInfoAsync(CancellationToken ct = default)
    {
        var query = new GetCustomerWithLoginQuery(
            AccountNumber: "770090090",
            CompanyCode: "Easy",
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
            var customer = result.Entity;
            Console.WriteLine($"[SUCCESS] Customer Details Found.");
            Console.WriteLine($"Name: {customer.ReceiverKYC.FirstName} {customer.ReceiverKYC.FamilyName}");
            Console.WriteLine($"Account Status: {customer.Status}");
            Console.WriteLine($"Fee Structure: {customer.TotalFees}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 28.1 Response Model: GetCustomerResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Status` | `string` | The current state of the account (e.g., "Active", "Suspended", "Unverified"). |
| `ReceiverKYC` | `LinkCustomerKYC` | Object containing the customer's full identity and contact details. |
| `TotalFees` | `decimal` | The standard service fees associated with this customer's account type. |
| `TotalFeesCommission`| `decimal` | The total fees including commission settings for this customer. |