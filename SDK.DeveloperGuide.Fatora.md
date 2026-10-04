# Developer Guide: Fatora Services in MerchantSDK

Welcome to the **Fatora Services** integration guide. This SDK utilizes a unified command-based architecture to streamline financial operations like Deposits, Withdrawals, and Transfers ..etc.

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
 "FatoraUriSettings": {
    "BaseUrl": "http://localhost:50005/V1",
    "LoginUrl": "https://rts-gw-appdev:11022/authenticate/token"
  }
```

### 3. Registering the SDK
In your `Program.cs`, register the configuration using the Options pattern and initialize the SDK services.

```csharp

var builder = WebApplication.CreateBuilder(args);

// 1. Bind the settings section
var settings = builder.Configuration.GetSection("FatoraUriSettings");
builder.Services.AddOptions<FatoraUriSettings>().Bind(settings);

// 2. Register Unified link services
builder.Services.AddLinkServices();
```

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

### 1.1 Create Invoice
*   **Create Invoice:** This service initiates a payment request or invoice between a source account and a beneficiary. It requires Know Your Customer (KYC) details for both the sender and the receiver to comply with financial regulations and ensure secure processing.

### 1.1 CreateInvoiceCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string?` | Optional unique identifier for the request to prevent duplication. |
| `ReferenceNumber` | `string?` | Optional external reference number for tracking. |
| `CurrencyCode` | `string` | The ISO currency code (e.g., "YER", "USD"). |
| `Amount` | `decimal` | The total transaction amount. |
| `AmountType` | `int` | The identifier for the type of amount being processed. |
| `CaptureMode` | `string` | Defines how the payment is captured (e.g., "MANUAL", "AUTO"). |
| `IsBeneficiaryInitiated` | `bool` | Indicates if the transaction was started by the receiver. |
| `Source` | `LinkSourceRequest` | The account details of the person sending the funds. |
| `Beneficiary` | `LinkSourceRequest` | The account details of the person receiving the funds. |
| `SenderKYC` | `LinkCustomerKYC` | Comprehensive identity details of the sender. |
| `ReceiverKYC` | `LinkCustomerKYC` | Comprehensive identity details of the receiver. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `MetaData` | `LinkMetaData` | Optional collection of custom key-value pairs. |
| `Notes` | `string` | Optional descriptive notes or memo for the transaction. |

#### 1.1 Example: Create Invoice Execution

```csharp
public class InvoiceExample
{
    private readonly ISender _sender;

    public InvoiceExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunCreateInvoiceAsync(CancellationToken ct = default)
    {
        var command = new CreateInvoiceCommand(
            RequestId: null,
            ReferenceNumber: null,
            CurrencyCode: "YER",
            Amount: 1100m,
            AmountType: 1,
            CaptureMode: "MANUAL",
            IsBeneficiaryInitiated: false,

            Source: new LinkSourceRequest
            {
                OrganizationCode = "Link",
                AccountType = AccountType.ProfileId,
                AccountId = "1113"
            },

            Beneficiary: new LinkSourceRequest
            {
                OrganizationCode = "Easy",
                AccountType = AccountType.WalletId,
                AccountId = "778888855"
            },

            SenderKYC: new LinkCustomerKYC
            {
                FirstName = "Taha",
                SecondName = "Moahmmed",
                ThirdName = "Abdulkareem",
                FamilyName = "AL-Ghabri",
                MobileNumber = "779651512",
                IdType = "profileId",
                IdNumber = "73"
            },

            ReceiverKYC: new LinkCustomerKYC
            {
                FirstName = "badr",
                SecondName = "basem",
                ThirdName = "basem",
                FamilyName = "alzekri",
                MobileNumber = "779458265",
                IdType = "profileId",
                IdNumber = "74"
            },

            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-client-id",
                Client_secret = "your-client-secret"
            },
            MetaData: null,
            Notes: "Payment for clothing order"
        );

        var result = await _sender.SendAsync(command, ct);
        
        if (result.Success)
        {
            // Process the Invoice response
            var invoice = result.Entity;
            Console.WriteLine($"Invoice Created: {invoice.ReferenceNumber}");
        }
    }
}
```

#### 1.1 Response Model: CreateInvoiceResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | The unique request ID echoed back from the system. |
| `ReferenceNumber` | `string` | The generated transaction reference number. |
| `Amount` | `decimal` | The final amount processed. |
| `CurrencyCode` | `string` | The currency code used for the transaction. |
| `Fees` | `decimal` | The service fees applied to this transaction. |
| `Commission` | `decimal` | The commission earned/charged on the transaction. |
| `Source` | `LinkSourceRequest` | Echoes the source account details. |
| `Beneficiary` | `LinkSourceRequest` | Echoes the beneficiary account details. |
| `SenderKYC` | `LinkCustomerKYC` | Echoes the sender's KYC information. |
| `ReceiverKYC` | `LinkCustomerKYC` | Echoes the receiver's KYC information. |

