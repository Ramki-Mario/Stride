# STRIDE — Project Overview

# Product Name

STRIDE

Expanded Form:
Smart Task Routing, Integration & Distributed Execution

---

# Product Vision

STRIDE is a multi-tenant operational orchestration platform designed to support organizations managing:
- workforce operations
- operational scheduling
- approvals
- task execution
- field operations
- reporting
- operational visibility
- workflow automation

The platform is intentionally designed to remain:
- industry-flexible
- SaaS-ready
- modular
- scalable
- enterprise-oriented

STRIDE is NOT intended to be tightly coupled to one specific business domain.

---

# Problem Statement

Many organizations rely on:
- spreadsheets
- disconnected internal tools
- manual approvals
- fragmented operational workflows
- email-based coordination
- non-scalable legacy systems

This creates:
- operational inefficiency
- lack of visibility
- poor auditability
- workflow delays
- scheduling conflicts
- reporting challenges
- scaling problems

STRIDE exists to centralize and orchestrate operational workflows through a scalable, modular platform.

---

# Supported Industry Examples

The architecture should support multiple industries without major redesign.

Examples include:
- outsourced staffing companies
- field service organizations
- construction companies
- maintenance businesses
- logistics coordination
- petroleum logistics
- facility management
- inspection agencies
- real estate operations
- delivery management
- workforce outsourcing operations

The platform should remain domain-flexible.

---

# What STRIDE IS

STRIDE is:
- an operational orchestration platform
- a workflow management platform
- a scheduling and execution system
- a multi-tenant SaaS-ready architecture
- a modular enterprise platform
- an observability-aware system
- an AI-assisted engineered platform

---

# What STRIDE IS NOT

STRIDE is NOT:
- a social media platform
- a CRM clone
- a simple CRUD dashboard
- a microservices playground
- a low-code platform
- a niche single-industry application
- a tutorial/demo-only project

---

# Core Product Objectives

## Technical Objectives

- Enterprise-grade architecture
- Multi-tenant SaaS readiness
- Evolutionary scalability
- Modular architecture
- Strong observability
- Tenant isolation
- AI-assisted engineering support
- Future extraction readiness

---

## Business Objectives

- Operational visibility
- Workflow standardization
- Scheduling coordination
- Approval orchestration
- Auditability
- Reporting centralization
- Cross-team coordination
- Platform extensibility

---

# Core Feature Areas

# Identity & Access Management

Features:
- multi-tenant authentication
- RBAC
- claims-based authorization
- session management
- tenant-aware access control

Potential roles:
- Admin
- Operations Manager
- Finance User
- Field Worker
- Supervisor

---

# Workflow Management

Features:
- workflow lifecycle tracking
- task orchestration
- approval pipelines
- state transitions
- audit logging
- workflow visibility

Example states:
- New
- Assigned
- InProgress
- PendingApproval
- Completed
- Invoiced

---

# Scheduling & Assignment

Features:
- workforce scheduling
- assignment management
- operational coordination
- task dispatching
- execution tracking

---

# Reporting & Dashboards

Features:
- operational dashboards
- KPI reporting
- productivity metrics
- tenant-level analytics
- workflow insights
- operational summaries

---

# Notifications

Features:
- event-driven notifications
- workflow alerts
- operational updates
- status change notifications

---

# Auditability

Features:
- audit trails
- activity history
- operational logs
- tenant-aware logging
- workflow history

---

# Multi-Tenant SaaS Direction

## Current Strategy

Shared Database + TenantId Isolation

Every tenant-aware table must include:
- TenantId

---

## Tenant Resolution Strategy

### Corporate Domains

Example:
user@company1.com

Resolution:
company1.com → TenantId

---

### Generic Email Providers

Examples:
- gmail.com
- outlook.com
- yahoo.com

Resolution:
UserTenantMapping lookup table

---

## Future Scalability Direction

Future support:
- dedicated tenant databases
- shard-aware routing
- tenant-specific extraction
- hybrid tenant storage strategy

The architecture must remain:
- shard-aware
- extraction-ready
- modular

