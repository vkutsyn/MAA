# Frontend

## Setup

- Install dependencies: `npm install`
- Start dev server: `npm run dev`
- Run tests: `npm test`
- Build: `npm run build`

## Dynamic Questions

The wizard supports conditional questions that appear or disappear based on user answers.

### Components

- `ConditionalQuestionContainer`: Wraps questions with conditional logic
- `QuestionTooltip`: Displays "Why we ask this" help text
- `conditionEvaluator.ts`: Pure functions for condition evaluation

### Usage

Questions with a `conditions` property only display when all conditions pass.
