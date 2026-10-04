
# Developer Guide: Link Services in MerchantSDK

Welcome to the **Link Services** integration guide. This SDK utilizes a unified command-based architecture to streamline financial operations like Deposits, Withdrawals, and Transfers ..etc.

## 1. Core Concept

The SDK is built on a **Command/Request architecture**:
*   **Commands:** Represent the specific action you want to perform (e.g., `InitiateInstantLinkDepositWithLoginCommand`).
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
    "BaseUrl": "https://api-dev.tharwatt.com:51000/gateway",
    "ConnectionsUri": "/v1/accounts",
    "ProfilesUri": "/accounts/v1/profiles",
    "AccountLinksUri": "/accounts/accountlinks/initiate",
    "ExternalAccountUri": "/accounts/externalaccounts/initiate",
    "BatchProfilesUri": "/accounts/v1/batchprofiles"
  },
  "GatewayLoginUriSettings": {
    "BaseUrl": "https://api-dev.tharwatt.com:51000/gateway",
    "LoginUri": "/accounts/v1/authenticate/token"
  },
  "AccountingUriSettings": {
    "BaseUrl": "https://acc-dev.tharwatt.com:55000",
    "ProfileWithLinkKycUri": "/accounts/secondaryProfile"
  }
```

### 3. Registering the SDK
In your `Program.cs`, register the configuration using the Options pattern and initialize the SDK services.

```csharp

var builder = WebApplication.CreateBuilder(args);

// 1. Bind the settings section
var settings = builder.Configuration.GetSection("UnifiedUriSettings");
builder.Services.AddOptions<UnifiedUriSettings>().Bind(settings);

var loginUriSettings = builder.Configuration.GetSection("GatewayLoginUriSettings");
builder.Services.AddOptions<GatewayLoginUriSettings>().Bind(loginUriSettings);

var accountingUriSettings = builder.Configuration.GetSection("AccountingUriSettings");
builder.Services.AddOptions<AccountingUriSettings>().Bind(accountingUriSettings);


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
In your **Deposit Services (4.1.1)**, the `LoginRequest` object can be embedded directly into the `InitiateInstantLinkDepositWithLoginCommand` to perform authentication and the deposit in a single logical step (In SDK), or you can use the standalone Login service shown above to manage sessions independently and call the `InitiateInstantLinkDepositWithTokenCommand` and pass for it the `AccessToken` instead of `LoginRequest`.



## 3. Executing a Command

The integration flow follows three simple steps:

1.  **Prepare the Command**: Create an instance of the specific Command record.
2.  **Send**: Call `await sender.SendAsync(command)`.
3.  **Handle Result**: Process the `ServiceResult<TResponse>` returned by the SDK.

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

### LoginRequest
| Property | Type | Description |
| :--- | :--- | :--- |
| `Grant_type` | `string` | Auth type ("client_credentials"). |
| `Client_id` | `string` | Your Application ID. |
| `Client_secret` | `string` | Your Application Secret. |

## Shared Response Object (Sub-Models)

### LinkBeneficiaryResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `OrganizationCode` | `string` | Unique code identifying the organization. |
| `AccountType` | `AccountType` | The type of the account (Enum). |
| `AccountId` | `string` | The unique identifier/number of the account. |
| `FirstName` | `string` | Beneficiary's first name. |
| `SecondName` | `string` | Beneficiary's middle or second name. |
| `LastName` | `string` | Beneficiary's last name. |
| `FamilyName` | `string` | Beneficiary's family or tribal name. |
| `FullName` | `string` | The full name of the beneficiary. |

---


## 4. Deposit Services

### Description
**Deposit:** The process of funding an account. It involves moving money from an external source (like a credit card, cash, or another bank) into a target account within the system. This increases the account balance.

 ### 4. Deposit Services

> ### 4.1 Instant Deposit
> *   **Instant Deposit:** Money moves directly into the target account once the source confirms.
> 
> **4.1.1 Initiate Instant Deposit**
> * **Initiate Instant Deposit:** This is the preliminary step required to validate and prepare a deposit request. It performs real-time checks on account availability and KYC data, calculates applicable fees and commissions, and generates a unique `ConfirmId`. This ID acts as a temporary reference that secures the transaction details and must be used to finalize the process in the confirmation step.
>
> **4.1.2 Confirm Instant Deposit**
> * **Confirm Instant Deposit:** This is the final step to execute a previously initiated deposit. It uses the `ConfirmId` received from the initiation step to finalize the movement of funds and update account balances.


### 4.1.1 InitiateInstantLinkDepositWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Unique transaction reference from the client side. |
| `RequestId` | `string` | Unique GUID for the specific request (for idempotency). |
| `CurrencyCode` | `string` | The currency of the transaction (e.g., "YER"). |
| `Amount` | `decimal` | The total amount for the deposit. |
| `AmountType` | `AmountType` | Defines if the amount is Exact (1) or includes other logic. |
| `Source` | `LinkSourceRequest` | Object containing details of the origin account (see shared requests 👆). |
| `Beneficiary` | `LinkSourceRequest` | Object containing details of the destination account(see shared requests 👆). |
| `SenderKYC` | `LinkCustomerKYC` | Personal details of the person sending the funds(see shared requests 👆). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the session(see shared requests 👆). |
| `Notes` | `string` | Additional remarks (Optional). |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

### 4.1.1 Example: Initiate Instant Deposit Execution

```csharp
public class InitiateInstantDepositExample 
{
    private readonly ISender _sender;

    public InitiateInstantDepositExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunInitiateInstantDepositAsync(CancellationToken cancellationToken = default)
    {
        // 1. Prepare the Command
        var command = new InitiateInstantLinkDepositWithLoginCommand(
            ReferenceNumber: "12345678823",
            RequestId: Guid.NewGuid().ToString(),
            CurrencyCode: "YER",
            Amount: 1000.00m,
            AmountType: (AmountType)1, 
            Source: new LinkSourceRequest 
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12673"
            },
            Beneficiary: new LinkSourceRequest 
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12678"
            },
            SenderKYC: new LinkCustomerKYC 
            {
                FirstName = "مؤمن",
                SecondName = "مروان",
                ThirdName = "صالح",
                FamilyName = "الجعيدي",
                MobileNumber = "776841195"
            },
            Notes: "Instant deposit test",
            LoginRequest: new LoginRequest()
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            }
        );

        // 2. Send via ISender
        var result = await _sender.SendAsync(command, cancellationToken);

        // 3. Handle the Response
         var response = result.Entity;
         Console.WriteLine($"Confirm ID: {response.ConfirmId}");
       
    }
}
```

---

## 4.1.1 Response Models

When a Deposit command is successful, the `result.Entity` contains the following structure:

### InitiateInstantLinkDepositResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ConfirmId` | `string` | Unique ID required to confirm the transaction. |
| `ExpiryDate` | `DateTime?` | When the initiation expires. |
| `RequestId` | `string` | Echoes the RequestId sent in the command. |
| `Amount` | `decimal` | The transaction amount. |
| `Fees` | `decimal` | Calculated fees for the transaction. |
| `FeesCurrency` | `string` | The currency in which fees are charged. |
| `Commission` | `decimal` | The commission amount for the transaction. |
| `CommissionCurrency` | `string` | The currency in which the commission is charged. |
| `CurrencyCode`| `string` | Currency used (e.g., "YER"). |
| `Beneficiary` | `LinkBeneficiaryResponse` | Details of the receiving party(see Shared response 👆). |


---

### 4.1.2 Confirm Instant Deposit
*   **Confirm Instant Deposit:** This is the final step to execute a previously initiated deposit. It uses the `ConfirmId` received from the initiation step to finalize the movement of funds and update account balances.

### 4.1.2 ConfirmInstantLinkDepositWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Client's reference number. |
| `RequestId` | `string` | Unique ID for the request. |
| `ConfirmId` | `string` | ID received from the initiation response. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `OrganizationCode` | `string` | Code of the handling organization. |
| `ReceiverAccountId` | `string` | ID of the receiver's account. |
| `ReceiverOrganizationCode` | `string` | Organization code of the receiver. |
| `AuthorizationType` | `string` | Type of authorization (e.g., "Token"). |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |


### Example: Confirm Instant Deposit Execution

```csharp
public class ConfirmInstantDepositExample
{
    private readonly ISender _sender;

    public ConfirmInstantDepositExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunConfirmInstantDepositAsync(CancellationToken cancellationToken = default)
    {
        // 1. Prepare the Command
        // Usually, the ConfirmId is retrieved from the Initiate step response
        var command = new ConfirmInstantLinkDepositWithLoginCommand(
            ConfirmId: "78108294411",
            ReferenceNumber: "12345678823",
            RequestId: Guid.NewGuid().ToString(),
            CurrencyCode: "YER",
            Amount: 1000.00m,
            OrganizationCode: "Link",
            ReceiverAccountId: "12678",
            ReceiverOrganizationCode: "Link",
            AuthorizationType: "Token",
            Notes: "Confirm instant deposit test",
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "Source", Value = "MobileApp" }
            },
            LoginRequest: new LoginRequest()
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            }
        );

        // 2. Send via ISender
        var result = await _sender.SendAsync(command, cancellationToken);

        // 3. Handle the Response
        var response = result.Entity;
    }
}
```

---

## 4.1.2 Response Model

When the Confirm command is successful, the `result.Entity` contains the following structure:

### ConfirmInstantLinkDepositResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Amount` | `decimal` | The final amount processed in the transaction. |
| `CurrencyCode` | `string` | The currency of the transaction (e.g., "YER"). |
| `Balance` | `decimal` | The updated balance of the account after the deposit. |
| `TransactionStatus` | `TransactionStatusEnum` | The status of the transaction (Success =1, Failed = 2, Pending = 3). |
| `TransactionId` | `string` | The unique system reference for the completed transaction. |

>### 4.2 Token-Based Deposit
> *   **Token-Based Deposit:** A multi-stage secure funding process that generates a unique redemption token. This allows for a decoupled workflow where the transaction is initiated and confirmed to generate a token, which is then "captured" at a later time or by a different system to finalize the movement of funds.

> **4.2.1 Initiate Token-Based Deposit**
>* **Initiate Token-Based Deposit:** The first step in the token workflow. It validates the request parameters, verifies KYC details, and calculates relevant fees. Upon success, it provides a `ConfirmId` needed for the next step.

> **4.2.2 Confirm Token-Based Deposit**
>* **Confirm Token-Based Deposit:** This step transitions the initiation into a secure state and generates the `Token`. This token represents the authorized funds ready to be captured.

>**4.2.3 Capture Token-Based Deposit**
> * **Capture Token-Based Deposit:** The final execution step. By providing the `Token`, the system completes the financial transfer, updates the account balances, and generates a final transaction record.

---
### 4.2.1 InitiateTokenBasedLinkDepositWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's unique transaction reference. |
| `RequestId` | `string` | Unique ID for the specific request. |
| `CurrencyCode` | `string` | Currency of the transaction (e.g., "YER"). |
| `Amount` | `decimal` | The total amount for the deposit. |
| `AmountType` | `AmountType` | Enum defining the amount calculation type. |
| `CaptureMode` | `string` | The capture strategy (e.g., "MANUAL"). |
| `Source` | `LinkSourceRequest` | Details of the account providing the funds. |
| `Beneficiary` | `LinkSourceRequest` | Details of the target account. |
| `SenderKYC` | `LinkCustomerKYC` | KYC details of the person sending the funds. |
| `LoginRequest` | `LoginRequest` | Auth credentials (client_id, secret, etc.). |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

### 4.2.1 Example: Initiate Token-Based Deposit Execution

