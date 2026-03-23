# Dashboard and Bids Notes

## Dashboard

### Office Command Center header bubble
- Maybe not so big?
- Header smaller and single line or perhaps centered
- Lose the "Open Bids", "Open Jobs" and "Open Service" buttons within the command center

### Action Queue bubble
- Change orange "Action Queue" label to "Bids Queue"
- Swap the sizes of "action queue" and "bids due soon"
- Make the listed bids selectable to navigate to the bid editor page with the selected bid loaded
- Make "go to bids" button look like the "Open Bids" button we removed from the office command center
- Due dates shouldn't include the time, just the date it's due

### Project Cost Control bubble
- Change orange "Project Cost Control" label to "Jobs Queue"
- Swap the sizes of "Project Cost Control" (now "Jobs Queue") and "Active Jobs"
- Make the listed jobs selectable to navigate to the job editor page with the selected job loaded
- Make "go to jobs" button look like the "Open Jobs" button we removed from the office command center

### Recurring Revenue bubble
- Change orange "Recurring Revenue" label to "Service Queue"
- Change "Upcoming Inspections and Billing" to "Upcoming Service and Billing"
- Swap the sizes of "Recurring Revenue" and "Upcoming Inspections and Billing" (now "Upcoming Service and Billing")
- Make the listed service items selectable to navigate to the service editor page with the selected service loaded
- Make "go to service" button look like the "Open Service" button we removed from the office command center

### Storage Profile bubble
- Change the "Storage Profile" label to "Template Profiles"
- Change the "Open settings" button text to "Open Templates"
- Change "TextFile" label to "Default Templates"
- Swap sizes of Template Profiles and Default Template text
- List within bubble all of the default templates (will change templates and how they work in the templates page to accommodate this list)

## Bids

### Estimating
- Should not display the time of the due date, just the date itself

### Bid Editor
- Bid number format should be initially set by a named template set to default within the templates page. It should still be able to be edited within the bid editor independently. The app default format is `BID-YY-NNNN` (`BID-26-0001`). `NNNN` (`0001`) is the current bid number for the year 2026.
- Shall keep Bid Number, Project Name, Status, Active Checkbox, Created, and Due Date. Client selection and creation shall be included in this section. This section shall be called **Bid Info**.
- The field hours, estimated cost, suggested sell, and margin sections will be converted / merged / replaced by the **Totals Overview** section.
  - Display bid estimated totals and an adjustable totals section.
  - For the bid, show total cost, total sale, profit, and margin %.
  - Duplicate this for the adjusted totals, but make the sale price editable and have it update the profit and margin % accordingly.
  - This is for lowering or raising the price while remaining competitive in the market.
  - The left side shows what the bid actually comes out to; the right side shows the accepted sale price for the bid upon creating a job from it.
- Still inside the Totals Overview, display total job hours by hour type and personnel type:
  - Journeyman and apprentice personnel
  - Regular hours and overnight hours
- Labor rates and global markup will be set by a single template. The default rates will be set on the templates page and the defaults will be loaded into the labor rates section, editable, and able to be saved as new templates from the bid editor page.
- Global markup will be placed before the tasks and materials section, loaded with a default value but editable.
- Scope summary will be its own section named **Site Info** with text fields for:
  - Scope of work
  - Address
  - Parcel number
  - Jurisdiction
  - Building area
  - Number of stories
  - Occupancy group
  - Occupant load
  - Construction type
  - Yes/No selector for **Sprinklered**
- Site notes and estimator notes can be combined into a single **Notes** section and moved to the bottom.
- Administrative and engineering tasks shall include:
  - Task name
  - Cost price
  - Sale price
  - Maybe toggle by hour or set price to calculate the price
  - Sale price is cost + markup
  - Below the task section will be a cost and sale total, or both task types as one total
- Components and material / wire will need to be updated to match the new Location and Install Time Matrix. Updated rule line:
  - `Location profile | normal - lift - panel - pipe | install - demo - trim - test | notes`
  - Headers should be delineated by a bar between types like shown above.
- Components will update to include the added fields from the revised template, and `mat lea` will change to **Unit Cost** and add a **Unit Sale**.
- Add a totals section for the components that shows total **Cost** and **Sale** for the components.
- For the material / wire section, make them separate:
  - One for wire
  - One for material
  - Add a Unit Sale per line for both
  - Add a totals section for both
- All dropdowns have hard-to-see text against the cream background, and only when highlighted can you clearly read the text. The dropdown listed item text color should be black at all times, not just when hovered over, if possible.
