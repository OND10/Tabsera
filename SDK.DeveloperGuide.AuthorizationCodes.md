# Developer Guide: Authorization codes Services in MerchantSDK

Welcome to the **Authorization Codes Services** integration guide. This SDK utilizes a unified command-based architecture to streamline financial operations like Deposits, Withdrawals, and Transfers ..etc.

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
"AuthorizationCodesUriSettings": {
    "BaseUrl": "http://localhost:5021/api/v1/authorization-codes"
  }
```

### 3. Registering the SDK
In your `Program.cs`, register the configuration using the Options pattern and initialize the SDK services.

```csharp

var builder = WebApplication.CreateBuilder(args);

// 1. Bind the settings section
var settings = builder.Configuration.GetSection("AuthorizationCodesUriSettings");
builder.Services.AddOptions<AuthorizationCodesUriSettings>().Bind(settings);

// 2. Register Unified link services
builder.Services.AddLinkServices();
```

---

## 1. Authorization codes services

### Common Enums

The following enums are used to define the behavior, permissions, and lifecycle of authorization codes within the system.

### ConsumptionType
Defines the rules for how the balance is deducted from the authorization code.

| Value | Name | Description |
| :--- | :--- | :--- |
| `1` | `Limit` | Acts as a spending ceiling. Allows for multiple partial redemptions until the balance reaches zero. |
| `2` | `Exact` | Intended for a single transaction. Consumes the entire balance in one use or requires an exact amount match. |

### RedemptionChannel (Flags)
A bitwise flag combination used to specify which channels are permitted to redeem the code. Multiple channels can be combined using bitwise OR.

| Value | Name | Description |
| :--- | :--- | :--- |
| `1` | `Telecom` | Redemption via telecommunication services (IVR, SMS, etc.). |
| `2` | `ATM` | Redemption via Automated Teller Machines. |
| `4` | `Agent` | Redemption through authorized human agents or representatives. |
| `8` | `InStore` | Redemption at physical brick-and-mortar retail locations. |
| `16` | `ECommerce` | Redemption via online platforms and digital marketplaces. |
| `31` | `All` | Composite flag representing permission across all available channels. |

### AuthorizationCodeStatus
Represents the various stages in the life-cycle of an authorization code.

| Value | Name | Description |
| :--- | :--- | :--- |
| `1` | `Pending` | Initial state. The code is created but not yet ready for use. |
| `2` | `Active` | The code is live, has a positive balance, is not expired, and is available for redemption. |
| `3` | `Redeemed` | The balance has reached zero. The code is fully utilized and can no longer be used. |
| `4` | `Expired` | The validity period has passed. The code is no longer usable regardless of balance. |
| `5` | `Revoked` | Manually stopped by an admin or fraud system. Associated funds are typically frozen. |
| `6` | `Refunded` | The remaining balance was withdrawn and returned to the original creator. |

---

### 1.1 Create Authorization Code
*   **Create Authorization Code:** This service initiates the generation of a new authorization code aggregate. It defines the monetary value, consumption rules, expiration, and technical formatting for the code to be generated.

### 1.1 CreateAuthorizationCodeCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `Amount` | `decimal` | The initial monetary value to be loaded onto the code. |
| `Currency` | `string` | The 3-letter ISO currency code (e.g., "USD", "YER"). |
| `ConsumptionType` | `ConsumptionType` | Defines rules for balance deduction (Limit, Exact). |
| `FundingAccountId` | `string` | The unique identifier of the wallet or account funding the code. |
| `RequestorId` | `string` | The identifier of the client system originating this request. |
| `ExpiresAt` | `DateTime` | The UTC timestamp after which the code becomes invalid. |
| `Channels` | `RedemptionChannel` | A bitwise flag combination of allowed channel options (Enum). |
| `Options` | `AuthCodeOptions` | Technical configuration for the code string generation. |

**AuthCodeOptions Properties:**
| Property | Type | Description |
| :--- | :--- | :--- |
| `Length` | `int` | Total characters in the generated code (Default: 16). |
| `Prefix` | `string` | Optional prefix to prepend to the code (e.g., "X-"). |
| `UseGrouping` | `bool` | Whether to group characters for readability (e.g., "XXXX-XXXX"). |
| `IncludeUppercase` | `bool` | Include uppercase letters (A-Z). |
| `IncludeLowercase` | `bool` | Include lowercase letters (a-z). |
| `IncludeDigits` | `bool` | Include numeric digits (0-9). |
| `IncludeSpecialChars`| `bool` | Include special characters (@, #, $, etc). |
| `CustomCharacters` | `string` | Custom character pool to be used for generation. |

#### 1.1 Example: Create Authorization Code Execution

```csharp
public class AuthorizationCodeExample
{
    private readonly ISender _sender;