```csharp
public class InitiateTokenBasedDepositExample 
{
    private readonly ISender _sender;

    public InitiateInstantDepositExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunInitiateTokenBasedDepositAsync(CancellationToken ct = default)
   {
     // 1. Prepare the command
    var command = new InitiateTokenBasedLinkDepositWithLoginCommand(
        ReferenceNumber: "12345678936",
        RequestId: Guid.NewGuid().ToString(),
        CurrencyCode: "YER",
        Amount: 1000.00m,
        AmountType: AmountType.Fixed, // (AmountType)1
        CaptureMode: "MANUAL",
        Source: new LinkSourceRequest
        {
            OrganizationCode = "Link",
            AccountType = AccountType.ProfileId,
            AccountId = "12678"
        },
        Beneficiary: new LinkSourceRequest
        {
            OrganizationCode = "Link",
            AccountType = AccountType.ProfileId,
            AccountId = "12776"
        },
        SenderKYC: new LinkCustomerKYC
        {
            FirstName = "عبدالكريم",
            SecondName = "شوقي",
            ThirdName = "يوسف",
            FamilyName = "أحمد",
            MobileNumber = "736687523"
        },
        Notes: "Token-Based Deposit test",
        LoginRequest: new LoginRequest()
        {
            Grant_type = "client_credentials",
            Client_id = "dev-app",
            Client_secret = "your-app-secret"
        }
    );
    // 2. Send
    var result = await sender.SendAsync(command, ct);
    var response = result.Entity;
    Console.WriteLine($"Confirm ID: {response.ConfirmId}");
   }
}
```

#### 4.2.1 Response Model: InitiateTokenBasedLinkDepositResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ConfirmId` | `string` | Unique ID required to confirm the transaction. |
| `ExpiryDate` | `DateTime?` | When the token initiation expires. |
| `RequestId` | `string` | Echoes the RequestId sent in the command. |
| `Amount` | `decimal` | The transaction amount. |
| `Fees` | `decimal` | Calculated fees for the transaction. |
| `FeesCurrency` | `string` | The currency in which fees are charged. |
| `Commission` | `decimal` | The commission amount for the transaction. |
| `CommissionCurrency` | `string` | The currency in which the commission is charged. |
| `CurrencyCode`| `string` | Currency used (e.g., "YER"). |
| `Beneficiary` | `LinkBeneficiaryResponse` | Details of the receiving party. |

---

### 4.2.2 ConfirmTokenBasedLinkDepositWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `RequestId` | `string` | Unique ID for the specific request. |
| `ConfirmId` | `string` | The ID received from the initiation response. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `OrganizationCode` | `string` | Code of the organization handling the link. |
| `ReceiverAccountId` | `string` | The account ID of the receiver. |
| `ReceiverOrganizationCode` | `string` | Organization code of the receiver. |
| `AuthorizationType` | `string` | Type of authorization (e.g., "Token"). |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

### 4.2.2 Example: Confirm Token-Based Deposit Execution

```csharp
public class ConfirmTokenBasedDepositExample 
{
    private readonly ISender _sender;

    public ConfirmTokenBasedDepositExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunConfirmTokenBasedDepositAsync( CancellationToken ct = default)
    {
        var command = new ConfirmTokenBasedLinkDepositWithLoginCommand(
        ReferenceNumber: "12345678936",
        RequestId: Guid.NewGuid().ToString(),
        ConfirmId: "78308288325", // Obtained from Initiation
        CurrencyCode: "YER",
        Amount: 1000.00m,
        OrganizationCode: "Link",
        ReceiverAccountId: "12776",
        ReceiverOrganizationCode: "Link",
        AuthorizationType: "Token",
        Notes: "Confirm token-based Deposit",
        LoginRequest: new LoginRequest()
        {
            Grant_type = "client_credentials",
            Client_id = "dev90-app",
            Client_secret = "123456"
        }
        );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
    Console.WriteLine($"Redemption Token: {response.Token}");
   }
}
```

#### 4.2.2 Response Model: ConfirmTokenBasedLinkDepositResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Token` | `string` | **The redemption token** required to capture the deposit. |
| `Amount` | `decimal` | The transaction amount. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Balance` | `decimal` | Current account balance (pre-capture). |
| `TransactionStatus` | `TransactionStatusEnum` | Status (Success = 1, Failed = 2, Pending = 3). |
| `TransactionId` | `string` | System reference for the confirmation. |

---
### 4.2.3 CaptureTokenBasedLinkDepositWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `Token` | `string` | The redemption token from the confirm step. |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `RequestId` | `string` | Unique ID for the request. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `AmountType` | `AmountType` | Enum defining the amount calculation type. |
| `ReceiverAccountId` | `string` | ID of the receiver's account. |
| `Source` | `LinkSourceRequest` | Details of the origin account. |
| `Beneficiary` | `LinkSourceRequest` | Details of the target account. |
| `ReceiverKYC` | `LinkCustomerKYC` | KYC details of the person receiving the funds. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |


### 4.2.3 Example: Capture Token-Based Deposit Execution

```csharp
public class ConfirmTokenBasedDepositExample 
{
    private readonly ISender _sender;

    public ConfirmTokenBasedDepositExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunCaptureTokenBasedDepositAsync(CancellationToken ct = default)
    {
    var command = new CaptureTokenBasedLinkDepositWithLoginCommand(
        Token: "78308288325", // Obtained from Confirmation
        ReferenceNumber: "12345678946",
        RequestId: Guid.NewGuid().ToString(),
        CurrencyCode: "YER",
        Amount: 1000.00m,
        AmountType: AmountType.Fixed,
        ReceiverAccountId: "12776",
        Source: new LinkSourceRequest
        {
          OrganizationCode = "Link",
          AccountType = AccountType.ProfileId,
          AccountId = "12776"
        },
        Beneficiary: new LinkSourceRequest
         {
           OrganizationCode = "Link",
           AccountType = AccountType.ProfileId,
           AccountId = "12678"
        },
        ReceiverKYC: new LinkCustomerKYC
        {
            FirstName = "Moumen",
            SecondName = "Marwan",
            LastName = "Al-Juaidi",
            MobileNumber = "776841195"
        },
        LoginRequest: new LoginRequest()
        {
            Grant_type = "client_credentials",
            Client_id = "dev90-app",
            Client_secret = "123456"
        }
      );

    var result = await _sender.SendAsync(command, ct);

    var response = result.Entity;
    Console.WriteLine($"Final Balance: {response.Balance}");
   }
}
```

#### 4.2.3 Response Model: CaptureTokenBasedLinkDepositResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Amount` | `decimal` | The final amount captured. |
| `Fees` | `decimal` | Total fees applied. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Total commission applied. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Balance` | `decimal` | The updated balance after successful capture. |
| `TransactionStatus` | `TransactionStatusEnum` | Final status (Success = 1, Failed = 2, Pending = 3). |
| `TransactionId` | `string` | The final unique system reference for the transaction. |

## 5. Withdraw Services

### Description
**Withdraw:** The process of removing funds from an account. It involves moving money from a target account within the system to an external destination (such as a bank account, cash payout, or mobile wallet). This decreases the account balance.

---

>### 5.1 Instant Withdraw
>*   **Instant Withdraw:** Money moves directly from the source account to the beneficiary once the >transaction is confirmed.

>**5.1.1 Initiate Instant Withdraw**
>* **Initiate Instant Withdraw:** This is the preliminary step to validate the withdrawal request. It verifies account balance availability, validates KYC data, and calculates fees. It generates a `ConfirmId` which is required to finalize the withdrawal.

>**5.1.2 Confirm Instant Withdraw**
>* **Confirm Instant Withdraw:** The final step to execute the withdrawal. It uses the `ConfirmId` to settle the transaction and update the source account balance immediately.

### 5.1.1 InitiateInstantLinkWithdrawWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's unique transaction reference. |
| `RequestId` | `string` | Unique ID for the specific request. |
| `CurrencyCode` | `string` | Currency of the transaction (e.g., "YER"). |
| `Amount` | `decimal` | The withdraw amount. |
| `AmountType` | `AmountType` | Enum defining the amount calculation type. |
| `Source` | `LinkSourceRequest` | Details of the account providing the funds. |
| `Beneficiary` | `LinkSourceRequest` | Details of the target account. |
| `SenderKYC` | `LinkCustomerKYC` | KYC details of the sender. |
| `LoginRequest` | `LoginRequest` | Auth credentials (client_id, secret, etc.). |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

### 5.1.1 Example: Initiate Instant Withdraw Execution

```csharp
public class InitiateInstantWithdrawExample 
{
    private readonly ISender _sender;

    public InitiateInstantWithdrawExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunInitiateInstantWithdrawAsync(ISender sender, CancellationToken ct = default)
   {
    var command = new InitiateInstantLinkWithdrawWithLoginCommand(
        ReferenceNumber: "12345678833",
        RequestId: Guid.NewGuid().ToString(),
        CurrencyCode: "YER",
        Amount: 1000.00m,
        AmountType: (AmountType)1,
        Source: new LinkSourceRequest { 
            OrganizationCode = "Link", 
            AccountType = AccountType.ProfileId, 
            AccountId = "12776" 
        },
        Beneficiary: new LinkSourceRequest { 
            OrganizationCode = "Link", 
            AccountType = AccountType.ProfileId, 
            AccountId = "12678" 
        },
        SenderKYC: new LinkCustomerKYC { 
            FirstName = "مؤمن", 
            SecondName = "مروان", 
            ThirdName = "صالح", 
            FamilyName = "الجعيدي", 
            MobileNumber = "776841195" 
        },
        Notes: "Instant withdraw test",
        LoginRequest: new LoginRequest()
        {
            Grant_type = "client_credentials",
            Client_id = "dev90-app",
            Client_secret = "123456"
        }
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
    Console.WriteLine($"Confirm ID: {response.ConfirmId}");
   }
}
```

#### 5.1.1 Response Model: InitiateInstantLinkWithdrawResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ConfirmId` | `string` | Unique ID required to confirm the withdrawal. |
| `ExpiryDate` | `DateTime?` | When the initiation expires. |
| `RequestId` | `string` | Echoes the RequestId sent in the command. |
| `Amount` | `decimal` | The withdrawal amount. |
| `Fees` | `decimal` | Calculated fees for the withdrawal. |
| `FeesCurrency` | `string` | The currency in which fees are charged. |
| `Commission` | `decimal` | The commission amount. |
| `CommissionCurrency` | `string` | The currency in which commission is charged. |
| `CurrencyCode`| `string` | Currency used (e.g., "YER"). |
| `Beneficiary` | `LinkBeneficiaryResponse` | Details of the receiving party. |

---

### 5.1.2 ConfirmInstantLinkDepositWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `RequestId` | `string` | Unique ID for the request. |
| `ConfirmId` | `string` | ID received from the initiation response. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `OrganizationCode` | `string` | Code of the handling organization. |
| `ReceiverAccountId` | `string` | ID of the receiver's account. |
| `ReceiverOrganizationCode` | `string` | Organization code of the receiver. |
| `AuthorizationType` | `string` | Type of authorization (e.g., "Token"). |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

### 5.1.2 Example: Confirm Instant Withdraw Execution

```csharp
using RTS.PaymentsTransfers.MerchantSDK.Features.Link.Withdraw.Instant.Commands;
public class ConfirmInstantWithdrawExample 
{
    private readonly ISender _sender;

    public ConfirmInstantWithdrawExample (ISender sender)
    {
        _sender = sender;
    }

    public async Task RunConfirmInstantWithdrawAsync(CancellationToken ct = default)
    {
    var command = new ConfirmInstantLinkWithdrawWithLoginCommand(
        ReferenceNumber: "12345678833",
        RequestId: Guid.NewGuid().ToString(),
        ConfirmId: "79308113860", // Obtained from Initiation
        CurrencyCode: "YER",
        Amount: 1000.00m,
        OrganizationCode: "Link",
        ReceiverAccountId: "12678",
        ReceiverOrganizationCode: "Link",
        AuthorizationType: "Token",
        Notes: "Confirm instant withdraw",
        LoginRequest: new LoginRequest()
        {
            Grant_type = "client_credentials",
            Client_id = "dev90-app",
            Client_secret = "123456"
        }
    );
    // 
    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
    Console.WriteLine($"Transaction ID: {response.TransactionId}");
   }
}
```

#### 5.1.2 Response Model: ConfirmInstantLinkWithdrawResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Amount` | `decimal` | The final amount withdrawn. |
| `CurrencyCode` | `string` | The currency of the transaction. |
| `Balance` | `decimal` | The updated balance of the source account. |
| `TransactionStatus` | `TransactionStatusEnum` | Status (Success = 1, Failed = 2, Pending = 3). |
| `TransactionId` | `string` | Unique system reference for the completion. |

---

>### 5.2 Token-Based Withdraw
>*   **Token-Based Withdraw:** A secure workflow where a withdrawal is authorized to produce a redemption token. This token can be used later to "capture" the cash or funds at a specific location or system.

>**5.2.1 Initiate Token-Based Withdraw**
* **Initiate Token-Based Withdraw:** Validates the intent to withdraw and prepares the transaction. Returns a `ConfirmId`.

>**5.2.2 Confirm Token-Based Withdraw**
>* **Confirm Token-Based Withdraw:** Authorizes the transaction and generates the **Redemption Token**.

>**5.2.3 Capture Token-Based Withdraw**
>* **Capture Token-Based Withdraw:** The final fulfillment step. The token is provided to finalize the disbursement and update the financial records.

---

### 5.2.1 InitiateTokenBasedLinkWithdrawWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's unique transaction reference. |
| `RequestId` | `string` | Unique ID for the specific request. |
| `CurrencyCode` | `string` | Currency of the transaction (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be withdrawn. |
| `AmountType` | `AmountType` | Enum defining the amount calculation type. |
| `CaptureMode` | `string` | The capture strategy (e.g., "MANUAL"). |
| `Source` | `LinkSourceRequest` | Details of the origin account being debited. |
| `Beneficiary` | `LinkSourceRequest` | Details of the destination for the funds. |
| `SenderKYC` | `LinkCustomerKYC` | KYC details of the person initiating the withdrawal. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |
---


### 5.2.1 Example: Initiate Token-Based Withdraw Execution

```csharp
public class InitiateTokenBasedWithdrawExample 
{
    private readonly ISender _sender;

    public InitiateTokenBasedWithdrawExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunInitiateTokenBasedWithdrawAsync(CancellationToken ct = default)
    {
     var command = new InitiateTokenBasedLinkWithdrawWithLoginCommand(
        ReferenceNumber: "12345678933",
        RequestId: Guid.NewGuid().ToString(),
        CurrencyCode: "YER",
        Amount: 1000.00m,
        AmountType: (AmountType)1,
        CaptureMode: "MANUAL",
        Source: new LinkSourceRequest 
        {
           OrganizationCode = "Link",
           AccountType = AccountType.ProfileId,
           AccountId = "12678" 
        },
        Beneficiary: new LinkSourceRequest 
        {
           OrganizationCode = "Link",
           AccountType = AccountType.ProfileId,
           AccountId = "12776" 
        },
        SenderKYC: new LinkCustomerKYC { 
            FirstName = "مؤمن", 
            SecondName = "مروان", 
            ThirdName = "صالح", 
            FamilyName = "الجعيدي", 
            MobileNumber = "776841195" 
        },
        Notes: "Token-Based Withdraw test",
        LoginRequest: new LoginRequest()
        {
            Grant_type = "client_credentials",
            Client_id = "dev90-app",
            Client_secret = "123456"
        }
    );

    var result = await _sender.SendAsync(command, ct);
    Console.WriteLine($"Confirm ID: {result.Entity.ConfirmId}");
   }
}
```
#### 5.2.1 Response Model: InitiateTokenBasedLinkWithdrawResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ConfirmId` | `string` | Unique ID required to confirm the withdrawal. |
| `ExpiryDate` | `DateTime?` | When the initiation expires. |
| `RequestId` | `string` | Echoes the RequestId sent in the command. |
| `Amount` | `decimal` | The withdrawal amount. |
| `Fees` | `decimal` | Calculated fees for the withdrawal. |
| `FeesCurrency` | `string` | The currency in which fees are charged. |
| `Commission` | `decimal` | The commission amount. |
| `CommissionCurrency` | `string` | The currency in which commission is charged. |
| `CurrencyCode`| `string` | Currency used (e.g., "YER"). |
| `Beneficiary` | `LinkBeneficiaryResponse` | Details of the receiving party. |

---

### 5.2.2 ConfirmTokenBasedLinkWithdrawWithLoginCommand

| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's unique transaction reference. |
| `RequestId` | `string` | Unique ID for the specific request. |
| `ConfirmId` | `string` | The ID received from the initiation response. |
| `CurrencyCode` | `string` | Currency of the transaction (e.g., "YER"). |
| `Amount` | `decimal` | Transaction amount to be confirmed. |
| `OrganizationCode` | `string` | Code of the organization handling the link. |
| `ReceiverAccountId` | `string` | The account ID of the receiver/beneficiary. |
| `ReceiverOrganizationCode` | `string` | Organization code of the receiver. |
| `AuthorizationType` | `string` | Type of authorization (e.g., "Token"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials (client_id, secret, etc.). |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata for the transaction. |

### 5.2.2 Example: Confirm Token-Based Withdraw Execution

```csharp
public class ConfirmTokenBasedWithdrawExample
{
    private readonly ISender _sender;

    public ConfirmTokenBasedWithdrawExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunConfirmTokenBasedWithdrawAsync( CancellationToken ct = default)
    {
    var command = new ConfirmTokenBasedLinkWithdrawWithLoginCommand(
        ReferenceNumber: "12345678933",
        RequestId: Guid.NewGuid().ToString(),
        ConfirmId: "79508201763",
        CurrencyCode: "YER",
        Amount: 1000.00m,
        OrganizationCode: "Link",
        ReceiverAccountId: "12776",
        ReceiverOrganizationCode: "Link",
        AuthorizationType: "Token",
        Notes: "Confirm token-based Withdraw",
        LoginRequest: new LoginRequest()
        {
            Grant_type = "client_credentials",
            Client_id = "dev90-app",
            Client_secret = "123456"
        }
    );

    var result = await _sender.SendAsync(command, ct);
    Console.WriteLine($"Redemption Token: {result.Entity.Token}");
   }
}
```

#### 5.2.2 Response Model: ConfirmTokenBasedLinkWithdrawResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Token` | `string` | **The redemption token** required for the capture process. |
| `Amount` | `decimal` | The transaction amount. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Balance` | `decimal` | Current account balance. |
| `TransactionStatus` | `TransactionStatusEnum` | Status (Success = 1, Failed = 2, Pending = 3). |
| `TransactionId` | `string` | System reference for the confirmation. |

---


### 5.2.3 CaptureTokenBasedLinkWithdrawWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `Token` | `string` | The redemption token from the confirm step. |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `RequestId` | `string` | Unique ID for the request. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `AmountType` | `AmountType` | Enum defining the amount calculation type. |
| `ReceiverAccountId` | `string` | ID of the account being debited. |
| `Source` | `LinkSourceRequest` | Details of the origin account. |
| `Beneficiary` | `LinkSourceRequest` | Details of the payout target. |
| `ReceiverKYC` | `LinkCustomerKYC` | KYC details of the person withdrawing. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

### 5.2.3 Example: Capture Token-Based Withdraw Execution

```csharp
public class CaptureTokenBasedWithdrawExample
{
    private readonly ISender _sender;

    public CaptureTokenBasedWithdrawExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunCaptureTokenBasedWithdrawAsync( CancellationToken ct = default)
   {
    var command = new CaptureTokenBasedLinkWithdrawWithLoginCommand(
        Token: "79508201763", // Obtained from Confirmation
        ReferenceNumber: "12345678941",
        RequestId: Guid.NewGuid().ToString(),
        CurrencyCode: "YER",
        Amount: 1000.00m,
        AmountType: (AmountType)1,
        ReceiverAccountId: "12776",
        Source: new LinkSourceRequest 
        {
           OrganizationCode = "Link",
           AccountType = AccountType.ProfileId,
           AccountId = "12776" 
        },
        Beneficiary: new LinkSourceRequest 
        {
           OrganizationCode = "Link",
           AccountType = AccountType.ProfileId,
           AccountId = "12678" 
        },
        ReceiverKYC: new LinkCustomerKYC { 
            FirstName = "مؤمن", 
            SecondName = "مروان", 
            ThirdName = "صالح", 
            FamilyName = "الجعيدي", 
            MobileNumber = "776841195" 
        },
        Notes: "Token-Based Withdraw test",
        LoginRequest: new LoginRequest()
        {
            Grant_type = "client_credentials",
            Client_id = "dev90-app",
            Client_secret = "123456"
        }
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
    Console.WriteLine($"Final Balance: {response.Balance}");
   }
}
```

#### 5.2.3 Response Model: CaptureTokenBasedLinkWithdrawResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Amount` | `decimal` | The final amount captured/disbursed. |
| `Fees` | `decimal` | Total fees applied. |
| `FeesCurrency` | `string` | Currency of the fees. |
| `Commission` | `decimal` | Total commission applied. |
| `CommissionCurrency` | `string` | Currency of the commission. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Balance` | `decimal` | The updated balance after successful capture. |
| `TransactionStatus` | `TransactionStatusEnum` | Final status (Success = 1, Failed = 2, Pending = 3). |
| `TransactionId` | `string` | Final unique system reference. |

## 6. Transfer Services

### Description
**Transfer:** The process of moving funds directly between two accounts within the system (Peer-to-Peer). Unlike deposits or withdrawals which interface with external sources, a transfer represents an internal movement of value from a sender's account to a beneficiary's account.

---

>### 6.1 Instant Transfer
>*   **Instant Transfer:** A direct, real-time movement of funds from the sender to the receiver.

>**6.1.1 Initiate Instant Transfer**
>* **Initiate Instant Transfer:** Validates the transfer request, checks the sender's balance, and calculates the total costs (including fees and commissions). It generates authorization tokens (like `UnifiedToken`) required for the confirmation step.

>**6.1.2 Confirm Instant Transfer**
>* **Confirm Instant Transfer:** Finalizes the internal movement of funds using the authorization ID provided during initiation. It updates both balances and generates a settlement record.

### 6.1.1 InitiateInstantLinkTransferWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique ID for the request. |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `AmountType` | `AmountType` | Enum defining the amount calculation type. |
| `CaptureMode` | `string` | Capture method (e.g., "MANUAL"). |
| `IsBeneficiaryInitiated` | `bool` | Whether the beneficiary started the request. |
| `Source` | `LinkSourceRequest` | Sender account details. |
| `Beneficiary` | `LinkSourceRequest` | Receiver account details. |
| `SenderKYC` | `LinkCustomerKYC` | KYC details of the sender. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |


### 6.1.1 Example: Initiate Instant Transfer Execution

```csharp
public class TransferExample
{
    private readonly ISender _sender;

    public TransferExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunInitiateInstantTransferAsync(CancellationToken ct = default)
    {
        // Prepare the command
        var command = new InitiateInstantLinkTransferWithLoginCommand(
            RequestId: Guid.NewGuid().ToString(),
            ReferenceNumber: "71548376349",
            CurrencyCode: "YER",
            Amount: 1100.00m,
            AmountType: AmountType.Exact,
            CaptureMode: "MANUAL",
            IsBeneficiaryInitiated: false,
            Source: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12776"
            },
            Beneficiary: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12678"
            },
            SenderKYC: new LinkCustomerKYC
            {
                FirstName = "عبدالكريم",
                SecondName = "شوقي",
                ThirdName = "يوسف",
                FamilyName = "احمد",
                MobileNumber = "782422822"
            },
            Notes: "Instant Transfer test",
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            }
        );
        // send
        var result = await _sender.SendAsync(command, ct);
        // handle response 
        var response = result.Entity;
        Console.WriteLine($"Unified Token: {response.UnifiedToken}");
    }
}
```
#### 6.1.1 Response Model: InitiateInstantLinkTransferResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Echoes the RequestId sent in the command. |
| `ReferenceNumber` | `string` | The merchant's reference number. |
| `ExpiryDate` | `DateTime` | When the initiation request expires. |
| `Beneficiary` | `LinkSourceRequest` | Details of the receiving account. |
| `ReceiverKyc` | `LinkCustomerKYC` | KYC details of the receiver. |
| `ReSourceToken` | `string` | Specific resource token for the sender. |
| `UnifiedToken` | `string` | The token used as `AuthorizationId` in the confirm step. |
| `Fees` | `decimal` | Base fees for the transfer. |
| `TotalFees` | `decimal` | Total fees including sub-components. |
| `FeesCurrency` | `string` | Currency used for fees. |
| `Commission` | `decimal` | Base commission. |
| `TotalFeesCommission` | `decimal` | Total commission applied. |
| `CommissionCurrency` | `string` | Currency used for commission. |
| `CurrencyCode` | `string` | Currency of the transfer amount. |
| `Amount` | `decimal` | Transfer amount. |
| `ProviderReference` | `string` | Internal provider tracking reference. |

---

### 6.1.2 ConfirmInstantLinkTransferWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique ID for the request. |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `OrganizationCode` | `string` | Code of the organization. |
| `AuthorizationType` | `string` | Auth type (e.g., "UNIFIED"). |
| `AuthorizationId` | `string` | The token ID (UnifiedToken) from initiation. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

### 6.1.2 Example: Confirm Instant Transfer Execution

```csharp
public class TransferExample
{
    private readonly ISender _sender;

