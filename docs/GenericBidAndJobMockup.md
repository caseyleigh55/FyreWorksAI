# Generic Bid And Job Mockup

This is a safe reference example for the FyreWorksAI workflow.

It does not change the live workspace data file.

Use it side by side with the Bids and Jobs pages to see what belongs where and how the totals relate to each other.

## Quick Goal

This mockup shows:

- A simple bid with 3 components, 1 wire line, and 1 material line
- A converted job from that bid
- Two vendor invoices
- One approved change order
- Time entries for actual labor
- Actual device costs linked to invoices
- No commitments in the main example, so it stays easy to follow

## Mock Bid

### Header

- Bid Number: `BID-26-9001`
- Project Name: `Generic Fire Alarm Upgrade`
- Client: `Demo Client`
- Status: `Accepted`
- Accepted Sale Price: `$1,638.00`

### Project Contacts

- Site Name: `Generic Warehouse`
- Address: `100 Sample Ave`
- City / State / Zip: `Sacramento, CA 95814`
- Occupancy: `Warehouse`

### Exclusions + Proposal

- Exclusions: `Permit fees, monitoring activation, and patch/paint by others.`
- Proposal Summary: `Provide and install three generic fire alarm devices, related wire, and supporting material.`
- Proposal Closing: `Work to be scheduled during normal business hours unless otherwise noted.`

### Bid Scope

#### Administrative Tasks

| Item | Hours | Cost | Sale |
| --- | ---: | ---: | ---: |
| Project Setup | 1.00 | $68.00 | $95.00 |

#### Engineering Tasks

| Item | Hours | Cost | Sale |
| --- | ---: | ---: | ---: |
| Shop Drawings | 2.00 | $192.00 | $260.00 |

#### Components

| Item | Qty | Unit Cost | Unit Sale | Ext. Cost | Ext. Sale |
| --- | ---: | ---: | ---: | ---: | ---: |
| Component 1 | 1 | $120.00 | $168.00 | $120.00 | $168.00 |
| Component 2 | 1 | $150.00 | $210.00 | $150.00 | $210.00 |
| Component 3 | 1 | $200.00 | $280.00 | $200.00 | $280.00 |

#### Materials

| Item | Qty | Unit Cost | Unit Sale | Ext. Cost | Ext. Sale |
| --- | ---: | ---: | ---: | ---: | ---: |
| Wire | 500 ft | $0.40 | $0.56 | $200.00 | $280.00 |
| Material | 1 lot | $150.00 | $210.00 | $150.00 | $210.00 |

### Bid Baseline Summary

| Category | Cost | Sale |
| --- | ---: | ---: |
| Admin | $68.00 | $95.00 |
| Engineering | $192.00 | $260.00 |
| Materials / Wire / Components | $820.00 | $1,148.00 |
| Install | $48.75 | $67.50 |
| Trim | $32.50 | $45.00 |
| Test | $16.25 | $22.50 |
| Total | $1,177.50 | $1,638.00 |

### Bid Labor Distribution Assumption

This simple example assumes the devices create these base field labor hours:

- Install: `0.75`
- Trim: `0.50`
- Test: `0.25`

That is why the baseline contains labor sale and cost for those three field phases.

## Mock Job

### Header

- Job Number: `JOB-26-9001`
- Project Name: `Generic Fire Alarm Upgrade`
- Source Bid: `BID-26-9001`
- Status: `Closeout`
- Active: `Yes`

### Baseline Reference

The job baseline carries over from the bid.

| Baseline Item | Value |
| --- | ---: |
| Original Revenue | $1,638.00 |
| Estimated Cost | $1,177.50 |
| Estimated Base Hours | 4.50 |

Base hours in this mockup:

- Admin: `1.00`
- Engineering: `2.00`
- Install: `0.75`
- Trim: `0.50`
- Test: `0.25`

### Schedule Of Values

One approved change order is added later, so the full job contract becomes:

- Base Contract: `$1,638.00`
- Approved Change Order Revenue: `$280.00`
- Total Job Revenue: `$1,918.00`

| SOV Line | Scheduled Value | % Of Contract | Billed | Paid |
| --- | ---: | ---: | ---: | ---: |
| Admin | $95.00 | 4.95% | $95.00 | $95.00 |
| Engineering | $260.00 | 13.56% | $260.00 | $260.00 |
| Materials | $1,148.00 | 59.85% | $900.00 | $900.00 |
| Install | $67.50 | 3.52% | $67.50 | $30.00 |
| Trim | $45.00 | 2.35% | $0.00 | $0.00 |
| Test | $22.50 | 1.17% | $0.00 | $0.00 |
| CO - Added Device | $280.00 | 14.60% | $140.00 | $0.00 |
| Total | $1,918.00 | 100.00% | $1,462.50 | $1,285.00 |

Resulting billing view:

- Remaining To Bill: `$455.50`
- Billed Revenue To Date: `$1,462.50`
- Collected Revenue To Date: `$1,285.00`

### Invoices

These are vendor invoices, not customer billings.

They help organize documents and connect actual item costs, but the invoice total itself does not directly increase job actual cost.

| Invoice Ref | Vendor | Invoice # | Invoice Total | Purpose |
| --- | --- | --- | ---: | --- |
| INV-001 | Generic Supply House | GS-1001 | $790.00 | Base scope parts and material |
| INV-002 | Generic Supply House | GS-1002 | $105.00 | Change order device |

### Bid Devices