### 2.1 Confirm Invoice
*   **Confirm Invoice:** This service is used to finalize and authorize a previously created invoice. It moves the transaction from a pending/draft state to a completed state, triggering the actual movement of funds and returning the final transaction details, including the updated account balance.

### 2.1 ConfirmInvoiceCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `RequestId` | `string` | Unique identifier for the confirmation request. |
| `ReferenceNumber` | `string` | The reference number of the invoice being confirmed (e.g., "INV..."). |
| `CurrencyCode` | `string` | The ISO currency code of the transaction. |
| `Amount` | `decimal` | The exact amount to be confirmed. |
| `OrganizationCode` | `string` | The code of the organization processing the confirmation (e.g., "Easy"). |
| `AuthorizationType` | `string` | The method of authorization used (e.g., "UNIFIED"). |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `MetaData` | `List<LinkMetaData>` | Optional list of custom metadata associated with the confirmation. |
| `Notes` | `string` | Optional descriptive notes regarding the confirmation. |

#### 2.1 Example: Confirm Invoice Execution

```csharp
public class InvoiceConfirmationExample
{
    private readonly ISender _sender;

    public InvoiceConfirmationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunConfirmInvoiceAsync(CancellationToken ct = default)
    {
        var command = new ConfirmInvoiceCommand(
            RequestId: "1258",
            ReferenceNumber: "INV2604228529",
            CurrencyCode: "YER",
            Amount: 1100m,
            OrganizationCode: "Easy",
            AuthorizationType: "UNIFIED",

            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-client-id",
                Client_secret = "your-client-secret"
            },
            MetaData: null,
            Notes: "confirm this invoice"
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"[SUCCESS] Transaction ID: {response.TransactionId}");
            Console.WriteLine($"New Balance: {response.Balance} {response.CurrencyCode}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 2.1 Response Model: ConfirmInvoiceResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Balance` | `decimal` | The remaining balance in the source account after the transaction. |
| `TransactionId` | `string` | The internal unique identifier for the completed transaction. |
| `TransactionStatus` | `int` | The status code of the transaction (e.g., 1 for Success). |
| `Amount` | `decimal` | The total amount that was confirmed/processed. |
| `CurrencyCode` | `string` | The currency code of the confirmed transaction. |
| `TransactionDate` | `DateTime` | The date and time when the transaction was finalized. |
| `ProviderReference` | `string` | The reference identifier provided by the external organization. |

### 3.1 Split Invoice Payment
*   **Split Invoice Payment:** This service allows an existing invoice to be divided among multiple participants. Based on a specific strategy (e.g., equal split, percentage-based, or fixed amounts), the system calculates the share for each participant and generates access tokens for them to complete their individual payments.

### 3.1 SplitInvoicePaymentCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `InvoiceReference` | `string` | The reference number of the original invoice to be split. |
| `Strategy` | `int` | The logic for splitting (e.g., 1 for Equal Split). |
| `SplitParticipants` | `List<SplitParticipantRequest>` | List of individuals participating in the payment. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |
| `ExpiresAt` | `string?` | Optional expiration timestamp for the split request. |
| `MetaData` | `List<LinkMetaData>` | Optional custom metadata for the split transaction. |

#### 3.1 Sub-Model: SplitParticipantRequest
| Property | Type | Description |
| :--- | :--- | :--- |
| `ParticipantPhoneNumber` | `string` | The phone number of the participant. |
| `ParticipantName` | `string` | The display name of the participant. |
| `StrategyValue` | `decimal?` | Specific value based on strategy (e.g., fixed amount or percentage). Null for equal split. |

#### 3.1 Example: Split Invoice Payment Execution

```csharp
public class SplitPaymentExample
{
    private readonly ISender _sender;

    public SplitPaymentExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunSplitInvoiceAsync(CancellationToken ct = default)
    {
        var command = new SplitInvoicePaymentCommand(
            InvoiceReference: "INV2604227508",
            Strategy: 1, // Equal Split
            ExpiresAt: null,
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-client-id",
                Client_secret = "your-client-secret"
            },
            SplitParticipants: new List<SplitParticipantRequest>
            {
                new SplitParticipantRequest(
                    ParticipantPhoneNumber: "775039696",
                    ParticipantName: "Moaid Manager",
                    StrategyValue: null
                ),
                new SplitParticipantRequest(
                    ParticipantPhoneNumber: "779651512",
                    ParticipantName: "Ahmed Jalal",
                    StrategyValue: null
                )
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"Total Amount: {response.TotalAmount} {response.CurrencyCode}");
        }
    }
}
```