    public TransferExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunConfirmInstantTransferAsync(CancellationToken ct = default)
   {
    var command = new ConfirmInstantLinkTransferWithLoginCommand(
        RequestId: "3f6d0f7e-0bd9-4a81-a258-305c8a0df168",
        ReferenceNumber: "17757263513436",
        CurrencyCode: "YER",
        Amount: 1100.00m,
        OrganizationCode: "Link",
        AuthorizationType: "UNIFIED",
        AuthorizationId: "75609458951", // Obtained from UnifiedToken in initiation
        LoginRequest:  new LoginRequest
        {
          Grant_type = "client_credentials",
          Client_id = "dev-app",
          Client_secret = "your-app-secret"
        }
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
    Console.WriteLine($"Transaction ID: {response.TransactionId}");
   }
}
```

#### 6.1.2 Response Model: ConfirmInstantLinkTransferResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Final transferred amount. |
| `Balance` | `decimal` | Remaining balance of the sender. |
| `TransactionStatus` | `TransactionStatusEnum` | Status (Success = 1, Failed = 2, Pending = 3). |
| `TransactionId` | `string` | Unique system transaction reference. |
| `SettlementDate` | `DateTime` | Date the transaction was settled. |
| `TransactionDate` | `DateTime` | Date the transaction was recorded. |
| `ProviderReference` | `string` | Provider reference string. |

---

>### 6.2 Token-Based Transfer
>*   **Token-Based Transfer:** A decoupled transfer process that generates a redemption token. This allows the sender to authorize a transfer that the receiver can claim at a later time by "capturing" the token.

>**6.2.1 Initiate Token-Based Transfer**
>* Validates the P2P request and produces a `ConfirmId`.

>**6.2.2 Confirm Token-Based Transfer**
>* Consumes the `ConfirmId` to generate a secure `Token` for redemption.

>**6.2.3 Capture Token-Based Transfer**
>* The final step where the `Token` is presented to actually move the funds into the beneficiary account.

---
### 6.2.1 InitiateTokenBasedLinkTransferWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's unique transaction reference. |
| `RequestId` | `string` | Unique ID for the specific request. |
| `CurrencyCode` | `string` | Currency of the transaction (e.g., "YER"). |
| `Amount` | `decimal` | The total amount to be withdrawn. |
| `AmountType` | `AmountType` | Enum defining the amount calculation type. |
| `CaptureMode` | `string` | The capture strategy (e.g., "MANUAL"). |
| `Source` | `LinkSourceRequest` | Details of the origin account being debited. |
| `Beneficiary` | `LinkSourceRequest` | Details of the destination for the funds. |
| `SenderKYC` | `LinkCustomerKYC` | KYC details of the person initiating the withdrawal. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |
---

### 6.2.1 Example: Initiate Token-Based Transfer Execution

```csharp
public class TransferExample
{
    private readonly ISender _sender;

