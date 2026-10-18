# Helpdesk Workflow
This document defines the expected workflows of this platform, making it easier to plan out required features. 

## Main workflow
A customer submits a ticket, and a support agent attempts to resolve it. 

### Diagram
```mermaid
flowchart TD
    A[Customer signs in] --> B[Create ticket]
    B --> C{Input valid?}
    C -- No --> D[Show validation errors]
    D --> B
    C -- Yes --> E[Ticket created]
    E --> F[Agent views support queue]
    F --> G[Agent opens and takes ownership of ticket]
    G --> H[Agent investigates issue]
    H --> I{Can agent resolve it?}

    I -- Yes --> J[Agent works with customer]
    J --> K{Issue resolved?}
    K -- No --> J
    K -- Yes --> L[Agent resolves ticket]

    I -- No --> M[Agent escalates ticket]
    M --> N[Ticket reassigned to qualified agent]
    N --> O[New agent reviews ticket]
    O --> P[New agent investigates and works with customer]
    P --> Q{Issue resolved?}
    Q -- No --> R{Requires further escalation?}
    R -- Yes --> M
    R -- No --> P
    Q -- Yes --> L

    L --> S[Customer views resolution]
```