#### 3.1 Response Model: SplitInvoicePaymentResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `TotalAmount` | `decimal` | The total amount of the original invoice. |
| `CurrencyCode` | `string` | The currency of the transaction. |
| `ParticipantsData` | `List<ParticipantDataResponse>` | Breakdown of shares and access details for each participant. |

#### 3.1 Sub-Model: ParticipantDataResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `ParticipantName` | `string` | Name of the participant. |
| `ParticipantPhoneNumber` | `string` | Phone number of the participant. |
| `ParticipantShare` | `decimal` | The specific amount this participant is required to pay. |
| `AccessToken` | `string` | Secure token used by the participant to authorize their share. |

### 4.1 Authorize Participant Split Payment
*   **Authorize Participant Split Payment:** This service is used by an individual participant to authorize their specific portion of a split invoice. By providing the unique payment token generated during the split process, the participant receives a transaction URL or reference to complete their specific payment share.

### 4.1 AuthorizeParticipantSplitPaymentCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `PaymentToken` | `string` | The unique secure token assigned to the participant during the split invoice process. |
| `LoginRequest` | `LoginRequest` | Authentication credentials for the service provider. |

#### 4.1 Example: Authorize Participant Split Payment Execution

```csharp
public class ParticipantAuthorizationExample
{
    private readonly ISender _sender;

    public ParticipantAuthorizationExample(ISender sender)
    {
        _sender = sender;
    }

    public async Task RunAuthorizeParticipantPaymentAsync(CancellationToken ct = default)
    {
        var command = new AuthorizeParticipantSplitPaymentCommand(
            PaymentToken: "2604223543",
            LoginRequest: new LoginRequest
            {
                Grant_type = "client_credentials",
                Client_id = "your-client-id",
                Client_secret = "your-client-secret"
            }
        );

        var result = await _sender.SendAsync(command, ct);

        if (result.Success)
        {
            var response = result.Entity;
            Console.WriteLine($"Invoice Reference: {response.InvoiceReference}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {result.Message}");
        }
    }
}
```

#### 4.1 Response Model: AuthorizeParticipantSplitPaymentResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `InvoiceReference` | `string` | The reference number of the parent invoice. |
| `PaymentReference` | `string` | The specific reference number for this participant's transaction. |
| `TotalAmountDue` | `decimal` | The specific amount this participant is required to pay. |
| `CurrencyCode` | `string` | The currency code for the payment. |
| `TransactionURL` | `string` | The redirect URL where the participant can complete the transaction. |


## Shared Registration Sub-Models
These models are utilized by both the Merchant and Client registration services.

| Model | Property | Type | Description |
| :--- | :--- | :--- | :--- |
| **RegistrationSubject** | `FirstName` | `string` | First name of the registrant. |
| | `MiddleName` | `string` | Middle name/Father's name. |
| | `LastName` | `string` | Family or Last name. |
| | `ProfileId` | `int?` | Optional existing profile ID. |
| **RegistrationAddress** | `Country` | `string` | Country of residence/operation. |
| | `Details` | `string` | Full address details. |
| | `Location` | `string` | GPS Coordinates (e.g., "37.21122,-120.5456"). |
| **RegistrationContact** | `PhoneNumber` | `string` | Primary contact phone number. |
| | `Email` | `string` | Primary contact email address. |
| **RegistrationUser** | `UserName` | `string` | Desired username for login. |
| | `Password` | `string` | Secure password for the account. |
| | `Email/Phone` | `string` | User-specific contact info. |
| **RegistrationAcquirer**| `ExternalAccountType`| `int` | Category of the bank/external account. |
| | `BankCode` | `int` | Code of the financial institution. |
| | `AccountNumber` | `string` | The actual bank account number. |
| | `CurrencyCode` | `string` | Currency of the settled account. |

---

### 5.1 Register Fatora Merchant
*   **Register Fatora Merchant:** This service is used to onboard a new primary merchant into the system. It establishes the merchant's profile, contact information, physical address, and the settlement bank account (Acquirer) where funds will be deposited. Upon success, it generates API credentials for the merchant.

### 5.1 RegisterFatoraMerchantCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `CategoryType` | `int` | Business category identifier. |
| `Subject` | `RegistrationSubject` | Personal or business name details. |
| `Acquirer` | `RegistrationAcquirer` | Settlement bank account information. |
| `IntegrationType` | `string` | Type of integration (e.g., "API"). |
| `ServiceType` | `string` | Type of service (e.g., "Wallets"). |
| `ProfileType` | `string` | Description of the profile (e.g., "Merchant"). |
| `RegistrationType` | `int` | Type of registration flow. |
| `Address` | `RegistrationAddress` | Physical location details. |
| `Contact` | `RegistrationContact` | Contact information. |
| `User` | `RegistrationUser` | Login credentials for the merchant dashboard. |
| `LoginRequest` | `LoginRequest` | Admin credentials to authorize the registration. |

