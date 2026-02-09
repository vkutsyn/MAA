/**
 * Phase 10: Performance & Load Testing - Rules Engine Load Test
 * 
 * Load test for POST /api/rules/evaluate endpoint
 * Target: 1,000 concurrent users over 5 minutes
 * Success Criteria: p95 latency ≤ 2 seconds, 0% error rate
 * 
 * Run with: k6 run rules-load-test.js
 * 
 * Prerequisites:
 * - k6 installed (https://k6.io/docs/getting-started/installation/)
 * - Rules Engine API running on http://localhost:5000
 * - Database seeded with test data
 */

import http from 'k6/http';
import { check, group } from 'k6';
import { Rate, Trend, Counter, Gauge } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const requestDuration = new Trend('request_duration');
const requestCount = new Counter('requests');
const activeConnections = new Gauge('active_connections');

// Configuration
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const RAMP_UP_DURATION = '30s';
const SUSTAIN_DURATION = '5m';
const TARGET_USERS = 1000;

// Test data
const STATES = ['IL', 'CA', 'NY', 'TX', 'FL'];
const HOUSEHOLD_SIZES = [1, 2, 3, 4, 5, 6, 7, 8];

// Monthly income thresholds (in cents) for testing various scenarios
const INCOME_LEVELS = [
  100_000,      // $1,000/month (low income)
  200_000,      // $2,000/month (typical)
  350_000,      // $3,500/month (near threshold)
  500_000,      // $5,000/month (high income)
  750_000,      // $7,500/month (well above threshold)
];

/**
 * Random integer between min and max (inclusive)
 */
function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

/**
 * Random element from array
 */
function randomElement(array) {
  return array[Math.floor(Math.random() * array.length)];
}

/**
 * Generate random eligibility input
 */
function generateEligibilityInput() {
  return {
    stateCode: randomElement(STATES),
    householdSize: randomElement(HOUSEHOLD_SIZES),
    monthlyIncomeCents: randomElement(INCOME_LEVELS),
    age: randomInt(18, 85),
    hasDisability: Math.random() < 0.15,  // 15% have disability
    isPregnant: Math.random() < 0.08,     // 8% are pregnant
    receivesSsi: Math.random() < 0.05,    // 5% receive SSI
    isCitizen: Math.random() < 0.95,      // 95% are citizens
    assetsCents: randomInt(0, 500_000)    // $0-$5,000 in assets
  };
}

/**
 * Test configuration with ramp-up and sustained load
 */
export const options = {
  stages: [
    // Ramp-up phase: 0 to 1,000 users over 30 seconds
    { duration: RAMP_UP_DURATION, target: TARGET_USERS },
    // Sustained load: 1,000 users for 5 minutes
    { duration: SUSTAIN_DURATION, target: TARGET_USERS },
    // Cool down: 1,000 to 0 users over 30 seconds
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    // SLO: 95th percentile latency ≤ 2 seconds (2000ms)
    'request_duration{group:::evaluate}': ['p(95) < 2000', 'p(99) < 3000', 'p(50) < 1000'],
    // SLO: Error rate = 0%
    'errors': ['rate == 0'],
    // SLO: 99th percentile latency < 3 seconds
    'http_req_duration': ['p(99) < 3000'],
  },
  // Connection settings
  vus: 1,
  duration: '6m', // Total duration including ramp-up, sustain, and cool-down
  // Increase timeout to allow for longer evaluations
  timeout: '10s',
};

/**
 * Setup function: runs before load test
 */
export function setup() {
  console.log('Starting load test configuration:');
  console.log(`  Base URL: ${BASE_URL}`);
  console.log(`  Ramp-up: 0 to ${TARGET_USERS} users over ${RAMP_UP_DURATION}`);
  console.log(`  Sustain: ${TARGET_USERS} users for ${SUSTAIN_DURATION}`);
  console.log(`  Cool-down: 30 seconds`);
  console.log(`  States: ${STATES.join(', ')}`);
  console.log(`  Household sizes: ${HOUSEHOLD_SIZES.join(', ')}`);
  
  // Test health check
  const healthResponse = http.get(`${BASE_URL}/api/rules/health`);
  check(healthResponse, {
    'Health check status is 200': (r) => r.status === 200,
  }) || console.error('Health check failed!');
  
  return { startTime: Date.now() };
}

/**
 * Main load test function
 */
export default function (data) {
  activeConnections.add(1);
  
  group('eligibility', function () {
    const input = generateEligibilityInput();
    
    const response = http.post(
      `${BASE_URL}/api/rules/evaluate`,
      JSON.stringify(input),
      {
        headers: {
          'Content-Type': 'application/json',
          'User-Agent': 'k6-load-test/1.0',
        },
        tags: { name: 'EvaluateEligibility' },
      }
    );
    
    requestDuration.add(response.timings.duration);
    requestCount.add(1);
    
    const isSuccess = check(response, {
      'Status is 200': (r) => r.status === 200,
      'Response time < 2000ms': (r) => r.timings.duration < 2000,
      'Response has OverallStatus': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body && body.overallStatus;
        } catch (e) {
          return false;
        }
      },
      'No server errors': (r) => r.status < 500,
    });
    
    errorRate.add(!isSuccess);
  });
  
  activeConnections.add(-1);
}

/**
 * Teardown function: runs after load test completes
 */
export function teardown(data) {
  const endTime = Date.now();
  const elapsedMs = endTime - data.startTime;
  const elapsedMinutes = (elapsedMs / 1000 / 60).toFixed(2);
  
  console.log(`\n=== Load Test Summary ===`);
  console.log(`Total duration: ${elapsedMinutes} minutes`);
  console.log(`\nMetrics:`);
  console.log(`  - Request duration tracked in 'request_duration' metric`);
  console.log(`  - Error rate tracked in 'errors' metric`);
  console.log(`  - Active connections tracked in 'active_connections' metric`);
  console.log(`\nCheck k6 output above for detailed results.`);
}
