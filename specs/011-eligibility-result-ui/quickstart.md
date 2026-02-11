# Phase 1 Quickstart - Eligibility Result UI

## Prerequisites

- Node.js 22 LTS
- .NET 10 SDK

## Start the backend API

From the repository root:

1. `cd src/MAA.API`
2. `dotnet run`

## Start the frontend

From the repository root:

1. `cd frontend`
2. `npm install`
3. `npm run dev`

## View the results UI

Navigate to the eligibility results route in the frontend.
Ensure the UI displays:

- Eligibility status
- Program matches
- Explanation bullets
- Confidence indicator

## Results Route & Implementation Details

### Route

- **URL**: `/results`
- **Prerequisites**: Complete wizard or have valid session context
- **Auto-redirect**: Wizard automatically navigates to results on completion

### Results Page Sections

1. **Eligibility Status Card** — Overall status badge, explanation, confidence level with timestamp
2. **Matched Programs Card** — List of eligible programs with confidence scores, matching/disqualifying factors
3. **Confidence Indicator** — Visual gauge (0-100%) showing evaluation confidence with interpretation
4. **Explanation Bullets** — Key factors influencing the determination with icons

### Technical Integration

- Maps wizard session answers via `eligibilityInputMapper.ts`
- Calls `POST /api/rules/evaluate` with bearer token
- Uses React Query v5 for caching and loading/error states
- Displays `EligibilityResultDto` response mapped to `EligibilityResultView`

### Testing the Feature

1. Start backend: `cd src/MAA.API && dotnet run`
2. Start frontend: `cd frontend && npm run dev`
3. Navigate to http://localhost:5173
4. Complete wizard questions for your state
5. Submit → redirected to `/results` with eligibility evaluation

### WCAG 2.1 AA Compliance

- Semantic HTML structure with proper headings
- ARIA labels on status badges and interactive elements
- Color not sole indicator (status shown with icon + text + label)
- Keyboard navigation supported
- Screen reader friendly labels