    public TransferExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunInitiateTokenTransferAsync(CancellationToken ct = default)
    {
    var command = new InitiateTokenBasedLinkTransferWithLoginCommand(
        ReferenceNumber: "12345678935",
        RequestId: Guid.NewGuid().ToString(),
        CurrencyCode: "YER",
        Amount: 1000.00m,
        AmountType: (AmountType)1,
        CaptureMode: "MANUAL",
        Source: new LinkSourceRequest 
        { 
            OrganizationCode = "Link", 
            AccountType = AccountType.ProfileId, 
            AccountId = "12678" 
        },
        Beneficiary: new LinkSourceRequest 
        { 
            OrganizationCode = "Link", 
            AccountType = AccountType.ProfileId, 
            AccountId = "12776" 
        },
        SenderKYC: new LinkCustomerKYC 
        { 
            FirstName = "عبدالكريم", 
            SecondName = "شوقي", 
            FamilyName = "أحمد", 
            MobileNumber = "736687523" 
        },
        LoginRequest:   new LoginRequest
        {
          Grant_type = "client_credentials",
          Client_id = "dev-app",
          Client_secret = "your-app-secret"
        }
    );

    var result = await _sender.SendAsync(command, ct);
    Console.WriteLine($"Confirm ID: {result.Entity.ConfirmId}");
   }
}
```

#### 6.2.1 Response Model: InitiateTokenBasedLinkTransferResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ConfirmId` | `string` | ID required for the confirmation step. |
| `ExpiryDate` | `DateTime?` | Expiry of the initiation request. |
| `RequestId` | `string` | Request tracking ID. |
| `Amount` | `decimal` | Requested amount. |
| `Fees` | `decimal` | Transaction fees. |
| `Commission` | `decimal` | Transaction commission. |
| `CurrencyCode` | `string` | Currency (e.g., "YER"). |
| `Beneficiary` | `LinkBeneficiaryResponse` | Details of the target beneficiary. |

---

### 6.2.2 ConfirmTokenBasedLinkTransferWithLoginCommand

| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's unique transaction reference. |
| `RequestId` | `string` | Unique ID for the specific request. |
| `ConfirmId` | `string` | The ID received from the initiation response. |
| `CurrencyCode` | `string` | Currency of the transaction (e.g., "YER"). |
| `Amount` | `decimal` | Transaction amount to be confirmed. |
| `OrganizationCode` | `string` | Code of the organization handling the link. |
| `ReceiverAccountId` | `string` | The account ID of the receiver/beneficiary. |
| `ReceiverOrganizationCode` | `string` | Organization code of the receiver. |
| `AuthorizationType` | `string` | Type of authorization (e.g., "Token"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials (client_id, secret, etc.). |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata for the transaction. |
### 6.2.2 Example: Confirm Token-Based Transfer Execution

```csharp
public class TransferExample
{
    private readonly ISender _sender;

    public TransferExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunConfirmTokenTransferAsync(CancellationToken ct = default)
   {
    var command = new ConfirmTokenBasedLinkTransferWithLoginCommand(
        ReferenceNumber: "12345678935",
        RequestId: Guid.NewGuid().ToString(),
        ConfirmId: "76709455145",
        CurrencyCode: "YER",
        Amount: 1000.00m,
        OrganizationCode: "Link",
        ReceiverAccountId: "12776",
        ReceiverOrganizationCode: "Link",
        AuthorizationType: "Token",
        LoginRequest:   new LoginRequest
        {
          Grant_type = "client_credentials",
          Client_id = "dev-app",
          Client_secret = "your-app-secret"
        }
    );

    var result = await _sender.SendAsync(command, ct);
    Console.WriteLine($"Redemption Token: {result.Entity.Token}");
   }
}
```
#### 6.2.2 Response Model: ConfirmTokenBasedLinkTransferResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Token` | `string` | **The redemption token** required for the capture process. |
| `Amount` | `decimal` | The transaction amount. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Balance` | `decimal` | Current account balance. |
| `TransactionStatus` | `TransactionStatusEnum` | Status (Success = 1, Failed = 2, Pending = 3). |
| `TransactionId` | `string` | System reference for the confirmation. |
---

### 6.2.3 CaptureTokenBasedLinkTransferWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `Token` | `string` | The redemption token from the confirm step. |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `RequestId` | `string` | Unique ID for the request. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `AmountType` | `AmountType` | Enum defining the amount calculation type. |
| `ReceiverAccountId` | `string` | ID of the account being debited. |
| `Source` | `LinkSourceRequest` | Details of the origin account. |
| `Beneficiary` | `LinkSourceRequest` | Details of the payout target. |
| `ReceiverKYC` | `LinkCustomerKYC` | KYC details of the person withdrawing. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |


### 6.2.3 Example: Capture Token-Based Transfer Execution

```csharp
public class TransferExample
{
    private readonly ISender _sender;

    public TransferExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunCaptureTokenTransferAsync(CancellationToken ct = default)
   {
    var command = new CaptureTokenBasedLinkTransferWithLoginCommand(
        Token: "76709455145",
        ReferenceNumber: "12345678941",
        RequestId: Guid.NewGuid().ToString(),
        CurrencyCode: "YER",
        Amount: 1000.00m,
        AmountType: (AmountType)1,
        ReceiverAccountId: "12776",
        Source: new LinkSourceRequest 
        { 
            OrganizationCode = "Link", 
            AccountType = AccountType.ProfileId, 
            AccountId = "12678" 
        },
        Beneficiary: new LinkSourceRequest 
        { 
            OrganizationCode = "Link", 
            AccountType = AccountType.ProfileId, 
            AccountId = "12776" 
        },
        ReceiverKYC: new LinkCustomerKYC 
        { 
            FirstName = "Moumen", 
            SecondName = "Marwan", 
            FamilyName = "Al-Juaidi", 
            MobileNumber = "776841195" 
        },
        LoginRequest:    new LoginRequest
        {
          Grant_type = "client_credentials",
          Client_id = "dev-app",
          Client_secret = "your-app-secret"
        }
    );

    var result = await _sender.SendAsync(command, ct);
    Console.WriteLine($"Transfer Captured. New Balance: {result.Entity.Balance}");
   }
}
```

#### 6.2.3 Response Model: CaptureTokenBasedLinkTransferResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Amount` | `decimal` | Final captured amount. |
| `Fees` | `decimal` | Final fees applied. |
| `Commission` | `decimal` | Final commission applied. |
| `CurrencyCode` | `string` | Transaction currency. |
| `Balance` | `decimal` | Updated account balance. |
| `TransactionStatus` | `TransactionStatusEnum` | Final status of the capture. |
| `TransactionId` | `string` | Unique completion reference. |

## 7. Payment Services

### Description
**Payment:** The process of using account funds to pay for goods or services. While similar to a transfer, a payment often includes additional authorization layers (like OTP or Redemption Tokens) to ensure security between a payer (Source) and a merchant or service provider (Beneficiary).

---

>### 7.1 Instant Payment
>*   **Instant Payment:** A real-time payment execution where the sender authorizes an immediate debit from their account to the beneficiary's account.

>**7.1.1 Initiate Instant Payment**
>*   **Initiate Instant Payment:** Validates the payment details, checks for account sufficiency, and calculates the total cost including fees. It generates a `UnifiedToken` required for the final confirmation.

>**7.1.2 Confirm Instant Payment**
>*   **Confirm Instant Payment:** Finalizes the payment using the `AuthorizationId` (UnifiedToken) >generated during the initiation phase.

### 7.1.1 InitiateInstantLinkPaymentWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique ID for the specific request (for idempotency). |
| `ReferenceNumber` | `string` | Merchant's unique transaction reference. |
| `CurrencyCode` | `string` | Currency of the transaction (e.g., "YER"). |
| `Amount` | `decimal` | The total payment amount. |
| `AmountType` | `AmountType` | Enum defining how the amount is calculated. |
| `CaptureMode` | `string` | The capture strategy (e.g., "MANUAL" or "AUTO"). |
| `IsBeneficiaryInitiated` | `bool` | Flag indicating if the payment was triggered by the receiver. |
| `Source` | `LinkSourceRequest` | Details of the account paying the funds. |
| `Beneficiary` | `LinkSourceRequest` | Details of the account receiving the funds. |
| `SenderKYC` | `LinkCustomerKYC` | KYC details of the payer. |
| `LoginRequest` | `LoginRequest` | Auth credentials (client_id, secret, etc.). |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata for the transaction. |

#### 7.1.1 Example: Initiate Instant Payment Execution

```csharp
public class PaymentExample
{
    private readonly ISender _sender;

    public PaymentExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunInitiateInstantPaymentAsync(CancellationToken ct = default)
    {
        var command = new InitiateInstantLinkPaymentWithLoginCommand(
            RequestId: Guid.NewGuid().ToString(),
            ReferenceNumber: "71548376349",
            CurrencyCode: "YER",
            Amount: 1100.00m,
            AmountType: AmountType.Exact,
            CaptureMode: "MANUAL",
            IsBeneficiaryInitiated: false,
            Source: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12776"
            },
            Beneficiary: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12678"
            },
            SenderKYC: new LinkCustomerKYC
            {
                FirstName = "عبدالكريم",
                SecondName = "شوقي",
                ThirdName = "يوسف",
                FamilyName = "احمد",
                MobileNumber = "782422822"
            },
            Notes: "Instant Payment test",
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            }
        );

        var result = await _sender.SendAsync(command, ct);
        var response = result.Entity;
        Console.WriteLine($"Unified Token: {response.UnifiedToken}");
    }
}
```

#### 7.1.1 Response Model: InitiateInstantLinkPaymentResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `UnifiedToken` | `string` | The token used as `AuthorizationId` in the confirm step. |
| `ExpiryDate` | `DateTime` | When the initiation expires. |
| `Amount` | `decimal` | The base payment amount. |
| `Fees` | `decimal` | Calculated transaction fees. |
| `TotalFees` | `decimal` | The sum of all applicable fees. |
| `Commission` | `decimal` | Calculated commission. |
| `CurrencyCode` | `string` | Payment currency. |
| `Beneficiary` | `LinkSourceRequest` | Details of the receiving party. |
---
### 7.1.2 Confirm Instant Payment
*   **Confirm Instant Payment:** Finalizes the immediate payment request using the authorization ID obtained from the initiation step.

### 7.1.2 ConfirmInstantLinkPaymentWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique ID for the specific request. |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount to be confirmed. |
| `OrganizationCode` | `string` | Code of the organization handling the link. |
| `AuthorizationType` | `string` | Type of authorization used (e.g., "UNIFIED"). |
| `AuthorizationId` | `string` | The `UnifiedToken` obtained from the initiation step. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

#### 7.1.2 Example: Confirm Instant Payment Execution

```csharp
public class ConfirmPaymentExample
{
    private readonly ISender _sender;

    public ConfirmPaymentExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunConfirmInstantPaymentAsync(CancellationToken ct = default)
    {
        var command = new ConfirmInstantLinkPaymentWithLoginCommand(
            RequestId: "a93a35df-1162-424d-be44-1966ca5804fc",
            ReferenceNumber: "17758934965320",
            CurrencyCode: "YER",
            Amount: 1100.00m,
            OrganizationCode: "Link",
            AuthorizationType: "UNIFIED",
            AuthorizationId: "74811678074",
            LoginRequest: new LoginRequest 
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            },
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "Device", Value = "POS-01" }
            }
        );

        var result = await _sender.SendAsync(command, ct);
        if (result.Success)
        {
            Console.WriteLine($"Payment Confirmed. Transaction ID: {result.Entity.TransactionId}");
        }
    }
}

```
>### 7.2 Token-Based Payment
>*   **Token-Based Payment:** A three-step secure workflow. It generates a specific `TokenNumber` that acts as a secure pre-authorization, which is then verified (Confirmed) and finally executed (Captured).

>**7.2.1 Initiate Token-Based Payment**
>* Validates the request and returns a `UnifiedToken` to proceed.

### 7.2.1 InitiateTokenBasedLinkPaymentWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `RequestId` | `string` | Unique ID for the request. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `AmountType` | `AmountType` | Enum defining amount type. |
| `CaptureMode` | `string` | Method of capture. |
| `IsBeneficiaryInitiated` | `bool` | Flag for beneficiary initiation. |
| `Source` | `LinkSourceRequest` | Payer account details. |
| `Beneficiary` | `LinkSourceRequest` | Merchant/Receiver account details. |
| `SenderKYC` | `LinkCustomerKYC` | Payer KYC details. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional notes. |
| `MetaData` | `List<LinkMetaData>` | Custom metadata. |


### 7.2.1 Example: Initiate Token-Based Payment Execution

```csharp
public class TokenPaymentExample
{
    private readonly ISender _sender;

    public TokenPaymentExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunInitiateTokenBasedPaymentAsync(CancellationToken ct = default)
    {
        var command = new InitiateTokenBasedLinkPaymentWithLoginCommand(
            ReferenceNumber: "38548678917",
            RequestId: Guid.NewGuid().ToString(),
            CurrencyCode: "YER",
            Amount: 1300m,
            AmountType: AmountType.Exact,
            CaptureMode: "MANUAL",
            IsBeneficiaryInitiated: true,
            Source: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12776"
            },
            Beneficiary: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12678"
            },
            SenderKYC: new LinkCustomerKYC
            {
                FirstName = "عبدالكريم",
                SecondName = "شوقي",
                ThirdName = "يوسف",
                FamilyName = "احمد",
                MobileNumber = "782422822"
            },
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            },
            Notes: "Token Payment Initiation"
        );

        var result = await _sender.SendAsync(command, ct);
        Console.WriteLine($"Unified Token: {result.Entity.UnifiedToken}");
    }
}
```

### 7.2.1 Response Model: InitiateTokenBasedLinkPaymentResponse

When the initiation of a token-based payment is successful, the `result.Entity` contains the following structure. This step prepares the transaction and generates the tokens required for authorization.

| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Echoes the RequestId sent in the command. |
| `ReferenceNumber` | `string` | The merchant's reference number. |
| `ExpiryDate` | `DateTime` | Date and time when this initiation expires. |
| `ReSourceToken` | `string` | Internal token identifying the source resource. |
| `UnifiedToken` | `string` | The token used to authorize the next step. |
| `Fees` | `decimal` | The base fee for the payment. |
| `TotalFees` | `decimal` | The total fees including all sub-components. |
| `FeesCurrency` | `string` | The currency in which fees are charged. |
| `Commission` | `decimal` | The base commission for the transaction. |
| `TotalFeesCommission` | `decimal` | Total commission applied to the fees. |
| `CommissionCurrency` | `string` | The currency in which commission is charged. |
| `CurrencyCode` | `string` | The currency used for the payment (e.g., "YER"). |
| `Amount` | `decimal` | The transaction amount. |
| `ProviderReference` | `string` | The tracking reference from the underlying provider. |
| `MetaData` | `List<LinkMetaData>` | Additional custom data associated with the request. |

---
>**7.2.2 Confirm Token-Based Payment**
> * Uses a `TokenNumber` (pre-shared or system-generated) to confirm the payment's validity.

### 7.2.2 ConfirmTokenBasedLinkPaymentWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique ID for the request. |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `AmountType` | `AmountType` | Enum defining amount type. |
| `CaptureMode` | `string` | Method of capture. |
| `TokenNumber` | `string` | The shared redemption/payment token. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional notes. |
| `MetaData` | `List<LinkMetaData>` | Custom metadata. |

### 7.2.2 Example: Confirm Token-Based Payment Execution

```csharp
public class TokenPaymentExample
{
    private readonly ISender _sender;

    public TokenPaymentExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunConfirmTokenBasedPaymentAsync(CancellationToken ct = default)
    {
        var command = new ConfirmTokenBasedLinkPaymentWithLoginCommand(
            RequestId: "fa3b2b0e-e912-4306-a7f2-24950310b9d9",
            ReferenceNumber: "17758978068801",
            CurrencyCode: "YER",
            Amount: 1300m,
            AmountType: (AmountType)1,
            CaptureMode: "MANUAL",
            TokenNumber: "8761389", // The Redemption/Shared Token
            LoginRequest:  new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            },
            Notes: "Confirming via Token Number"
        );

        var result = await _sender.SendAsync(command, ct);
        var response = result.Entity;
        
    }
}
```

### 7.2.2 Response Model: ConfirmTokenBasedLinkPaymentResponse

When the confirmation is successful, the system returns the following details. At this stage, the transaction is authorized, and KYC details for both parties are finalized before the capture phase.

| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Echoes the RequestId sent in the command. |
| `ReferenceNumber` | `string` | The merchant's reference number. |
| `ExpiryDate` | `DateTime` | The expiry date for the confirmed authorization. |
| `Source` | `LinkSourceRequest` | Detailed information of the paying account. |
| `Beneficiary` | `LinkSourceRequest` | Detailed information of the receiving account. |
| `SenderKYC` | `LinkCustomerKYC` | Verified KYC details of the sender. |
| `ReceiverKYC` | `LinkCustomerKYC` | Verified KYC details of the receiver. |
| `ReSourceToken` | `string` | Updated resource token after confirmation. |
| `UnifiedToken` | `string` | **The finalized token used as `AuthorizationId` in the Capture step.** |
| `Fees` | `decimal` | Confirmed base fees. |
| `TotalFees` | `decimal` | Confirmed total fees. |
| `Commission` | `decimal` | Confirmed base commission. |
| `TotalFeesCommission` | `decimal` | Confirmed total fees commission. |
| `CurrencyCode` | `string` | Transaction currency. |
| `Amount` | `decimal` | The final amount to be paid. |

---
**7.2.3 Capture Token-Based Payment**
* Finalizes the financial settlement using the `AuthorizationId`.


### 7.2.3 CaptureTokenBasedLinkPaymentWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `RequestId` | `string` | Unique ID for the request. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | The final amount to be captured. |
| `OrganizationCode` | `string` | Code of the organization handling the link. |
| `AuthorizationType` | `string` | Type of authorization (e.g., "Token"). |
| `AuthorizationId` | `string` | The specific Token ID or Authorization ID to be captured. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

#### 7.2.3 Example: Capture Token-Based Payment Execution

```csharp
public class TokenPaymentExample
{
    private readonly ISender _sender;

    public TokenPaymentExample(ISender sender)
    {
        _sender = sender;
    }
    public async Task RunCaptureTokenPaymentAsync(CancellationToken ct = default)
   {
    var command = new CaptureTokenBasedLinkPaymentWithLoginCommand(
        ReferenceNumber: "19521654789",
        RequestId: "469b5e13-8fc0-49df-af23-2a08d7ab2c45",
        CurrencyCode: "YER",
        Amount: 1300m,
        OrganizationCode: "Link",
        AuthorizationType: "Token",
        AuthorizationId: "8761389", // The TokenNumber confirmed previously
        LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            },
        MetaData: new List<LinkMetaData>
        {
            new LinkMetaData { Key = "Source", Value = "POS-Terminal-01" }
        }
    );

    var result = await _sender.SendAsync(command, ct);

    var response = result.Entity;
   }
}
```

#### 7.2.3 Response Model: CaptureTokenBasedLinkPaymentResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Amount` | `decimal` | Final captured amount. |
| `Balance` | `decimal` | Updated account balance. |
| `TransactionStatus` | `int` | Status (1 = Success, 2 = Failed, 3 = Pending). |
| `TransactionId` | `string` | System reference for the transaction. |
| `CurrencyCode` | `string` | Transaction currency. |

---

### 7.3 OTP (One-Time Password) Payment
*   **OTP Payment:** A highly secure payment method where the transaction is authorized via a code sent to the receiver’s/payer's mobile phone.

**7.3.1 Initiate OTP Payment**
* Prepares the transaction and triggers the system to send an SMS/OTP to the registered mobile number. 
* *Note: The `OneTimeCode` is **not** returned in the response; it is sent directly to the receiver.*

### 7.3.1 InitiateOTPLinkPaymentWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's unique transaction reference. |
| `RequestId` | `string` | Unique ID for the request. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | The total payment amount. |
| `AmountType` | `AmountType` | Enum defining the amount type. |
| `CaptureMode` | `string` | The capture strategy (e.g., "MANUAL"). |
| `IsBeneficiaryInitiated` | `bool` | Flag indicating if the receiver started the request. |
| `Source` | `LinkSourceRequest` | Details of the account providing the funds. |
| `Beneficiary` | `LinkSourceRequest` | Details of the target account. |
| `ReceiverKYC` | `LinkCustomerKYC` | KYC details of the receiver (used to send the OTP). |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

### 7.3.1 Example: Initiate OTP Payment Execution

```csharp
public class OtpPaymentExample
{
    private readonly ISender _sender;

    public OtpPaymentExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunInitiateOTPPaymentAsync(CancellationToken ct = default)
    {
        var command = new InitiateOTPLinkPaymentWithLoginCommand(
            ReferenceNumber: "54548396713",
            RequestId: Guid.NewGuid().ToString(),
            CurrencyCode: "YER",
            Amount: 1100m,
            AmountType: AmountType.Exact,
            CaptureMode: "MANUAL",
            IsBeneficiaryInitiated: true,
            Source: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12678"
            },
            Beneficiary: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "12776"
            },
            ReceiverKYC: new LinkCustomerKYC
            {
                FirstName = "عبدالكريم",
                SecondName = "شوقي",
                ThirdName = "يوسف",
                FamilyName = "أحمد",
                MobileNumber = "782422822"
            },
            LoginRequest:  new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            },
        );

        var result = await _sender.SendAsync(command, ct);
        // Note: The OneTimeCode is sent to the Receiver's MobileNumber via SMS.
        var respnse = result.Entity;
    }
}
```

### 7.3.1 Response Model: InitiateOTPLinkPaymentResponse
When the OTP initiation is successful, the system sends an SMS to the receiver and returns the following details:

| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | The merchant reference. |
| `RequestId` | `string` | Unique request tracking ID. |
| `ExpiryDate` | `DateTime?` | When the OTP/Initiation expires. |
| `Source` | `LinkSourceRequest` | The payer's account details. |
| `Beneficiary` | `LinkSourceRequest` | The receiver's account details. |
| `SenderKYC` | `LinkCustomerKYC` | KYC details of the payer. |
| `ReceiverKYC` | `LinkCustomerKYC` | KYC details of the receiver. |
| `ReSourceToken` | `string` | Internal source identifier. |
| `UnifiedToken` | `string` | **The token to be used as `AuthorizationId` in the confirm step.** |
| `Fees` | `decimal` | Transaction fees. |
| `TotalFees` | `decimal` | Total fees calculated. |
| `Commission` | `decimal` | Transaction commission. |
| `TotalFeesCommission` | `decimal` | Total commission applied. |
| `CurrencyCode` | `string` | Transaction currency (e.g., "YER"). |
| `Amount` | `decimal` | The base amount. |
| `ProviderReference` | `string` | Internal provider reference. |


**7.3.2 Confirm OTP Payment**
* Finalizes the payment by providing the `OneTimeCode` received via phone.
---
### 7.3.2 ConfirmOTPLinkPaymentWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ReferenceNumber` | `string` | Merchant's reference number. |
| `RequestId` | `string` | Unique ID for the request. |
| `CurrencyCode` | `string` | Currency of the transaction. |
| `Amount` | `decimal` | Transaction amount. |
| `OrganizationCode` | `string` | Code of the organization. |
| `AuthorizationId` | `string` | The UnifiedToken from initiation. |
| `OneTimeCode` | `string` | The 6-digit code sent to the phone. |
| `AuthorizationType` | `string` | Auth type (e.g., "UNIFIED"). |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom metadata. |


#### 7.3.2 Example: Confirm OTP Payment Execution

```csharp
public class OtpPaymentExample
{
    private readonly ISender _sender;

    public OtpPaymentExample(ISender sender)
    {
        _sender = sender;
    }
     public async Task RunConfirmOTPPaymentAsync(CancellationToken ct = default)
     {
    var command = new ConfirmOTPLinkPaymentWithLoginCommand(
        ReferenceNumber: "17759033202601",
        RequestId: "20a511dc-c16c-4ff9-9502-839f6731308c",
        CurrencyCode: "YER",
        Amount: 1100m,
        OrganizationCode: "Link",
        AuthorizationId: "70711210935", // The UnifiedToken from Initiation
        OneTimeCode: "790427", // The code the user received via phone
        AuthorizationType: "UNIFIED",
        LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            },
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
   }
}
```

#### 7.3.2 Response Model: ConfirmOTPLinkPaymentResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Balance` | `decimal` | The balance after the OTP payment. |
| `TransactionId` | `string` | Unique completion reference. |
| `TransactionStatus` | `int` | Status (1 = Success, 2 = Failed, 3 = Pending). |
| `Amount` | `decimal` | The processed amount. |
| `TransactionDate` | `DateTime` | Date of transaction. |
| `ProviderReference` | `string` | Reference from the underlying provider. |

## 8. Account Services

### Description
**Account Services:** These services allow for the management and linking of financial accounts within the system. This includes connecting a user profile to specific bank accounts or mobile wallets (Link Accounts) and defining the technical configurations for external financial providers (External Accounts).

---

### 8.1 Create Account Link
*   **Create Account Link:** This service establishes a connection between an internal system profile and a specific account held at an external organization (e.g., linking a user to their specific bank account number).

### 8.1 CreateAccountLinkWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ProfileId` | `int` | Internal system profile ID. |
| `ExternalAccountType` | `int` | External category ID. |
| `OrganizationCode` | `string` | Code of the organization (e.g., "Hitar"). |
| `AccountNo` | `string` | The actual bank/wallet account number. |
| `SortNo` | `string` | Sorting or routing code. |
| `Currency` | `string` | Account currency. |
| `Iban` | `string` | IBAN formatted account number. |
| `LinkReference` | `string` | Reference for the link. |
| `LinkMode` | `int` | Mode of linking. |
| `Status` | `int` | Account status. |
| `CustomerId` | `string` | Customer identifier at the provider. |
| `ClientId` | `string` | Client identifier. |
| `Priority` | `int` | Priority level (1 being high). |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom metadata. |

#### 8.1 Example: Create Account Link Execution

```csharp
public class AccountManagementExample
{
    private readonly ISender _sender;

    public AccountManagementExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunCreateAccountLinkAsync(CancellationToken ct = default)
    {
        var command = new CreateAccountLinkWithLoginCommand(
            ProfileId: 31,
            ExternalAccountType: 4,
            OrganizationCode: "Hitar",
            AccountNo: "777777779",
            SortNo: "777777777",
            Currency: "YER",
            Iban: "777777777",
            LinkReference: "",
            LinkMode: 1,
            Status: 1,
            CustomerId: "",
            ClientId: "",
            Priority: 1,
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            },
            MetaData: null
        );

        var result = await _sender.SendAsync(command, ct);
        var response = result.Entity;
       
    }
}
```

#### 8.1 Response Model: CreateAccountLinkResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ProfileId` | `int` | The ID of the internal profile. |
| `ExternalAccountType` | `int` | The type category of the external account. |
| `OrganizationCode` | `string` | The code of the organization (e.g., "Hitar"). |
| `AccountNo` | `string` | The linked account number. |
| `SortNo` | `string` | The sort code or routing number. |
| `Currency` | `string` | The account currency. |
| `Iban` | `string` | The International Bank Account Number. |
| `LinkMode` | `int` | The mode of linking (e.g., 1 for manual). |
| `Status` | `int` | The current status code. |
| `IsActive` | `bool` | Indicates if the link is currently active. |
| `AccountType` | `string` | Friendly name/description of the account type. |
| `IsDefault` | `bool` | Whether this is the primary linked account. |
| `Priority` | `int` | Selection priority for this account. |

---

### 8.2 Create External Account
*   **Create External Account:** This service is used to define the full technical integration parameters for an external provider, including credentials, URLs, and custom provider properties.


### 8.2 CreateExternalAccountWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `Priority` | `int` | Priority of the account. |
| `Balance` | `decimal` | Initial balance. |
| `SiteUrl` | `string` | URL of the external provider site. |
| `IsHttps` | `bool` | SSL requirement flag. |
| `ZoneId` | `string` | Zone identifier. |
| `AgentCode` | `string` | Agent/Merchant code. |
| `Code` | `string` | System code. |
| `GrantType` | `string` | Authentication grant type. |
| `Ip` | `string` | Allowed IP address. |
| `Password` | `string` | Connection password. |
| `ProfileId` | `int` | Profile identifier. |
| `ProviderId` | `string` | ID of the provider. |
| `ServiceType` | `int` | Type of service. |
| `Type` | `string` | Category (e.g., "CUSTOMER"). |
| `UserName` | `string` | Connection username. |
| `User_Id` | `string` | Provider user ID. |
| `VirtualProfileId` | `int` | Virtual profile identifier. |
| `IsActive` | `bool` | Activation flag. |
| `Status` | `int` | Status code. |
| `RowDateTime` | `DateTime` | Timestamp of the record. |
| `CertificateUrl` | `string` | Path to SSL certificate. |
| `CertificatePass` | `string` | Certificate password. |
| `CustomerId` | `string` | Provider customer ID. |
| `ClientId` | `string` | Client identifier. |
| `AuthorizedProfileId` | `int` | Authorizing profile ID. |
| `AuthorizedClientId` | `string` | Authorizing client ID. |
| `Name` | `string` | Local name. |
| `NameEn` | `string` | English name. |
| `Description` | `string` | Account description/notes. |
| `CustomPropertiesValues` | `List<CustomPropertyItem>` | List of provider-specific properties. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom metadata. |

#### CustomPropertyItem
Used to send provider-specific dynamic properties.
| Property | Type | Description |
| :--- | :--- | :--- |
| `CustomPropertiesId` | `int` | The ID of the predefined custom property. |
| `Value` | `string` | The value to assign to that property. |

#### 8.2 Example: Create External Account Execution

```csharp
public class ExternalAccountExample
{
    private readonly ISender _sender;

    public ExternalAccountExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunCreateExternalAccountAsync(CancellationToken ct = default)
    {
        var command = new CreateExternalAccountWithLoginCommand(
            Priority: 1,
            Balance: 100000m,
            SiteUrl: "https://fff.ye",
            IsHttps: true,
            ZoneId: "1",
            AgentCode: "swaid",
            Code: "1578",
            GrantType: "0",
            Ip: "192.168.11.11",
            Password: "11111",
            ProfileId: 6678,
            ProviderId: "1004",
            ServiceType: 2,
            Type: "CUSTOMER",
            UserName: "MohammedTalat",
            User_Id: "233",
            VirtualProfileId: 12617,
            IsActive: true,
            Status: 1,
            RowDateTime: DateTime.Parse("2026-01-13T12:00:00"),
            CertificateUrl: "",
            CertificatePass: "",
            CustomerId: "0",
            ClientId: "",
            AuthorizedProfileId: 14334,
            AuthorizedClientId: "7657",
            Name: "test",
            NameEn: "testt",
            Description: "note",
            CustomPropertiesValues: new List<CustomPropertyItem>
            {
                new(6, "Value 1"),
                new(16, "Value 2")
            },
            LoginRequest: new LoginRequest { /* ... */ },
            MetaData: null
        );

        var result = await _sender.SendAsync(command, ct);
        var response = result.Entity;
        
    }
}
```

#### 8.2 Response Model: CreateExternalAccountResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `int` | The unique system ID for this external account. |
| `ProfileId` | `int` | The associated profile ID. |
| `VirtualProfileId` | `int` | The associated virtual profile ID. |
| `User_Id` | `string` | The external provider user ID. |
| `UserName` | `string` | Login username for the external service. |
| `ProviderId` | `string` | Identifier for the provider service. |
| `Type` | `string` | The account type (e.g., "CUSTOMER"). |
| `Ip` | `string` | Authorized IP address for the connection. |
| `Balance` | `decimal` | Initial or current balance. |
| `IsActive` | `bool` | Activation status. |
| `Verfied` | `bool` | Whether the account has been verified. |
| `SiteUrl` | `string` | The integration endpoint URL. |
| `Name` | `string` | Account name (Local). |
| `NameEn` | `string` | Account name (English). |
| `Priority` | `int` | Execution priority. |
| `IsDefault` | `bool` | Whether this is the default external provider. |

---

### 8.3 Update Account Link
*   **Update Link Account:** This service allows for the modification of existing linked account details. It is used to update parameters such as account priority, status, or to correct account identifiers like the IBAN or Sort Number for an existing link.

### 8.3 UpdateAccountLinkWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ExternalAccountType` | `int` | External category ID. |
| `OrganizationCode` | `string` | Organization code. |
| `AccountNo` | `string` | Account number. |
| `SortNo` | `string` | Routing number. |
| `Currency` | `string` | Account currency. |
| `Iban` | `string` | IBAN number. |
| `LinkReference` | `string` | Reference for the link. |
| `LinkMode` | `int` | Linking behavior. |
| `Status` | `int` | Status of the account. |
| `CustomerId` | `string` | Provider customer ID. |
| `ClientId` | `string` | Client identifier. |
| `Priority` | `int` | Priority level. |
| `LoginRequest` | `LoginRequest` | Auth credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom metadata. |

#### 8.3 Example: Update Account Link Execution

```csharp
public class UpdateAccountExample
{
    private readonly ISender _sender;

    public UpdateAccountExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunUpdateAccountLinkAsync(CancellationToken ct = default)
    {
        var command = new UpdateAccountLinkWithLoginCommand(
            ExternalAccountType: 4,
            OrganizationCode: "Hitar",
            AccountNo: "777777779",
            SortNo: "777777777",
            Currency: "YER",
            Iban: "777777777",
            LinkReference: "",
            LinkMode: 1,
            Status: 1,
            CustomerId: "",
            ClientId: "",
            Priority: 2,
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            },
            MetaData: null
        );

        var result = await _sender.SendAsync(command, ct);
        var response = result.Entity;
    }
}
```

#### 8.3 Response Model: UpdateAccountLinkResponse

The response confirms the updated state of the linked account.

| Property | Type | Description |
| :--- | :--- | :--- |
| `ProfileId` | `int` | The ID of the internal profile associated with the account. |
| `ExternalAccountType` | `int` | The type category of the external account. |
| `OrganizationCode` | `string` | The code of the organization. |
| `AccountNo` | `string` | The linked account number. |
| `SortNo` | `string` | The sort code or routing number. |
| `Currency` | `string` | The account currency. |
| `Iban` | `string` | The updated International Bank Account Number. |
| `LinkMode` | `int` | The mode of linking. |
| `Status` | `int` | The updated status code. |
| `IsActive` | `bool` | Indicates if the link is currently active. |
| `AccountType` | `string` | Friendly name/description of the account type. |
| `IsDefault` | `bool` | Whether this is the primary linked account. |
| `Priority` | `int` | The updated selection priority. |



## 9. Link Connection Services

### 9.1 Initiate Connection
*   **Initiate Connection:** The first step in linking an external financial account. It specifies the account to be connected and the verification method to be used to prove ownership.

### 9.1 InitiateConnectionWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique ID for the specific request. |
| `Currency` | `string` | The currency code (e.g., "XYU"). |
| `VerificationMethod` | `VerificationMethod` | The method used for verification (Enum). |
| `Source` | `LinkSourceRequest` | Details of the account to be connected. |
| `SenderKYC` | `LinkCustomerKYC` | KYC details of the requester. |
| `LoginRequest` | `LoginRequest` | Authentication credentials. |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

#### 9.1 Example: Initiate Connection Execution

```csharp
public class ConnectionExample
{
    private readonly ISender _sender;

    public ConnectionExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunInitiateConnectionAsync(CancellationToken ct = default)
    {
        var command = new InitiateConnectionWithLoginCommand(
            RequestId: Guid.NewGuid().ToString(),
            Currency: "XYU",
            VerificationMethod: (VerificationMethod)3,
            Source: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.AccountId,
                AccountId = "12776",
                SubAccountId = ""
            },
            SenderKYC: new LinkCustomerKYC
            {
                FirstName = "عبدالكريم",
                SecondName = "شوقي",
                ThirdName = "يوسف",
                FamilyName = "احمد",
                MobileNumber = "782422822"
            },
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev90-app",
                Client_secret = "your-app-secret"
            },
            Notes: "Unified request for connection",
            MetaData: new List<LinkMetaData>
            {
                new LinkMetaData { Key = "lastTransactionDate", Value = "2025-12-11" }
            }
        );

        var result = await _sender.SendAsync(command, ct);
        var response = result.Entity;
    }
}
```

#### 9.1 Response Model: InitiateConnectionResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `VerificationId` | `int` | The unique ID assigned to this verification attempt. |
| `Status` | `int` | Current status code of the initiation. |
| `Message` | `string` | Descriptive status or error message. |

---

### 9.2 Verify Connection
*   **Verify Connection:** The second step where the user provides the verification code (OTP) received via the method specified during initiation.

### 9.2 VerifyConnectionWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique ID for the specific request. |
| `VerificationId` | `int` | The ID obtained from the Initiate response. |
| `OneTimeCode` | `string` | The 6-digit verification code provided by the user. |
| `LoginRequest` | `LoginRequest` | Authentication credentials. |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

#### 9.2 Example: Verify Connection Execution

```csharp
public async Task RunVerifyConnectionAsync(CancellationToken ct = default)
{
    var command = new VerifyConnectionWithLoginCommand(
        RequestId: Guid.NewGuid().ToString(),
        VerificationId: 69,
        OneTimeCode: "991955",
        LoginRequest: new LoginRequest { /* ... */ },
        Notes: "Unified request to verify a connection",
        MetaData: new List<LinkMetaData>()
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
}
```

#### 9.2 Response Model: VerifyConnectionResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `IsVerified` | `bool` | Indicates if the provided code was valid and verified. |

---

### 9.3 Authorize Connection
*   **Authorize Connection:** Assigns permission scopes to the verified connection and provides the URL needed for the user to grant access.

### 9.3 AuthorizeConnectionWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique ID for the specific request. |
| `VerificationId` | `int` | The verified initiation ID. |
| `Scopes` | `string` | Permission scopes (e.g., "accounting.balance.query"). |
| `CallBackUrl` | `string` | The URL to redirect to after successful authorization. |
| `LoginRequest` | `LoginRequest` | Authentication credentials. |
| `Notes` | `string` | Optional transaction notes. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

#### 9.3 Example: Authorize Connection Execution

```csharp
public async Task RunAuthorizeConnectionAsync(CancellationToken ct = default)
{
    var command = new AuthorizeConnectionWithLoginCommand(
        RequestId: Guid.NewGuid().ToString(),
        VerificationId: 69,
        Scopes: "accounting.balance.query",
        CallBackUrl: "https://rts-gw-appdev:5001/api/callback",
        LoginRequest: new LoginRequest { /* ... */ },
        Notes: "Unified request to verify a connection",
        MetaData: new List<LinkMetaData>()
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
}
```

#### 9.3 Response Model: AuthorizeConnectionResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ConnectionId` | `int` | The ID of the established connection. |
| `Status` | `string` | Current authorization status. |
| `AuthorizationUrl` | `string` | The URL used to complete the user authorization flow. |

---

### 9.4 Token Connection
*   **Token Connection:** Exchanges the authorization code for access and refresh tokens to enable API access to the linked account.

### 9.4 TokenConnectionWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ConnectionId` | `int` | The ID of the established connection. |
| `GrantType` | `string` | Auth grant type (e.g., "authorization_code"). |
| `ClientId` | `string` | The application client ID. |
| `ClientSecret` | `string` | The application client secret. |
| `Code` | `string` | The authorization code received from the callback. |
| `RequestedScope` | `string` | The specific scopes requested for the token. |
| `RedirectUri` | `string` | The redirect URI used during authorization. |
| `LoginRequest` | `LoginRequest` | Authentication credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

#### 9.4 Example: Token Connection Execution

```csharp
public async Task RunTokenConnectionAsync(CancellationToken ct = default)
{
    var command = new TokenConnectionWithLoginCommand(
        ConnectionId: 61,
        GrantType: "authorization_code",
        ClientId: "dev90-app",
        ClientSecret: "123456",
        Code: "5CC163A0EF33364AA2DDDB08707BCE2E4FF98999883730108F793D044851184A",
        RequestedScope: "accounting.balance.query",
        RedirectUri: "https://rts-gw-appdev:5001/api/callback",
        LoginRequest: new LoginRequest { /* ... */ },
        MetaData: new List<LinkMetaData>()
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
}
```

#### 9.4 Response Model: TokenConnectionResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `AccessToken` | `string` | The token used to authorize subsequent API requests. |
| `ConnectionId` | `int` | The ID of the associated connection. |
| `TokenType` | `string` | The type of token (e.g., "Bearer"). |
| `ExpiresIn` | `int` | Seconds until the access token expires. |
| `RefreshToken` | `string` | Token used to obtain a new access token. |
| `Scope` | `string` | The final granted scopes for this token. |

---

### 9.5 Refresh Token Connection
*   **Refresh Token Connection:** Uses a refresh token to generate a new access token when the current one expires.

### 9.5 RefreshTokenConnectionWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ConnectionId` | `int` | The ID of the established connection. |
| `ClientId` | `string` | The application client ID. |
| `RefreshToken` | `string` | The valid refresh token. |
| `LoginRequest` | `LoginRequest` | Authentication credentials. |
| `MetaData` | `List<LinkMetaData>` | Custom key-value metadata. |

#### 9.5 Example: Refresh Token Connection Execution

```csharp
public async Task RunRefreshTokenConnectionAsync(CancellationToken ct = default)
{
    var command = new RefreshTokenConnectionWithLoginCommand(
        ConnectionId: 61,
        ClientId: "dev90-app",
        RefreshToken: "481447B500606A9D15490DCBAF0C6A61A34D3BC89C35F8C4C7D3FF11746B5493",
        LoginRequest: new LoginRequest { /* ... */ },
        MetaData: null
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
}
```

#### 9.5 Response Model: TokenConnectionResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `AccessToken` | `string` | The new access token. |
| `ConnectionId` | `int` | The associated connection ID. |
| `TokenType` | `string` | The type of token (e.g., "Bearer"). |
| `ExpiresIn` | `int` | Seconds until the new token expires. |
| `RefreshToken` | `string` | A new refresh token |
| `Scope` | `string` | The granted scopes. |

## 10. Profile Services

### Description
**Profile Services:** This service manages the creation and registration of system profiles (such as Merchants or Customers). It allows for the comprehensive setup of a profile, including identity verification, contact information, business details, user credentials, and financial acquirer settings in either single or batch processing modes.

---

### 10.1 Create Link Profile
*   **Create Link Profile:** Registers a single new profile into the system with all associated metadata, business information, and default account settings.

### 10.1 CreateLinkProfileWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `LinkProfile` | `CreateLinkProfile` | Object containing all details for the new profile (see below). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the session. |


### CreateLinkProfile
| Property | Type | Description |
| :--- | :--- | :--- |
| `Subject` | `Subject` | Personal name details of the profile owner. |
| `IntegrationType` | `string` | The technical integration mode (e.g., "API"). |
| `Identity` | `Identity` | Official identification details. |
| `Address` | `Address` | Physical location and address details. |
| `Contact` | `Contact` | Contact methods (phone, email, etc.). |
| `Business` | `Business` | Commercial and business-specific details. |
| `User` | `User` | Account login credentials. |
| `Acquirer` | `Acquirer` | Settlement and bank account configurations. |
| `Pos` | `Pos` | Point of Sale configuration settings. |
| `ProfileType` | `string` | Category of the profile (e.g., "Merchant"). |
| `ServiceType` | `string` | The type of service provided. |
| `MetaData` | `List<LinkMetaData>` | Additional custom metadata. |

### Subject
| Property | Type | Description |
| :--- | :--- | :--- |
| `FirstName` | `string` | Payer/Owner's first name. |
| `MiddleName` | `string` | Payer/Owner's middle name. |
| `LastName` | `string` | Payer/Owner's last name. |

### Identity
| Property | Type | Description |
| :--- | :--- | :--- |
| `IdNumber` | `string` | The unique number on the identification document. |
| `IdType` | `string` | The type of ID (e.g., "Card", "Passport"). |

### Address
| Property | Type | Description |
| :--- | :--- | :--- |
| `Country` | `string` | Country name. |
| `City` | `string` | City name. |
| `Area` | `string` | District or area name. |
| `Details` | `string` | Specific street or building details. |
| `Location` | `string` | GPS coordinates (Latitude, Longitude). |

### Contact
| Property | Type | Description |
| :--- | :--- | :--- |
| `PhoneNumber` | `string` | Primary contact phone number. |
| `FaxNumber` | `string` | Fax number. |
| `Email` | `string` | Primary email address. |
| `Details` | `string` | Additional contact remarks. |

### Business
| Property | Type | Description |
| :--- | :--- | :--- |
| `NameAr` | `string` | Business name in Arabic. |
| `NameEn` | `string` | Business name in English. |
| `PhoneNumber` | `string` | Business contact number. |
| `ActivityType` | `string` | Industry or activity type (e.g., "Sports"). |
| `Address` | `string` | Business physical address. |
| `Description` | `string` | Short description of the business. |

### User
| Property | Type | Description |
| :--- | :--- | :--- |
| `Email` | `string` | User's email for login. |
| `PhoneNumber` | `string` | User's phone for verification. |
| `UserName` | `string` | Unique username for system access. |
| `Password` | `string` | Secure password for the account. |

### Acquirer
| Property | Type | Description |
| :--- | :--- | :--- |
| `ExternalAccountType`| `int` | Category ID of the external account. |
| `BankCode` | `int` | Code identifying the specific bank. |
| `AccountNumber` | `string` | The bank account number. |
| `SortNumber` | `string` | Bank sorting or routing number. |
| `Iban` | `string` | International Bank Account Number. |
| `CurrencyCode` | `string` | Currency used for the account (e.g., "YER"). |

### Pos
| Property | Type | Description |
| :--- | :--- | :--- |
| `Name` | `string` | Point of Sale display name. |
| `NameEn` | `string` | English name for the POS. |
| `IsShown` | `bool` | Whether the POS is visible in the UI. |
| `ServiceType` | `int` | Internal service type ID. |
| `CompanyCode` | `string` | Associated company code. |
| `AccountId` | `string` | Linked account identifier. |
| `Code` | `string` | Unique terminal code. |

---


#### 10.1 Example: Create Link Profile Execution

```csharp
public class ProfileManagementExample
{
    private readonly ISender _sender;

    public ProfileManagementExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunCreateProfileAsync(CancellationToken ct = default)
    {
        var command = new CreateLinkProfileWithLoginCommand(
            LinkProfile: new CreateLinkProfile
            {
                Subject = new Subject
                {
                    FirstName = "Waleed33",
                    MiddleName = "Waleed33",
                    LastName = "Waleed332"
                },
                IntegrationType = "API",
                Identity = new Identity
                {
                    IdNumber = "0tlf95393326",
                    IdType = "Card"
                },
                Address = new Address
                {
                    Country = "YEMEN",
                    City = "YE-3",
                    Area = "Re-2",
                    Details = "Re-2",
                    Location = "37.12234,-120.5456"
                },
                Contact = new Contact
                {
                    PhoneNumber = "736687523",
                    FaxNumber = "0123245",
                    Email = "Waleed332@gmail.com",
                    Details = "Waleed33"
                },
                Business = new Business
                {
                    NameAr = "شركة Waleed33 ",
                    NameEn = "Waleed33 2co",
                    PhoneNumber = "777438943",
                    ActivityType = "Sports",
                    Address = "Sanaa Street",
                    Description = "this for Madrista only "
                },
                User = new User
                {
                    Email = "Waleed332@gmail.com",
                    PhoneNumber = "736687523",
                    UserName = "Waleed33kkk",
                    Password = "123456"
                },
                Acquirer = new Acquirer
                {
                    ExternalAccountType = 5,
                    BankCode = 1,
                    AccountNumber = "123684864",
                    SortNumber = "554",
                    Iban = string.Empty,
                    CurrencyCode = "YER"
                },
                Pos = new Pos
                {
                    Name = "Waleed33",
                    NameEn = "Waleed332",
                    IsShown = true,
                    ServiceType = 2,
                    CompanyCode = "Link",
                    AccountId = "24",
                    Code = "24"
                },
                ProfileType = "تاجر",
                ServiceType = "Link"
            },
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "dev-app",
                Client_secret = "your-app-secret"
            }
        );

        var result = await _sender.SendAsync(command, ct);
        var response = result.Entity;
    }
}
```

#### 10.1 Response Model: CreateLinkProfileResponse
### CreateLinkProfileResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `LinkPubicId` | `string` | The public facing unique ID. |
| `LinkPrivateId` | `string` | The internal system ID. |
| `ConnectId` | `string` | Identifier for connection services. |
| `ConnectionId` | `int` | The primary ID of the established connection. |
| `Subject` | `SubjectResponse` | Created personal profile details. |
| `Contact` | `ContactResponse` | Registered contact information. |
| `Address` | `AddressResponse` | Registered address information. |
| `User` | `UserResponse` | User credentials and profile ID. |
| `Account` | `AccountResponse` | Detailed financial account balances. |
| `IntegrationType` | `string` | Assigned integration mode. |
| `Organizaion` | `int` | Internal organization ID. |
| `ClientResponse` | `ClientResponse` | API credentials for the new profile. |

### SubjectResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ProfileId` | `int` | The unique system ID for the profile. |
| `FirstName` | `string` | Registered first name. |
| `MiddleName` | `string` | Registered middle name. |
| `LastName` | `string` | Registered last name. |
| `CreationDate` | `DateTimeOffset`| Timestamp when the subject was created. |

### AccountResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `HolderId` | `int` | ID of the account holder. |
| `CurrencyId` | `int` | ID of the assigned currency. |
| `Balance` | `decimal` | Total account balance. |
| `WithdrawalAvailableBalance` | `decimal` | Balance available for withdrawal. |
| `WithdrawalOnHold` | `decimal` | Withdrawal amounts currently pending. |
| `DebitAllowanceBalance` | `decimal` | Allowed debit limit. |
| `DepositOnHold` | `decimal` | Pending deposit amounts. |
| `AccountNumber` | `string` | The generated system account number. |
| `Status` | `int` | Current status of the account. |
| `RowDateTime` | `DateTimeOffset`| Record timestamp. |

### ClientResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ClientId` | `string` | The generated OAuth Client ID. |
| `ClientSecret` | `string` | The generated OAuth Client Secret. |
| `GrantType` | `string` | Allowed grant types for these credentials. |
---

### 10.2 Create Batch Profiles
*   **Create Batch Profiles:** Allows for the bulk creation of multiple profiles in a single request, providing a summary of successes and detailed failure reports.

### 10.2 CreateBatchLinkProfilesWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `LinkProfiles` | `List<CreateLinkProfile>` | A list of profile objects (using the same structure as single creation). |
| `LoginRequest` | `LoginRequest` | Authentication credentials. |

#### 10.2 Example: Create Batch Profiles Execution

```csharp
public async Task RunCreateBatchProfilesAsync(CancellationToken ct = default)
{
    var command = new CreateBatchLinkProfilesWithLoginCommand(
        LinkProfiles: new List<CreateLinkProfile>
        {
            new CreateLinkProfile
            {
                Subject = new Subject { FirstName = "Waleed33", LastName = "Waleed332" },
                User = new User { UserName = "Waleed33kkk", Password = "123456" },
                ProfileType = "تاجر",
                ServiceType = "Link"
                // ... (Other properties as per single creation example)
            }
        },
        LoginRequest: new LoginRequest { /* ... */ }
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
}
```

#### 10.2 Response Model: CreateBatchLinkProfilesResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `TotalCount` | `int` | Total number of profiles processed. |
| `SuccessCount` | `int` | Number of profiles created successfully. |
| `FailureCount` | `int` | Number of profiles that failed creation. |
| `Failures` | `List<BatchFailureDetail>` | Detailed list of errors for failed profiles. |

#### Sub-Object: BatchFailureDetail
| Property | Type | Description |
| :--- | :--- | :--- |
| `AccountNumber` | `string` | The account number associated with the failure. |
| `FullName` | `string` | The name of the profile that failed. |
| `Message` | `string` | The error message explaining why it failed. |
| `ErrorCode` | `string` | System error code for the failure. |

---

### 10.3 Create Profile With Link Kyc
*   **Create Profile With Link Kyc:** Enables the creation of a secondary profile linked to an existing identity, requiring company-specific codes and profile type classification.

### 10.3 CreateProfileWithLinkKycWithLoginCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `ProfileId` | `string` | The unique identifier of the primary profile. |
| `CompanyCode` | `string` | The unique code representing the company (e.g., "fatora"). |
| `ProfileType` | `string` | The classification of the profile (e.g., "عميل"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the request. |

#### 10.3 Example: Create Profile With Link Kyc Execution

```csharp
public async Task RunCreateProfileWithLinkKycAsync(CancellationToken ct = default)
{
    var command = new CreateProfileWithLinkKycWithLoginCommand(
        ProfileId: "13229",
        CompanyCode: "fatora",
        ProfileType: "عميل",
        LoginRequest: new LoginRequest 
        { 
            UserName = "your_username", 
            Password = "your_password" 
        }
    );

    var result = await _sender.SendAsync(command, ct);
    
    if (result.Succeeded)
    {
        var response = result.Entity;
        // Access response.LinkPublicId, response.LinkPrivateId, etc.
    }
}
```

#### 10.3 Response Model: CreateProfileWithLinkKycResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `LinkPublicId` | `string` | The public-facing unique identifier for the created link. |
| `LinkPrivateId` | `string` | The internal/private unique identifier for the created link. |
| `Organization` | `int` | The ID of the organization the profile is associated with. |