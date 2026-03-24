# FyreWorksAI Project Notes

## solution
the solution should be located within the root folder and all projects in a folder labeled src. this is the core foundation.

## C# code creation and refactoring
C# code shall follow the SOLID principles for both creation and refactoring
The acronym SOLID stands for:
S - Single Responsibility Principle (SRP): A class should have only one reason to change, focusing on a single, well-defined job.
O - Open/Closed Principle (OCP): Software entities should be open for extension but closed for modification, often achieved through interfaces.
L - Liskov Substitution Principle (LSP): Derived classes must be substitutable for their base classes without altering program correctness.
I - Interface Segregation Principle (ISP): Clients should not depend on interfaces they do not use, favoring smaller, specific interfaces over large ones.
D - Dependency Inversion Principle (DIP): High-level modules should depend on abstractions, not low-level modules, enhancing flexibility.

file structures must be maintained and namespaces properly attributed, if a file would be placed in a folder within a project, the namespace should reflect the folder it is in and not just be included within the projects namespace.

all names used for files, methods, properties, folders etc. shall be explicitly clear as to their intent so as to be understood at a glance what is intended/happening

xaml comments are required and should contain a brief description about the methods, classes, interfaces etc. that the comments are referencing

headers are to be used to delineate different sections and the code for those sections should be consolidated as able into the section it pertains to.
headers will be in this format
//******************************//
//********** Header ************//
//******************************//

sections are file specific so if for instance a page has totals, items, invoices, attachments etc. and the flow is top down with totals at the top of the page and attachments at the bottom, each section will have a header and contain the pertinent code relating to that section.

classes, interfaces, helpers, extensions etc. are to be their own files in related folders with proper explicit naming and namespace attributions in accordance to the SOLID principles.


## Overview

FyreWorksAI is a commercial fire alarm operations app for managing the full office workflow around:

- Bids / estimating
- Jobs / cost tracking
- Service agreements
- Templates / estimating standards
- Settings / storage and defaults

The app is built as a .NET MAUI Blazor workspace with a shared UI and logic layer plus a web host. It is currently Windows-first, with future expansion paths for web, mobile, and richer storage backends.

## Current Architecture

- Shared UI and business logic live under `FyreWorksAI.App/FyreWorksAI.Shared`
- MAUI host lives under `FyreWorksAI.App/FyreWorksAI`
- Web host lives under `FyreWorksAI.App/FyreWorksAI.Web`
- Core data models are in `FyreWorksAI.App/FyreWorksAI.Shared/Core/AppModels.cs`
- Core app services and calculations are in:
  - `FyreWorksAI.App/FyreWorksAI.Shared/Core/AppServices.cs`
  - `FyreWorksAI.App/FyreWorksAI.Shared/Core/JobFinancialHelpers.cs`
- Main workflow pages are:
  - `Pages/Bids.razor`
  - `Pages/Jobs.razor`
  - `Pages/Service.razor`
  - `Pages/Templates.razor`
  - `Pages/Settings.razor`

## App-Wide UX Direction

These are recurring preferences that should be treated as the default product direction unless explicitly changed:

- Maximize usable horizontal space. Avoid wasting width with large fixed side panels.
- The Operations Hub should live as a thin sticky bar across the top, with nav links in a row.
- Left-side project-control lists should collapse into a small flag/drawer instead of permanently consuming layout width.
- Sections should be collapsible where it helps keep dense workflows readable.
- Individual records inside sections should also collapse where appropriate.
- Notes should stay compact by default and expand when focused/opened.
- Important references should cross-link between related records when possible.
- The UI should feel like an office operations workspace, not a generic dashboard.

## Numbering + Navigation Rules

- Bid numbers use the bid format system and default to `BID-YY-NNNN`
- Job numbers should use `JOB-YY-NNNN`
- Legacy long-form job numbers are normalized into `JOB-YY-NNNN`
- Jobs converted from bids should retain a reference back to the source bid
- Bids converted into jobs should expose a job-reference navigation action

## Jobs + Financial Concepts

### Baseline / Revenue

