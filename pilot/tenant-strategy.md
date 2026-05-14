# STRIDE Multi-Tenant Strategy

# Purpose

This document defines the finalized multi-tenant architecture strategy for STRIDE.

Goals:
- support multiple organizations/clients
- preserve tenant isolation
- enable SaaS scalability
- support future shard extraction
- support flexible onboarding
- preserve architecture simplicity initially
- remain extraction-ready for future scaling

---

# Finalized Tenant Strategy

## Current Model
Shared Database + TenantId Isolation

Every tenant-aware table MUST contain:
- TenantId

This is the finalized initial SaaS architecture.

---

# Why Shared DB Was Chosen Initially

Reasons:
- simpler operations
- easier development
- easier deployment
- lower infrastructure cost
- easier AI-assisted consistency
- faster MVP execution
- simpler reporting
- easier tenant onboarding

---

# Future Scalability Direction

Architecture must remain compatible with:
- dedicated tenant databases
- tenant-specific shards
- high-volume tenant extraction
- hybrid tenant storage strategy

Current implementation should remain:
- shard-aware
- extraction-ready
- abstraction-friendly

---

# Tenant Identification Strategy

## Corporate Domain Resolution

Example:
user@company1.com

Resolution:
company1.com → TenantId

This enables:
- automatic tenant routing
- simplified enterprise onboarding
- organization-level identity grouping

---

# Generic Email Resolution

Example:
gmail.com
outlook.com
yahoo.com

These cannot resolve tenant directly.

Use:
UserTenantMapping table.

---

# Tenant Resolution Flow

Login Request
    ↓
Extract Email Domain
    ↓
Corporate Domain?
    ├── YES → Resolve Tenant directly
    └── NO  → Lookup UserTenantMapping
    ↓
Resolve Tenant Context
    ↓
Initialize Tenant Session

---

# Tenant Context Requirements

Every request must contain:
- TenantId
- UserId
- CorrelationId

Tenant context must exist:
- throughout request lifecycle
- inside logs
- inside domain events
- inside background jobs

---

# Tenant Isolation Rules

## STRICT RULES

- cross-tenant access prohibited
- every repository query must filter TenantId
- every cache key must include TenantId
- every audit log must include TenantId
- every background job must preserve TenantId

---

# Backend Tenant Components

## Required Components

### Tenant Resolver
Responsible for:
- domain mapping
- tenant lookup
- session tenant resolution

---

### Tenant Middleware
Responsible for:
- validating tenant context
- attaching tenant context
- preventing invalid tenant access

---

### Tenant Context Provider
Responsible for:
- exposing TenantId to services/modules
- request-scoped tenant access

---

### Tenant-Aware Repository Layer
Repositories must:
- automatically apply TenantId filtering
- prevent accidental cross-tenant queries

---

# Database Design Rules

## Mandatory Columns

Every tenant-aware table must include:
- Id
- TenantId
- CreatedAt
- UpdatedAt
- CreatedBy
- IsDeleted

---

# Indexing Rules

Tenant-aware tables should use:
- composite indexes
- TenantId-first indexing strategy

Examples:
(TenantId, Status)
(TenantId, CreatedAt)

---

# Shard-Aware Design Principles

Although STRIDE initially uses shared DB architecture,
the system must remain compatible with future shard extraction.

Architecture should avoid:
- tightly coupled DB assumptions
- hardcoded connection strategies
- tenant-unaware infrastructure

---

# Future Dedicated Tenant Extraction

Future large tenants may move to:
- dedicated databases
- isolated infrastructure
- tenant-specific replicas

Potential extraction triggers:
- high storage consumption
- high throughput
- compliance requirements
- enterprise isolation requirements

---

# Redis Tenant Strategy

Redis keys must remain tenant-aware.

Examples:
tenant:{tenantId}:session:{sessionId}
tenant:{tenantId}:workflow:{workflowId}

This prevents:
- cross-tenant cache contamination
- invalid session reuse

---

# Logging Requirements

Every log entry must include:
- TenantId
- CorrelationId
- RequestId
- UserId

This is mandatory for:
- debugging
- auditability
- observability
- SaaS operations

---

# Background Job Requirements

Background jobs must preserve:
- TenantId
- User context
- CorrelationId

Jobs must NEVER:
- execute without tenant context
- access cross-tenant data

---

# Observability Requirements

Monitoring must support:
- tenant-level metrics
- tenant-level failures
- tenant-level performance tracking
- tenant-specific operational visibility

---

# AI Implementation Constraints

AI must:
- preserve tenant isolation
- preserve tenant-aware repositories
- preserve tenant-aware caching
- preserve tenant-aware logging

AI must NOT:
- create global shared caches
- bypass TenantId filtering
- create cross-tenant queries
- bypass tenant middleware

---

# Future Expansion Possibilities

Future SaaS enhancements:
- tenant provisioning portal
- custom tenant branding
- tenant-specific feature flags
- tenant-specific modules
- billing integration
- usage metering

These are intentionally OUT OF SCOPE initially.

---

# Current Status

Completed:
- tenant strategy finalized
- shared DB approach finalized
- shard-aware direction finalized
- tenant resolution strategy finalized

Pending:
- implementation
- tenant middleware
- tenant-aware repositories
- tenant context provider
- shard abstraction layer