    public AuthorizationCodeExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunCreateAuthorizationCodeAsync(CancellationToken ct = default)
    {
        var command = new CreateAuthorizationCodeCommand(
            Amount: 1000m,
            Currency: "YER",
            ConsumptionType: ConsumptionType.Limit,
            FundingAccountId: "ali6",
            RequestorId: "ali6",
            ExpiresAt: DateTime.UtcNow.AddDays(7),
            Channels: RedemptionChannel.Agent,
            Options: new AuthCodeOptions
            {
                Length = 16,
                Prefix = "X",
                UseGrouping = true,
                IncludeUppercase = true,
                IncludeLowercase = true,
                IncludeDigits = true,
                IncludeSpecialChars = true,
                CustomCharacters = ""
            }
        );

        var result = await _sender.SendAsync(command, ct);
        var response = result.Entity;
    }
}
```

#### 1.1 Response Model: CreateAuthorizationCodeResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The generated unique authorization code string. |
| `ExpiresAt` | `DateTime` | The expiration timestamp of the created code. |
---


### 1.2 Add Beneficiary
*   **Add Beneficiary:** Adds a specific Beneficiary to the white list of an authorization code, restricting use to the specified recipient.

### 1.2 AddBeneficiaryCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code to be updated. |
| `Identifier` | `string` | The unique business identifier for the Beneficiary (e.g., Beneficiary Id). |

#### 1.2 Example: Add Beneficiary Execution
```csharp
public async Task RunAddBeneficiaryAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new AddBeneficiaryCommand(
        AuthCode: "X,HVM-I-At-|^", 
        Identifier: "123456"
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.2 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful (e.g., `true`). |
| `ResponseCode` | `string` | The API response code (e.g., `"2000"`). |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.3 Remove Beneficiary
*   **Remove Beneficiary:** Removes a specific Beneficiary from the white list of an authorization code.

### 1.3 RemoveBeneficiaryCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code to be updated. |
| `Identifier` | `string` | The unique business identifier for the Beneficiary (e.g., Beneficiary Id). |

#### 1.3 Example: Remove Beneficiary Execution
```csharp
public async Task RunRemoveBeneficiaryAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new RemoveBeneficiaryCommand(
        AuthCode: "X,HVM-I-At-|^", 
        Identifier: "123456"
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.3 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful. |
| `ResponseCode` | `string` | The API response code. |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.4 Add Acquirer
*   **Add Acquirer:** Adds a specific Acquirer to the white list of an authorization code, restricting where the code can be processed.

### 1.4 AddAcquirerCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code to be updated. |
| `Identifier` | `string` | The unique business identifier for the Acquirer (e.g., Acquirer Id). |

#### 1.4 Example: Add Acquirer Execution
```csharp
public async Task RunAddAcquirerAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new AddAcquirerCommand(
        AuthCode: "X,HVM-I-At-|^", 
        Identifier: "123456"
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.4 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful. |
| `ResponseCode` | `string` | The API response code. |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.5 Remove Acquirer
*   **Remove Acquirer:** Removes a specific Acquirer from the white list of an authorization code.

### 1.5 RemoveAcquirerCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code to be updated. |
| `Identifier` | `string` | The unique business identifier for the Acquirer (e.g., Acquirer Id). |

#### 1.5 Example: Remove Acquirer Execution
```csharp
public async Task RunRemoveAcquirerAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new RemoveAcquirerCommand(
        AuthCode: "X,HVM-I-At-|^", 
        Identifier: "123456"
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.5 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful. |
| `ResponseCode` | `string` | The API response code. |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.6 Remove Authorization Code
*   **Remove Authorization Code:** Manually removes or deactivates a specific authorization code from the system.

### 1.6 RemoveAuthorizationCodeCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code to be removed. |

#### 1.6 Example: Remove Authorization Code Execution
```csharp
public async Task RunRemoveAuthorizationCodeAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new RemoveAuthorizationCodeCommand(
        AuthCode: "YV{p?-R|YW-Xb"
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.6 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful. |
| `ResponseCode` | `string` | The API response code. |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.7 Add Transaction Type
*   **Add Transaction Type:** Restricts the authorization code to be used only for specific transaction types by adding them to the white list.

### 1.7 AddTransactionTypeCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code to be updated. |
| `Identifier` | `string` | The unique business identifier for the Transaction Type. |

#### 1.7 Example: Add Transaction Type Execution
```csharp
public async Task RunAddTransactionTypeAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new AddTransactionTypeCommand(
        AuthCode: "X,HVM-I-At-|^", 
        Identifier: "123456"
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.7 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful. |
| `ResponseCode` | `string` | The API response code. |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.8 Remove Transaction Type
*   **Remove Transaction Type:** Removes a specific transaction type from the white list of an authorization code.

### 1.8 RemoveTransactionTypeCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code to be updated. |
| `Identifier` | `string` | The unique business identifier for the Transaction Type. |

#### 1.8 Example: Remove Transaction Type Execution
```csharp
public async Task RunRemoveTransactionTypeAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new RemoveTransactionTypeCommand(
        AuthCode: "X,HVM-I-At-|^", 
        Identifier: "123456"
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.8 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful. |
| `ResponseCode` | `string` | The API response code. |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.9 Redeem Authorization Code
*   **Redeem Authorization Code:** Executes a redemption against the authorization code balance. It validates the channel, amount, and identifiers before deducting the funds.

### 1.9 RedeemAuthorizationCodeCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The authorization code string to redeem from. |
| `AcceptorIdentifier` | `string?` | Optional identifier for the acceptor (Acquirer). |
| `BeneficiaryIdentifier` | `string?` | Optional identifier for the beneficiary. |
| `TransactionTypeIdentifier` | `string?` | Optional identifier for the transaction type. |
| `Channels` | `RedemptionChannel` | The channel used for redemption (Enum). |
| `Amount` | `decimal` | The amount to be deducted from the code. |
| `Currency` | `string` | The currency of the redemption amount. |

#### 1.9 Example: Redeem Authorization Code Execution
```csharp
public async Task RunRedeemAuthorizationCodeAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new RedeemAuthorizationCodeCommand(
        AuthCode: "X%NA1-kX#+-jl",
        AcceptorIdentifier: "123456",
        BeneficiaryIdentifier: "12345",
        TransactionTypeIdentifier: "ali",
        Channels: RedemptionChannel.Telecom,
        Amount: 1000m,
        Currency: "YER"
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
}
```

#### 1.9 Response Model: RedeemAuthorizationCodeResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RemainingAmount` | `decimal` | The remaining monetary balance of the code after redemption. |

---

### 1.10 Reschedule Code
*   **Reschedule Code:** Updates the expiration date of an existing authorization code to extend or shorten its validity period.

### 1.10 RescheduleCodeCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique code string to be rescheduled. |
| `NewExpirationDate` | `DateTime` | The new UTC timestamp for code expiration. |

#### 1.10 Example: Reschedule Code Execution
```csharp
public async Task RunRescheduleCodeAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new RescheduleCodeCommand(
        AuthCode: "Yn^Mp-(SER-SZ",
        NewExpirationDate: DateTime.UtcNow.AddMinutes(10)
    );

    var result = await _sender.SendAsync(command, ct);
    var response = result.Entity;
}
```

#### 1.10 Response Model: RescheduleCodeResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ExpiresAt` | `DateTime` | The updated expiration timestamp. |

---

### 1.11 Change Consumption Type
*   **Change Consumption Type:** Updates the consumption logic (e.g., Limit vs. Exact) for a specific authorization code. This is only permitted if the code status has not been updated or the code has not yet been redeemed.

### 1.11 ChangeConsumptionTypeCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code. |
| `ConsumptionType` | `ConsumptionType` | The new consumption rule to apply (Enum). |

#### 1.11 Example: Change Consumption Type Execution
```csharp
public async Task RunChangeConsumptionTypeAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new ChangeConsumptionTypeCommand(
        AuthCode: "Yv-QK-7=t=-<L", 
        ConsumptionType: ConsumptionType.Limit
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.11 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful. |
| `ResponseCode` | `string` | The API response code (e.g., `"2000"`). |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.12 Change Redemption Channel
*   **Change Redemption Channel:** Updates the allowed channels (e.g., ATM, Agent, ECommerce) for a specific authorization code. Like consumption type changes, this is only allowed if the code remains in a valid, unredeemed state.

### 1.12 ChangeRedemptionChannelCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code. |
| `RedemptionChannel` | `RedemptionChannel` | The new bitwise flag combination of allowed channels (Enum). |

#### 1.12 Example: Change Redemption Channel Execution
```csharp
public async Task RunChangeRedemptionChannelAsync(ISender _sender, CancellationToken ct = default)
{
    var command = new ChangeRedemptionChannelCommand(
        AuthCode: "Yv-QK-7=t=-<L", 
        RedemptionChannel: RedemptionChannel.ECommerce
    );

    var result = await _sender.SendAsync(command, ct);
}
```

#### 1.12 Response Model: ServiceResult
| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | Indicates if the operation was successful. |
| `ResponseCode` | `string` | The API response code (e.g., `"2000"`). |
| `Message` | `string` | Descriptive success or error message. |

---

### 1.13 Get All Authorization Codes
*   **Get All Authorization Codes:** Retrieves a paginated collection of authorization codes based on specified filtering criteria such as status, participants, or date ranges.

### 1.13 GetAllAuthorizationCodesQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `Filter` | `CodeRetrievalOptions` | The set of parameters used to narrow down the search results. |

**CodeRetrievalOptions Properties:**
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string?` | Filter by a specific authorization code string. |
| `FundingAccountId` | `string?` | The identifier of the wallet or account that funded the codes. |
| `RequestorId` | `string?` | The identifier of the client system that requested the code. |
| `Beneficiary` | `string?` | Filter for codes that specifically allow this beneficiary. |
| `Acceptor` | `string?` | Filter for codes that specifically allow this acceptor (merchant). |
| `Status` | `AuthorizationCodeStatus?` | Filter by lifecycle status (Enum). |
| `CreatedFrom` | `DateTime?` | Filter for codes created after this UTC date. |
| `CreatedTo` | `DateTime?` | Filter for codes created before this UTC date. |
| `PageNumber` | `int` | The page number to retrieve (Default: 1). |
| `PageSize` | `int` | The number of items per page (Default: 10). |

#### 1.13 Example: Get All Codes Execution
```csharp
public async Task RunGetAllAuthorizationCodesAsync(ISender _sender, CancellationToken ct = default)
{
    var query = new GetAllAuthorizationCodesQuery(
        Filter: new CodeRetrievalOptions
        {
            PageNumber = 1,
            PageSize = 5,
            Status = AuthorizationCodeStatus.Active
        }
    );

    var result = await _sender.SendAsync(query, ct);
    var pagedResult = result.Entity;
}
```

#### 1.13 Response Model: PagedResult\<GetAllAuthorizationCodesResponse\>
**Pagination Metadata:**
| Property | Type | Description |
| :--- | :--- | :--- |
| `Page` | `int` | The current page number. |
| `PageSize` | `int` | Number of items per page. |
| `TotalRecords` | `long` | Total number of items across all pages. |
| `TotalPages` | `int` | Calculated total number of pages. |
| `HasNextPage` | `bool` | Indicates if a following page exists. |
| `HasPreviousPage` | `bool` | Indicates if a preceding page exists. |

**Item Properties (GetAllAuthorizationCodesResponse):**
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The unique string representation of the code. |
| `Status` | `string` | Current lifecycle status (e.g., "Active"). |
| `Amount` | `decimal` | Current available monetary value. |
| `Currency` | `string` | The 3-letter ISO currency code. |
| `AllowedBeneficiaries` | `List<string>` | List of permitted customer identifiers. |
| `AllowedAqcuirers` | `List<string>` | List of permitted merchant/location identifiers. |
| `AllowedTransactionTypes`| `List<string>` | Specific transaction categories allowed. |
| `ExpiresAt` | `DateTime` | The UTC expiration timestamp. |
| `ConsumptionType` | `string` | Logic for fund deduction (e.g., "Limit"). |
| `AllowedChannels` | `string` | Permitted digital/physical channels. |

---

### 1.14 Get Authorization Code By Code
*   **Get Authorization Code By Code:** Retrieves the complete details and current state of a specific authorization code using its unique string identifier.

### 1.14 GetByCodeQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string identifier (the actual code) used to locate the record. |

#### 1.14 Example: Get By Code Execution
```csharp
public async Task RunGetByCodeAsync(ISender _sender, CancellationToken ct = default)
{
    var query = new GetByCodeQuery("Yv-QK-7=t=-<L");

    var result = await _sender.SendAsync(query, ct);
    var response = result.Entity;
}
```

#### 1.14 Response Model: GetByCodeResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Code` | `string` | The human-readable authorization code string. |
| `Status` | `string` | Current lifecycle state (e.g., "Active"). |
| `Amount` | `decimal` | Current available monetary balance. |
| `Currency` | `string` | ISO currency code associated with the balance. |
| `AllowedBeneficiaries` | `IReadOnlyCollection<string>`| Customer IDs permitted to use this code. |
| `AllowedAqcuirers` | `IReadOnlyCollection<string>`| Merchant IDs where this code is valid. |
| `AllowedTransactionTypes`| `IReadOnlyCollection<string>`| Specific types of transactions allowed. |
| `ExpiresAt` | `DateTime` | UTC timestamp after which the code is invalid. |
| `ConsumptionType` | `string` | Logic applied to redemptions (e.g., "Single-use"). |
| `AllowedChannels` | `string` | Permitted redemption channel flags. |

---


### 1.15 Check Code Usability
*   **Check Code Usability:** A query to determine if an authorization code is currently valid and usable based on its lifecycle status and expiration, without considering specific transaction context.

### 1.15 CheckCodeUsabilityQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique code string to check. |

#### 1.15 Example: Check Code Usability Execution
```csharp
public async Task RunCheckCodeUsabilityAsync(ISender _sender, CancellationToken ct = default)
{
    var query = new CheckCodeUsabilityQuery("Yv-QK-7=t=-<L");

    var result = await _sender.SendAsync(query, ct);
    var response = result.Entity.IsUsable;
}
```

#### 1.15 Response Model: CheckCodeUsabilityResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `IsUsable` | `bool` | Returns `true` if the code can be used; otherwise, `false`. |

---

### 1.16 Check Code Validity Usage
*   **Check Code Validity Usage:** Verifies if an authorization code is valid for use within a specific transactional context, including the participant (Beneficiary/Acceptor), the transaction type, and the redemption channel.

### 1.16 CheckCodeValidityUsageQuery
| Property | Type | Description |
| :--- | :--- | :--- |
| `AuthCode` | `string` | The unique string representation of the authorization code. |
| `Beneficiary` | `string?` | Optional identifier of the intended recipient or customer. |
| `Acceptor` | `string?` | Optional identifier of the merchant or entity accepting the code. |
| `TransactionType`| `string?` | The classification of the transaction being attempted. |
| `Channel` | `RedemptionChannel` | The redemption channel through which validation is requested (Enum). |

#### 1.16 Example: Check Code Validity Usage Execution
```csharp
public async Task RunCheckCodeValidityUsageAsync(ISender _sender, CancellationToken ct = default)
{
    var query = new CheckCodeValidityUsageQuery(
        AuthCode: "Yv-QK-7=t=-<L",
        Beneficiary: "12345",
        Acceptor: "123456",
        TransactionType: null,
        Channel: RedemptionChannel.Telecom
    );

    var result = await _sender.SendAsync(query, ct);
    var response = result.Entity.IsValid;
    
}
```

#### 1.16 Response Model: CheckCodeValidityUsageResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `IsValid` | `bool` | Returns `true` if the code is valid for the provided context; otherwise, `false`. |

---

---
*Prepared by Link Team, Made with Love.*