- A job created from a bid should use the bid's accepted sale value as the contract revenue
- If the accepted sale is untouched, it is effectively the same as the calculated sale and should still transfer cleanly
- Baseline category sale references still come from the underlying bid category sales
- Admin and Engineering should remain separate
- Materials should only represent:
  - Components
  - Wire
  - Material

### Schedule of Values

The current direction for SOV behavior is:

- SOV lives under Baseline Reference on the Jobs page
- Base SOV categories should be:
  - Admin
  - Engineering
  - Materials
  - Install
  - Demo
  - Trim
  - Test
  - Change Order
  - Other
- Each SOV line should carry a bid sale reference value
- `% of Contract` is a real allocation control and should total to 100% unless intentionally overridden
- Parent SOV billed/paid values should come from child progress sublines
- The old standalone editable amount column is being phased out in favor of:
  - Bid sale reference
  - Percentage of contract
  - Derived scheduled value behind the scenes
- Change orders should generate their own SOV lines and increase total contract value without being merged into the original base SOV buckets
- Footer totals in the SOV should appear in-column for:
  - Bid Sale Ref
  - % of Contract
  - Billed
  - Paid
  - % of SOV Paid

### SOV Sublines

- Each SOV parent line can have multiple child progress entries
- This supports partial billing and partial payment against the same parent SOV category
- Parent billed/paid totals should roll up from those child entries
- SOV child entries should be collapsible with the parent line
- When a commitment is linked to an SOV line, a related SOV child row should be able to appear automatically with relevant context

### Commitments

- Commitments should link to SOV parent lines
- If linked, committed value should reflect the scheduled value of that SOV line
- If not linked, commitment amount can default to `0`
- Commitment behavior should stay interconnected with the SOV
- Linked-commitment visibility should be expressed in a way that helps compare commitment payment progress against the SOV line

### Change Orders

- Change orders should be collapsible by item and also within the full section
- Attachments should be supported per change order
- Approved change orders should automatically create / maintain corresponding SOV lines
- Change-order reference sale should auto-populate from the approved revenue amount

### Actual Cost Tracking

- Actual labor should be collapsible as a section
- Bid-derived baseline line items should support actual cost tracking and invoice linkage
- Invoice references should be able to carry attachments
- Material purchase tracking should stay invoice-specific
- The long-term intent is stronger linkage between:
  - Baseline reference items
  - Actual material purchasing
  - Invoice documents
  - Reference IDs

## Current Implemented Product Memory

These are important decisions already reflected in the app:

- Top-bar Operations Hub layout
- Collapsible left flag/drawer project-control panels
- Cross-links between bids and jobs
- Collapsible sections across the Jobs workflow
- Compact-expand notes behavior
- Collapsible invoices and change orders
- Change-order attachments
- Job baseline revenue sourced from accepted bid sale
- SOV line reference values and child progress entries
- SOV totals footer
- Job number normalization to `JOB-YY-NNNN`

## Remembered Backlog / Direction From `Jobs.md`

These are not necessarily all finished, but they are intentional remembered requests and should stay visible:

- Bid address data should mirror the jobs address structure and transfer cleanly into jobs
- Add an Exclusions section after Site Info on bids
- Add a Proposal section after Exclusions on bids
- Generate a proposal document from bid information for client-facing use
- Show specified hours, used hours, and remaining hours by labor type
- Keep refining best-practice financial workflow around SOV + commitments + partial billing
- Keep change orders separate from original SOV values while still affecting total job revenue

## Editing Guidance For Future Work

- Preserve the current dark operations-workspace visual language
- Prefer compact layouts over oversized cards or wide empty gutters
- When changing financial logic, keep the data relationships explicit:
  - Bid accepted sale -> Job contract revenue
  - Bid category sales -> SOV reference values
  - SOV child entries -> Parent billed/paid rollups
  - SOV links -> Commitment alignment
- When possible, distinguish:
  - Implemented business rules
  - User-preference memory
  - Open product decisions

## Maintenance Note

This file is intended as a living memory document for future agent work on the project. When new app-wide rules are added, update this file instead of leaving the preference only in chat history.