These are the original bid-derived item lines now holding actual material cost and invoice references.

| Item | Qty | Actual Unit Cost | Actual Cost | Invoice Ref |
| --- | ---: | ---: | ---: | --- |
| Component 1 | 1 | $110.00 | $110.00 | INV-001 |
| Component 2 | 1 | $160.00 | $160.00 | INV-001 |
| Component 3 | 1 | $190.00 | $190.00 | INV-001 |
| Wire | 500 ft | $0.42 | $210.00 | INV-001 |
| Material | 1 lot | $120.00 | $120.00 | INV-001 |
| Total |  |  | $790.00 |  |

This is the key relationship:

- `INV-001` total is `$790.00`
- The linked bid-device actual costs also total `$790.00`
- The invoice record helps document the purchase
- The actual device costs are what hit `Actual Material Cost`

That means this does not double-count.

### Job Devices

No separate job devices are used in this mockup.

This section would only be used if you bought something that was not part of the original bid and not part of a change order device list.

### Change Orders

One approved change order is added to show how it affects the job.

#### Change Order Header

- Title: `Added Device`
- Approved: `Yes`
- Revenue Amount: `$280.00`
- Additional Estimated Cost: `$20.00`
- Estimated Labor Hours: `1.00`
- Estimated Labor Rate: `$65.00`

Estimated change-order cost impact:

- Additional Estimated Cost: `$20.00`
- Estimated Labor: `$65.00`
- Estimated Device Cost: `$85.00`
- Estimated Cost Impact Total: `$170.00`

#### Change Order Device

| Item | Qty | Est. Unit Cost | Est. Unit Sale | Actual Unit Cost | Invoice Ref |
| --- | ---: | ---: | ---: | ---: | --- |
| CO Device 1 | 1 | $85.00 | $120.00 | $90.00 | INV-002 |

This change order affects the job in three places:

- It increases job revenue by `$280.00`
- It increases estimated job cost by `$170.00`
- It creates its own SOV line: `CO - Added Device`

### Time Entries

These are the actual labor entries on the Jobs page.

| Date | Phase | Hours | Rate | Cost |
| --- | --- | ---: | ---: | ---: |
| 2026-03-03 | Admin | 0.50 | $68.00 | $34.00 |
| 2026-03-04 | Engineering | 1.00 | $96.00 | $96.00 |
| 2026-03-05 | Install | 1.00 | $65.00 | $65.00 |
| 2026-03-06 | Trim | 0.75 | $65.00 | $48.75 |
| 2026-03-07 | Test | 0.50 | $65.00 | $32.50 |
| 2026-03-08 | Change Order / Added Device | 1.00 | $65.00 | $65.00 |
| Total |  | 4.75 |  | $341.25 |

### Commitments

No commitments are used in this main example.

That is intentional so the invoice-to-device flow stays easy to see.

If you wanted to add something like a lift rental or a subcontract programmer, that would be a good commitment example because it is a vendor obligation but not a device/material line.

## Final Job Rollup

### Estimated Side

| Metric | Value |
| --- | ---: |
| Base Estimated Cost | $1,177.50 |
| Approved CO Estimated Cost | $170.00 |
| Total Estimated Cost | $1,347.50 |
| Base Revenue | $1,638.00 |
| Approved CO Revenue | $280.00 |
| Total Revenue | $1,918.00 |

### Actual Side

| Metric | Value |
| --- | ---: |
| Actual Labor Cost | $341.25 |
| Actual Material Cost From Base Scope | $790.00 |
| Actual Material Cost From CO Device | $90.00 |
| Actual Material Cost Total | $880.00 |
| Billed Commitments | $0.00 |
| Actual Job Cost | $1,221.25 |
| Profit | $696.75 |

Approximate margin on this mockup:

- `36.33%`

## How The Sections Mesh Together

### Customer-Side Revenue Tracking

- Bid accepted sale becomes the job baseline original revenue
- Approved change-order revenue increases total job revenue
- Schedule Of Values tracks what you bill and collect from the customer

### Vendor-Side Cost Tracking

- Invoices are vendor bill records and document buckets
- Bid devices, job devices, and change-order devices hold the actual item costs
- Those item-level actual costs are what feed actual material cost

### Why Invoices Do Not Double Count

In this app:

- The invoice total is a reference amount
- The linked device actual costs are the cost amounts that count

So if you enter:

- `INV-001 = $790.00`
- And the linked device actual costs also total `$790.00`

The job sees one material-cost stream, not two.

### Where Double Counting Can Happen

Double counting happens if the same vendor cost is entered in two different cost paths, for example:

- You enter actual device costs for parts on an invoice
- Then you also enter that same exact vendor bill as a billed commitment

That would count twice.

## Simple Reading Order In The App

If you want to follow this mockup inside the app, read the pages in this order:

1. Bid header and scope items
2. Job header and baseline reference
3. Schedule Of Values
4. Invoices
5. Bid Devices actual cost and invoice refs
6. Change Order and its device line
7. Time Entries
8. Commitments only if there is a subcontractor, rental, or other non-device vendor obligation

## Optional Commitment Example

If you want a simple commitment example to picture later, use this:

- Commitment Number: `JOB-26-9001-COM-001`
- Vendor: `Generic Lift Rental`
- Description: `2-day scissor lift rental`
- Billed Amount: `$350.00`
- Paid Amount: `$350.00`

Use that when the cost is real but not something you want sitting on a device/material line.