---

# Core Engineering Philosophy

STRIDE prioritizes:
- maintainability
- operational clarity
- tenant safety
- observability
- modularity
- evolutionary scalability
- architecture consistency
- clean engineering practices

The system should evolve incrementally rather than through premature distributed complexity.

---

# Finalized Architecture Decisions

## Frontend
- Angular SPA
- Tailwind CSS
- PrimeNG
- Angular Signals + RxJS
- BFF pattern

---

## Backend
- ASP.NET Core
- Controller-based APIs
- Hybrid Modular Clean Architecture
- Internal domain events
- Queue abstraction

---

## Data Layer
- Azure SQL
- EF Core + Dapper
- Redis

---

## Infrastructure
- Azure-native deployment
- Docker-ready architecture
- OpenTelemetry
- Serilog
- Application Insights

---

# Security Philosophy

Security principles:
- frontend must not directly manage bearer tokens
- BFF pattern required
- HttpOnly cookies preferred
- tenant isolation mandatory
- RBAC mandatory
- audit logging required
- centralized session management required

---

# Observability Philosophy

STRIDE must remain:
- operationally visible
- debuggable
- monitorable
- tenant-aware

The platform should support:
- structured logs
- health checks
- correlation IDs
- module health visibility
- operational dashboards

Example:
- Workflow Module — Healthy
- Notification Module — Degraded
- Invoicing Module — Offline

---

# AI Engineering Philosophy

AI is used as:
- engineering accelerator
- implementation assistant
- architecture collaborator

AI must NOT:
- override architecture
- bypass modular boundaries
- create uncontrolled complexity
- violate tenant isolation

Persistent AI memory is maintained through:
- agent files
- architecture docs
- discussion tracking
- roadmap tracking
- current-status tracking

---

# Initial MVP Goal

The initial implementation acts as:
- enterprise architecture showcase
- portfolio centerpiece
- international contractor positioning project
- production-style engineering demonstration

---

# Long-Term Vision

Future platform direction may include:
- AI-assisted operational analytics
- predictive scheduling
- workflow intelligence
- advanced orchestration
- dedicated tenant infrastructure
- Kubernetes deployment
- event-driven scaling
- external enterprise integrations

These are intentionally OUT OF SCOPE initially.

---

# Important Engineering Constraints

DO NOT:
- prematurely implement microservices
- overengineer abstractions
- introduce unnecessary infrastructure complexity
- tightly couple modules
- violate tenant boundaries

ALWAYS:
- preserve modularity
- preserve observability
- preserve tenant isolation
- preserve architecture consistency
- build incrementally
- maintain clean boundaries

---

# Current Project Stage

Current stage:
Architecture & AI engineering workspace finalized.

Next priorities:
1. Initialize monorepo
2. Scaffold backend architecture
3. Scaffold Angular architecture
4. Setup BFF foundation
5. Setup tenant resolution
6. Setup observability foundation

# Long-Term Vision

Future direction:
- production SaaS
- tenant onboarding
- advanced workflow orchestration
- enterprise integrations
- AI-assisted operational analytics
- scalable distributed modules

---

# Core Feature Areas

## Identity & Access
- RBAC
- multi-tenant auth
- claims authorization
- session management

## Workflow Management
- task lifecycle
- approvals
- state transitions
- audit logs

## Scheduling
- workforce assignment
- operational scheduling
- execution tracking

## Reporting
- dashboards
- metrics
- tenant analytics

## Notifications
- workflow alerts
- event-driven notifications
- operational updates

## Auditability
- activity history
- operational logs
- audit records

---

# Finalized Technology Stack

## Frontend
- Angular SPA
- Tailwind CSS
- PrimeNG
- Angular Signals
- RxJS

## Backend
- ASP.NET Core
- Controller-based APIs
- Modular Clean Architecture
- Internal domain events

## Database
- Azure SQL
- EF Core + Dapper
- Redis

## Infrastructure
- Azure-native deployment
- Docker-ready architecture
- Queue abstraction
- OpenTelemetry
- Serilog
- Application Insights