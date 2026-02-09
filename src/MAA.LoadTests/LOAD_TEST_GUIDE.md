# Phase 10: Performance & Load Testing - Load Test Guide

**Last Updated**: 2026-02-10  
**Status**: Ready for Execution  
**Feature**: E2 Rules Engine Performance Validation (T075)

---

## Overview

This guide provides instructions for running load tests against the Rules Engine API using k6, a modern load testing framework designed for performance testing and SLO validation.

### Objectives

- ✅ Validate eligibility evaluation meets latency SLOs (≤2 sec p95)
- ✅ Achieve 0% error rate under 1,000 concurrent users
- ✅ Identify performance bottlenecks and optimization opportunities
- ✅ Generate actionable performance reports
- ✅ Measure cache efficiency and database query performance

---

## Prerequisites

### 1. Install k6

k6 must be installed on your system:

**Windows (using Chocolatey)**:

```powershell
choco install k6
```

**Windows (using scoop)**:

```powershell
scoop install k6
```

**macOS (using Homebrew)**:

```bash
brew install k6
```

**Linux (Debian/Ubuntu)**:

```bash
sudo apt-get install k6
```

**Or download from**: https://k6.io/docs/getting-started/installation/

**Verify installation**:

```bash
k6 version
# Should output: k6 v0.x.x
```

### 2. Rules Engine API Running

The API must be running before starting load tests:

```bash
cd "D:\Programming\Langate\MedicaidApplicationAssistant\src"
dotnet run --project MAA.API/MAA.API.csproj
```

**Expected output**:

```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:7000
      Now listening on: http://localhost:5000
```

### 3. Database Seeded

Ensure the database has:

- ✅ All 5 pilot states (IL, CA, NY, TX, FL) populated
- ✅ At least 100 test scenarios for diverse inputs
- ✅ FPL tables loaded for 2026
- ✅ Rules for MAGI, Aged, Disabled, SSI, Pregnancy pathways

**Seed status check**:

```bash
# Connect to database and verify
psql -h localhost -U maa_user -d maa_rules_db -c "SELECT COUNT(*) FROM medicaid_programs;"
# Should show: count > 30 (5 states × 6+ programs)
```

---

## Running Load Tests

### Basic Load Test

Run the default load test configuration (1,000 concurrent users for 5 minutes):

```bash
cd "D:\Programming\Langate\MedicaidApplicationAssistant\src\MAA.LoadTests"
k6 run rules-load-test.js
```

### Custom Base URL

If the API is running on a different host/port:

```bash
k6 run rules-load-test.js -e BASE_URL=http://api.example.com:8080
```

### Output to File

Generate a JSON report for further analysis:

```bash
k6 run rules-load-test.js -o json=results.json
```

### Output to HTML Report

Use k6's HTML extension for a visual report:

```bash
# First, install the HTML extension (via terminal inside k6 environment)
k6 run rules-load-test.js --out html=report.html
```

### Extended Test (Custom Stages)

For testing different scenarios, modify the `options.stages` array in `rules-load-test.js`:

**Light load** (500 users, 3 min):

```javascript
stages: [
  { duration: '30s', target: 500 },
  { duration: '3m', target: 500 },
  { duration: '30s', target: 0 },
],
```

**Heavy load** (2,000 users, 10 min):

```javascript
stages: [
  { duration: '1m', target: 2000 },
  { duration: '10m', target: 2000 },
  { duration: '1m', target: 0 },
],
```

---

## Interpreting Results

### Key Metrics

**Latency (Response Time)**

- **p50**: Median latency (50th percentile)
- **p95**: 95th percentile latency (SLO target: ≤ 2 seconds)
- **p99**: 99th percentile latency (target: < 3 seconds)

**Example output**:

```
     data_received...................: 3.2 MB   10 kB/s
     data_sent........................: 1.6 MB   5 kB/s
     http_req_blocked..................: avg=1.23ms   min=0s    med=0s    max=150ms p(90)=5ms   p(95)=8ms
     http_req_connecting..............: avg=0.98ms   min=0s    med=0s    max=120ms p(90)=3ms   p(95)=5ms
     http_req_duration................: avg=456ms    min=100ms med=410ms max=2.1s  p(90)=890ms p(95)=1.2s  p(99)=1.8s ✓
     http_req_receiving...............: avg=15ms     min=1ms   med=12ms  max=300ms p(90)=25ms  p(95)=35ms
     http_req_sending.................: avg=8ms      min=1ms   med=5ms   max=100ms p(90)=15ms  p(95)=20ms
     http_req_tls_handshaking.........: avg=0s       min=0s    med=0s    max=0s    p(90)=0s    p(95)=0s
     http_req_waiting.................: avg=433ms    min=98ms  med=392ms max=2s    p(90)=850ms p(95)=1.1s
     http_reqs.........................: 7890    26.3/s
     request_duration.................: avg=458ms    min=102ms med=412ms max=2.1s  p(90)=895ms p(95)=1.2s  p(99)=1.8s ✓
     errors...........................: 0       0/s ✓
     active_connections...............: 0       0
```

### SLO Validation

The test automatically validates against defined SLOs:

```
✓ Status is 200
✓ Response time < 2000ms
✓ Response has OverallStatus
✓ No server errors
✓ request_duration{method:post,name:EvaluateEligibility}: avg=456ms, p(95)=1200ms, p(99)=1800ms <= 2000ms ✓
✓ errors: rate == 0% ✓
```

**Passing Test**:

```
 execution: local
   script: rules-load-test.js
   output: -

     ✓ is handling 1000 vus
     ✓ thresholds............................: ok

     checks.........................: 100% ✓
     data_received...................: 3.2 MB
     data_sent........................: 1.6 MB
     iteration_duration...............: avg=458ms
     iterations........................: 7890
     vus...............................: 1000
     vus_max...........................: 1000

PASSED ✓  - All SLOs met, system ready for production.
```

**Failing Test**:

```
 ✗ request_duration{method:post,name:EvaluateEligibility}: avg=2500ms, p(95)=3200ms <= 2000ms ✗
 ✗ errors: rate 5% > 0% ✗

 CRITICAL: SLO violations detected. System requires optimization before deployment.
```

---

## Common Scenarios

### Scenario 1: Baseline Performance (Current State)

Run the standard test to establish baseline:

```bash
k6 run rules-load-test.js -o json=baseline.json
```

**Capture baseline results in performance report for comparison during optimization.**

### Scenario 2: Cache Efficiency Test

Test with cache warming (run twice in succession):

```bash
# First run: cold cache
k6 run rules-load-test.js -o json=cold-cache.json

# Wait 10 seconds
sleep 10

# Second run: warm cache
k6 run rules-load-test.js -o json=warm-cache.json
```

**Compare results: warm cache should show 10-20% latency improvement.**

### Scenario 3: Stress Test (Find Breaking Point)

Progressively increase load to identify breaking point:

```javascript
// Modify options.stages for stress test:
stages: [
  { duration: '2m', target: 2000 },   // 2K users
  { duration: '2m', target: 4000 },   // 4K users
  { duration: '2m', target: 6000 },   // 6K users
  { duration: '1m', target: 0 },      // Cool down
],
```

**Results will identify at what concurrency SLOs break.**

### Scenario 4: Soak Test (Long-Duration Stability)

Test for 30+ minutes to identify memory leaks or connection pool exhaustion:

```javascript
stages: [
  { duration: '1m', target: 1000 },      // Warm-up
  { duration: '30m', target: 1000 },     // Sustained load
  { duration: '1m', target: 0 },         // Cool down
],
```

---

## Performance Report Template

After running tests, **[create a report](./PHASE-10-PERFORMANCE-REPORT.md)** with:

1. **Test Execution Summary**
   - Date/time
   - Duration
   - Concurrent users
   - Total requests

2. **SLO Validation**
   - ✅ p95 latency ≤ 2s: [PASS/FAIL]
   - ✅ p99 latency < 3s: [PASS/FAIL]
   - ✅ p50 latency < 1s: [PASS/FAIL]
   - ✅ Error rate: [X%]

3. **Detailed Metrics**
   - Response time distribution (p50, p95, p99, max)
   - Throughput (requests/sec)
   - Data sent/received

4. **Bottleneck Analysis**
   - Which endpoints are slowest
   - Which states/household sizes are slower
   - HTTP wait times vs network times

5. **Recommendations**
   - Caching improvements
   - Query optimization
   - Connection pooling adjustments
   - Scaling recommendations

---

## Troubleshooting

### Issue: Connection refused

```
Error: connection refused: ECONNREFUSED 127.0.0.1:5000
```

**Solution**: Ensure API is running:

```bash
# In separate terminal
cd src && dotnet run --project MAA.API/MAA.API.csproj
```

### Issue: Database errors during test

```
Error: 400: Database connection timeout
```

**Solution**: Check database is seeded and reachable:

```bash
# Verify database
psql -h localhost -U maa_user -d maa_rules_db -c "SELECT COUNT(*) FROM medicaid_programs;"
```

### Issue: p95 latency exceeds target (> 2 seconds)

```
✗ request_duration: p(95)=2.5s > 2000ms
```

**Solution**:

1. Check if database queries are slow: Enable query logging in appsettings.Development.json
2. Check cache hit rates: Look for cache misses in logs
3. Reduce test load temporarily: Find bottleneck with k6 profiling
4. Profile application: Use dotnet dump/trace tools

### Issue: High error rates

```
errors: 5% of requests failed
```

**Solution**:

1. Check API logs for error details
2. Verify database schema migrations ran
3. Check resource limits (memory, CPU)
4. Run with smaller load to isolate issue

---

## Performance Optimization Strategies

If SLOs are not met:

### 1. Caching Improvements

- Increase cache TTL for rules and FPL tables
- Add distributed cache (Redis) for high-traffic scenarios
- Implement cache warming on application startup

### 2. Database Query Optimization

- Add indexes on frequently queried columns (state_code, program_id, effective_date)
- Use connection pooling to reduce connection overhead
- Consider query result caching

### 3. Application Performance

- Profile hot paths using dotnet profiler
- Consider async/parallelization opportunities
- Review LINQ queries for N+1 patterns

### 4. Infrastructure Scaling

- Increase application server resources (CPU, memory)
- Implement load balancing across multiple instances
- Use container orchestration (Kubernetes) for auto-scaling

---

## Next Steps

1. ✅ Run baseline test and capture results
2. ✅ Create performance report
3. ✅ Identify optimization opportunities
4. ✅ Implement improvements
5. ✅ Re-run tests and compare

**Expected Outcome**: System validates against SLOs and is ready for production deployment.

---

## References

- [k6 Official Documentation](https://k6.io/docs)
- [k6 API Reference](https://k6.io/docs/javascript-api/)
- [HTTP Protocol Performance Testing](https://k6.io/docs/examples/http-authentication/)
- [Thresholds and SLO Validation](https://k6.io/docs/using-k6/thresholds/)

---

## Support

For issues or questions:

1. Check troubleshooting section above
2. Review k6 logs for detailed error messages
3. Consult project documentation: `specs/002-rules-engine/`
