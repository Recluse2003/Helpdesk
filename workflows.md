# Helpdesk Workflow
This document defines the expected workflows of this platform, making it easier to plan out required features. 

## Main workflow
A customer submits a ticket, and a support agent attempts to resolve it. 

### Diagram
```mermaid
flowchart TD
    A[Customer signs in] --> B[Create ticket]
    B --> C{Input valid?}
    C -- No --> D["Show validation<br>errors"]
    D --> B
    C -- Yes --> E[Ticket created]
    F["Agent views<br>support queue"] --> G["Agent opens and<br>takes ownership<br>of ticket"]
    E --> F
    G --> H["Agent investigates<br>issue"]
    H --> I{"Can agent<br>resolve it?"}

    I -- Yes --> J["Agent works<br>with customer"]
    J --> K{Issue resolved?}
    K -- No --> J
    K -- Yes --> L["Agent resolves<br>ticket"]

    I -- No --> M["Agent escalates<br>ticket"]
    M --> N["Ticket reassigned<br>to qualified<br>agent"]
    N --> O["New agent<br>reviews ticket"]
    O --> P["New agent<br>investigates<br>and works<br>with customer"]
    P --> Q{Issue resolved?}
    Q -- No --> R{"Requires further<br>escalation?"}
    R -- Yes --> M
    R -- No --> P
    Q -- Yes --> L

    L --> S["Customer views<br>resolution"]
```
