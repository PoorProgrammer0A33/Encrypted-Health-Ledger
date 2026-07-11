# Encrypted Health Ledger

A privacy-preserving statistics service that computes aggregate health data (sums, averages) directly on encrypted values using homomorphic encryption — the server never sees plaintext patient data. Every operation is logged to a tamper-evident Oracle blockchain table so the process is independently auditable, without ever exposing what was computed.

## Why this exists

Most "privacy" systems protect data at rest and in transit, but decrypt it the moment computation happens. This project explores a different guarantee: computation *on* encrypted data, where the server can produce correct aggregate statistics without ever holding a decryptable copy of the underlying values.

It pairs that with a second, complementary guarantee: every submission and computation is logged to an append-only, cryptographically hash-chained table, so anyone can verify that the reported process actually happened as claimed — without that log ever containing the sensitive data itself.

## Architecture

- **EHL.Crypto** — a C# wrapper around Microsoft SEAL implementing CKKS-scheme homomorphic encryption: encrypt, add, multiply, multiply-by-constant (used for division), and decrypt.
- **EHL.Client** — a console app representing the clinic. Generates and holds its own CKKS key pair, encrypts values locally before submission, and is the only component that ever decrypts data.
- **EHL.Api** — an ASP.NET Core Web API exposing `POST /submit` and `GET /average`. Operates entirely on serialized ciphertext bytes — performs homomorphic addition and constant multiplication without ever possessing a public or secret key.
- **EHL.Ledger** — a thin data-access layer writing audit events to an Oracle 26ai Free blockchain table.
- **Oracle blockchain table** — an append-only, SHA2-512 hash-chained table logging every submission and computation as an event type + a hash of the ciphertext involved. No plaintext or ciphertext values are ever stored in the ledger — only proof that an event occurred.

## Key concepts

**CKKS vs BFV.** CKKS supports approximate arithmetic on real numbers (used here for averages of values like cholesterol readings). BFV supports exact arithmetic on integers. This project uses CKKS throughout since health statistics are naturally real-valued.

**Noise budget.** Every homomorphic operation introduces a small amount of numerical error ("noise") into the ciphertext. Addition adds very little; multiplication adds substantially more — in testing, a single ciphertext-by-ciphertext multiplication introduced roughly 70x more drift than an addition. Once accumulated noise exceeds the ciphertext's budget, decryption fails or returns garbage. This project stays within a shallow computation depth (a handful of additions plus one constant multiplication for averaging) specifically to avoid needing bootstrapping, a much more expensive technique for refreshing noise budget mid-computation.

**Relinearization.** Multiplying two ciphertexts grows their internal size; relinearization (using a separate `RelinKeys` key) compresses the result back down after each multiply. This is a size/performance cost, distinct from and unrelated to noise growth.

**Division by a public constant.** CKKS has no native division operator. Averaging is implemented as multiplying the encrypted sum by the plaintext constant `1/n`, since `n` (the count of submissions) is public information, not sensitive data.

**Blockchain table, precisely.** Oracle's blockchain table is a centralized, tamper-evident, insert-only ledger — rows are hash-chained so any modification breaks verification. It is not a decentralized network with consensus across untrusted parties; the guarantee here is tamper-evidence within a trusted database, not decentralization.

**Server-blind computation.** The server never holds an encryption key of any kind — only the shared, non-secret CKKS parameters (polynomial modulus degree, coefficient modulus chain) needed to interpret ciphertext structure. It can add and scale ciphertexts, but cannot decrypt them under any circumstance. Only the client, which generated the key pair, can decrypt a result.

## Setup

**Prerequisites:** .NET 8 SDK, Oracle 26ai Free (or any Oracle instance with blockchain table support), a JDK if using SQL Developer/SQLcl for manual queries.

1. **Create the Oracle schema and table:**
    ```sql
    CREATE USER ehl_user IDENTIFIED BY "YourStrongPassword";
    GRANT CONNECT, RESOURCE, DBA TO ehl_user;
    ALTER USER ehl_user QUOTA UNLIMITED ON USERS;
    ```
    Connect as `ehl_user`, then:
    ```sql
    CREATE BLOCKCHAIN TABLE ehl_audit_log (
        event_id        RAW(16) DEFAULT SYS_GUID() PRIMARY KEY,
        event_type      VARCHAR2(30) NOT NULL,
        ciphertext_hash VARCHAR2(64) NOT NULL,
        event_timestamp TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
    )
    NO DROP UNTIL 0 DAYS IDLE
    NO DELETE LOCKED
    HASHING USING SHA2_512 VERSION V1;
    ```

2. **Configure the connection string** in `EHL.Api/appsettings.json`:
    ```json
    {
      "ConnectionStrings": {
        "OracleLedger": "User Id=ehl_user;Password=YOUR_PASSWORD_HERE;Data Source=localhost:1521/freepdb1;"
      }
    }
    ```

3. **Restore and run:**
    ```
    dotnet build
    dotnet run --project EHL.Api
    ```
    Open the Swagger UI at the URL shown in the console output.

## API reference

**`POST /api/HealthStats/submit`**
```json
{ "value": 42.5 }
```
Encrypts the value and stores it. Logs a `SUBMIT` event to the ledger.

Response:
```json
{ "message": "Submitted", "totalCount": 1 }
```

**`GET /api/HealthStats/average`**

Computes the homomorphic average of all submitted values, decrypts only the final result, and logs a `COMPUTE_AVERAGE` event to the ledger.

Response:
```json
{ "average": 36.19999999947569, "basedOnCount": 4 }
```

## Verifying the ledger

```sql
DECLARE
  verified_rows NUMBER;
BEGIN
  DBMS_BLOCKCHAIN_TABLE.VERIFY_ROWS(
    schema_name             => 'EHL_USER',
    table_name              => 'EHL_AUDIT_LOG',
    number_of_rows_verified => verified_rows
  );
  DBMS_OUTPUT.PUT_LINE('Verified rows: ' || verified_rows);
END;
/
```
No error and a row count matching your total events confirms the chain is intact.

## Known limitations

- **Server-side encryption (v1 simplification).** The API currently encrypts values it receives as plaintext, meaning plaintext briefly exists in server memory/request bodies. A fully client-side-encrypted flow (where the client encrypts before sending) would close this gap and is a natural next step.
- **In-memory ciphertext storage.** Submitted ciphertexts are not persisted; restarting the API loses them. The audit log persists independently in Oracle.
- **Single-tenant.** No authentication or per-clinic isolation yet — all submissions are pooled into one running average.
- **Development connection string.** The Oracle password is stored in plaintext in `appsettings.json`. For anything beyond local development, this should move to environment variables, user secrets, or a secrets manager.

## What this project actually demonstrates

Homomorphic encryption (CKKS via Microsoft SEAL) and Oracle blockchain tables solve two different, complementary problems: computing on data without ever seeing it, and proving what happened without exposing what it was. Together they sketch a pattern for privacy-preserving systems in domains — healthcare, finance, compliance — where both confidentiality and auditability matter simultaneously.
