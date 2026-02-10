# E2E Test Scenarios - State Context Initialization

**Feature**: 006-state-context-init  
**Date**: February 10, 2026  
**Status**: Ready for manual testing

## Prerequisites

1. Backend API running (`dotnet run --project src/MAA.API`)
2. Frontend dev server running (`npm run dev` in `frontend/`) OR frontend built and served
3. PostgreSQL database running with migrations applied
4. Test data seeded (state configurations, ZIP code mappings)

## Test Scenario 1: Valid ZIP Code Entry (User Story 1)

**Objective**: User enters valid ZIP code, state is auto-detected, navigates to wizard

### Steps

1. Navigate to `http://localhost:5173/state-context` (or appropriate URL)
2. Observe page loads with "Let's Get Started" heading
3. Enter ZIP code: **90210**
4. Click **Continue** button
5. Observe loading indicator
6. Observe state confirmation:
   - ZIP code displayed: "90210"
   - State detected: "California" (CA)
7. Click **Continue to Wizard** button
8. Verify navigation to `/wizard/step-1` (or appropriate next step)

### Expected Results

- ✓ ZIP code input accepts 5-digit numeric input
- ✓ No validation errors displayed
- ✓ State "California" detected correctly
- ✓ State configuration loaded (Medi-Cal program name may be visible)
- ✓ Navigation to wizard successful
- ✓ No console errors
- ✓ API response time < 1000ms (check browser DevTools Network tab)

---

## Test Scenario 2: State Override (User Story 2)

**Objective**: User overrides auto-detected state for edge cases

### Steps

1. Navigate to `http://localhost:5173/state-context`
2. Enter ZIP code: **10001** (New York City)
3. Click **Continue** button
4. Observe state confirmation:
   - ZIP code: "10001"
   - State detected: "New York" (NY)
5. Click **Change State** button (or equivalent override UI)
6. Select "New Jersey" (NJ) from dropdown
7. Click **Confirm** or **Apply** button
8. Observe updated state confirmation:
   - State updated to: "New Jersey" (NJ)
   - Manual override flag: true (may be indicated by UI)
9. Click **Continue to Wizard** button
10. Verify navigation to `/wizard/step-1`

### Expected Results

- ✓ Initial state "New York" detected correctly
- ✓ State override dropdown displays all 50 states + DC
- ✓ Dropdown is keyboard accessible (Tab, Arrow keys, Enter)
- ✓ State change persists after override
- ✓ No validation errors
- ✓ Navigation to wizard with overridden state
- ✓ No console errors

---

## Test Scenario 3: Invalid ZIP Code Handling (User Story 3)

**Objective**: User receives clear error messages for invalid ZIP codes

### Steps - Part A: Format Validation

1. Navigate to `http://localhost:5173/state-context`
2. Enter ZIP code: **1234** (only 4 digits)
3. Click **Continue** button
4. Observe validation error:
   - Error message: "Please enter a valid 5-digit ZIP code"
   - Input field has red border
   - Error displayed below input field
5. Clear input and enter: **ABCDE** (letters)
6. Observe same validation error
7. Clear input and enter: **90210** (valid ZIP)
8. Observe error clears immediately
9. Click **Continue**
10. Verify state "California" detected successfully

### Expected Results - Part A

- ✓ Invalid ZIP formats rejected before API call
- ✓ Error message is clear and actionable
- ✓ Error styling visible (red border, error text)
- ✓ Error announced by screen reader (check with NVDA/VoiceOver if available)
- ✓ Error clears when user corrects input
- ✓ Valid ZIP processes normally after correction

### Steps - Part B: ZIP Not Found