#### 5.1 Example: Merchant Registration Execution

```csharp
public async Task CreateMerchantExample(ISender sender)
{
    var command = new RegisterFatoraMerchantCommand(
        CategoryType: 2,
        Subject: new RegistrationSubject("Rami", "Moqbel", "Al-Ghamdi"),
        Acquirer: new RegistrationAcquirer(5, 1, "", "", "", "YER"),
        IntegrationType: "API",
        ServiceType: "Wallets",
        ProfileType: "Merchant",
        RegistrationType: 1,
        Address: new RegistrationAddress("Yemen", "Sanaa Al-Tahrir", "37.21122,-120.5456"),
        Contact: new RegistrationContact("777499849", "merchant@example.com"),
        User: new RegistrationUser("merchant@example.com", "777499849", "rami_m", "password123"),
        LoginRequest: new LoginRequest { /* admin credentials */ }
    );

    var result = await sender.SendAsync(command);
    if (result.Success)
    {
        Console.WriteLine($"Merchant Registered with ID: {result.Entity.MerchantId}");
    }
}
```

---

### 6.1 Register Fatora Client
*   **Register Fatora Client:** This service registers a sub-entity or a "Client" under an existing Merchant. While it shares the same structure as the Merchant registration, it requires a `MerchantId` to establish the parent-child relationship in the system hierarchy.

### 6.1 RegisterFatoraClientCommand
| Property | Type | Description |
| :--- | :--- | :--- |
| `MerchantId` | `int` | **Required.** The ID of the parent Merchant. |
| `CategoryType` | `int` | Business category identifier. |
| `Subject` | `RegistrationSubject` | Client name details. |
| `Acquirer` | `RegistrationAcquirer` | Client settlement account details. |
| `IntegrationType` | `string` | Type of integration (e.g., "API"). |
| `Address` | `RegistrationAddress` | Client physical location. |
| `User` | `RegistrationUser` | Login credentials for the client. |
| `LoginRequest` | `LoginRequest` | Credentials to authorize the registration. |

#### 6.1 Example: Client Registration Execution

```csharp
public async Task CreateClientExample(ISender sender)
{
    var command = new RegisterFatoraClientCommand(
        MerchantId: 175, // Link to existing Merchant
        CategoryType: 2,
        Subject: new RegistrationSubject("Client_Name", "Middle", "Last"),
        Acquirer: new RegistrationAcquirer(5, 1, "", "", "", "YER"),
        IntegrationType: "API",
        ServiceType: "Wallets",
        ProfileType: "Client",
        RegistrationType: 1,
        Address: new RegistrationAddress("Yemen", "Sanaa", "37.21122,-120.5456"),
        Contact: new RegistrationContact("777123456", "client@example.com"),
        User: new RegistrationUser("client@example.com", "777123456", "client_user", "pass123"),
        LoginRequest: new LoginRequest { /* admin credentials */ }
    );

    var result = await sender.SendAsync(command);
    if (result.Success)
    {
        Console.WriteLine($"Client Registered. ID: {result.Entity.MerchantId}");
    }
}
```

---

### Response Model: UserRegistrationResponse (Common)
This response is returned for both Merchant and Client registrations.

| Property | Type | Description |
| :--- | :--- | :--- |
| `MerchantId` | `int` | The unique ID assigned to the new entity. |
| `LinkPublicId` | `string` | Public API identifier. |
| `LinkPrivateId` | `string` | Private API identifier. |
| `Account` | `RegistrationAccountResponse` | Details of the created financial account (Balance, Account No). |
| `ClientResponse` | `ClientCredentialsResponse` | OAuth2 credentials (`Client_Id`, `Client_Secret`) for API access. |
| `Subject` | `RegistrationSubject` | Echoed subject details. |
| `IntegrationType`| `string` | The integration mode assigned. |

#### Sub-Model: RegistrationAccountResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `HolderId` | `int` | Internal ID of the account holder. |
| `Balance` | `decimal` | Initial account balance (usually 0). |
| `AccountNumber` | `string` | The generated internal account number. |
| `Status` | `int` | Account status code. |

#### Sub-Model: ClientCredentialsResponse
| Property | Type | Description |
| :--- | :--- | :--- |
| `Client_Id` | `string` | The unique ID for generating Auth tokens. |
| `Client_Secret` | `string` | The secret key (keep this secure). |
| `Grant_Type` | `string` | Usually "client_credentials". |