1. Navigate to `http://localhost:5173/state-context`
2. Enter ZIP code: **00000** (valid format but doesn't exist)
3. Click **Continue** button
4. Observe API error message:
   - Error message: "ZIP code not found. Please verify your ZIP code."
   - Toast notification or inline error displayed
5. Correct ZIP code to: **90210**
6. Click **Continue**
7. Verify state "California" detected successfully

### Expected Results - Part B

- ✓ API error caught and displayed to user
- ✓ Error message is user-friendly (not technical)
- ✓ User can retry with corrected ZIP
- ✓ No unhandled exceptions or console errors
- ✓ Valid ZIP processes normally after retry

---

## Accessibility Tests (T081-T083)

### T081: axe DevTools Scan

**Steps**:

1. Install axe DevTools Chrome/Firefox extension
2. Navigate to `http://localhost:5173/state-context`
3. Open browser DevTools → axe DevTools tab
4. Click **Scan All of My Page**
5. Review results

**Expected**: **Zero accessibility violations** (WCAG 2.1 AA)

### T082: Keyboard-Only Navigation

**Steps**:

1. Navigate to `/state-context` using keyboard only (no mouse)
2. Press **Tab** to focus ZIP code input
3. Type **90210**
4. Press **Enter** to submit form
5. Observe state confirmation
6. Press **Tab** to focus "Continue" button
7. Press **Enter** to navigate to wizard
8. If state override is available:
   - Press **Tab** to focus "Change State" button
   - Press **Enter** to open dropdown
   - Use **Arrow keys** to navigate state options
   - Press **Enter** to select state

**Expected**:

- ✓ All interactive elements reachable via Tab
- ✓ Visible focus indicators on all focusable elements
- ✓ Enter/Space activates buttons
- ✓ Escape closes modals/dropdowns (if applicable)
- ✓ No keyboard traps

### T083: Screen Reader Flow

**Tools**: NVDA (Windows), VoiceOver (macOS), or JAWS

**Steps**:

1. Start screen reader
2. Navigate to `/state-context`
3. Listen to page heading announcement: "Let's Get Started"
4. Tab to ZIP code input (should announce label: "ZIP Code")
5. Enter invalid ZIP (1234)
6. Submit form
7. Listen for error announcement (should announce error message automatically)
8. Correct ZIP to 90210
9. Submit form
10. Listen for state confirmation announcement
11. Navigate to "Continue" button and activate

**Expected**:

- ✓ Page structure is logical (headings, landmarks)
- ✓ Form labels properly associated with inputs
- ✓ Error messages announced via `aria-live="polite"` or `role="alert"`
- ✓ Success confirmations announced
- ✓ Button states and labels are descriptive

---

## Responsive Design Tests (T084-T086)

### T084: Mobile Layout (375px)

**Steps**:

1. Open browser DevTools (F12)
2. Enable Device Toolbar (Ctrl+Shift+M)
3. Select **iPhone SE** (375×667) or custom 375px width
4. Navigate to `/state-context`
5. Verify:
   - Form elements stack vertically
   - Text is readable (minimum 16px font size)
   - Touch targets ≥44×44px (buttons, inputs)
   - No horizontal scrolling
   - No content cut off
6. Complete ZIP code entry flow
7. Test state override UI (if applicable)

**Expected**: Usable on mobile without pinch/zoom

### T085: Tablet Layout (768px)

**Steps**:

1. Set device width to **768px** (iPad)
2. Verify:
   - Form adapts appropriately (not stretched excessively)
   - Spacing and padding comfortable for touch
   - All elements visible and accessible
3. Complete end-to-end flow

**Expected**: Optimized for tablet screen size

### T086: Desktop Layout (1920px)

**Steps**:

1. Set device width to **1920px** (desktop monitor)
2. Verify:
   - Form doesn't stretch to full width (max-width constrained)
   - Content centered and comfortable to read
   - No awkward spacing or gaps
3. Complete end-to-end flow

**Expected**: Clean desktop experience with proper max-width

---

## Performance Test (T080)

### API Endpoint Performance

**Objective**: Verify API response time <1000ms (p95)

**Steps**:

1. Open browser DevTools → Network tab
2. Navigate to `/state-context`
3. Enter ZIP code **90210** and submit
4. Observe Network tab:
   - Find `POST /api/state-context` request
   - Check response time in **Time** column
5. Repeat test 10 times with different ZIP codes:
   - 90210, 10001, 60601, 94102, 33101, 98101, 02101, 77001, 85001, 30301
6. Record response times
7. Calculate p95 (95th percentile)

**Expected**: p95 < 1000ms

**Actual Results** (to be filled after testing):

| ZIP   | Response Time (ms) |
| ----- | ------------------ |
| 90210 |                    |
| 10001 |                    |
| 60601 |                    |
| 94102 |                    |
| 33101 |                    |
| 98101 |                    |
| 02101 |                    |
| 77001 |                    |
| 85001 |                    |
| 30301 |                    |

**p95**: _____ ms (✓ PASS / ✗ FAIL)

---

## Quickstart Validation (T091)

**Objective**: Verify all steps in quickstart.md execute successfully

**Steps**:

1. Open [quickstart.md](./quickstart.md)
2. Follow all implementation steps from Phase 1 through Phase 6
3. Verify:
   - Backend builds without errors
   - Database migrations apply successfully
   - Frontend builds without errors
   - Dev servers start successfully
   - All API endpoints return expected responses
   - All UI components render correctly

**Expected**: All steps execute without errors

---

## Test Completion Checklist

- [ ] T091: Quickstart validation completed
- [ ] T092: E2E Test Scenario 1 (valid ZIP) ✓ PASS
- [ ] T093: E2E Test Scenario 2 (state override) ✓ PASS
- [ ] T094: E2E Test Scenario 3 (invalid ZIP) ✓ PASS
- [ ] T080: API performance test ✓ PASS (<1000ms p95)
- [ ] T081: axe DevTools scan ✓ PASS (zero violations)
- [ ] T082: Keyboard navigation ✓ PASS
- [ ] T083: Screen reader flow ✓ PASS
- [ ] T084: Mobile layout (375px) ✓ PASS
- [ ] T085: Tablet layout (768px) ✓ PASS
- [ ] T086: Desktop layout (1920px) ✓ PASS

---

## Notes for Manual Testers

- Test in multiple browsers: Chrome, Firefox, Edge, Safari (if available)
- Test with screen reader for accessibility verification
- Document any bugs or issues found
- Take screenshots of successful test completions
- Record timing data for performance tests

---

**Status**: Ready for manual QA testing  
**Last Updated**: February 10, 2026
