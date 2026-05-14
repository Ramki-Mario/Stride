# STRIDE Authentication Architecture

# Purpose

This document defines the finalized authentication architecture for STRIDE.

Goals:
- enterprise-grade security
- multi-tenant compatibility
- scalable auth model
- future IdP extensibility
- secure SPA authentication
- centralized token handling
- AI implementation guidance

---

# Finalized Authentication Architecture

## Frontend Pattern
Angular SPA + BFF

## Auth Strategy
Session-oriented authentication using:
- HttpOnly cookies
- centralized token management
- BFF token handling

## Identity Strategy
Hybrid-ready authentication architecture.

Current:
- Internal identity management

Future:
- Azure AD
- Auth0
- Okta
- OIDC/SAML providers

---

# Why BFF Architecture Was Chosen

The UI should NOT directly manage bearer tokens.

Reasons:
- reduce XSS exposure
- avoid browser token leakage
- centralized token lifecycle management
- enterprise-grade security posture
- better refresh token handling
- easier session invalidation
- better observability

---

# High-Level Auth Flow

Browser (Angular SPA)
    ↓
BFF Layer
    ↓
Identity Module
    ↓
Session Store / Redis
    ↓
Application Modules

---

# Authentication Flow

## Step 1 — Login Request

User submits:
- email
- password

to:
- BFF endpoint

The frontend never directly communicates with internal auth services.

---

## Step 2 — Tenant Resolution

System resolves tenant using:

### Corporate Domains
Example:
user@company1.com

Resolution:
company1.com → TenantId

### Generic Domains
Example:
gmail.com

Resolution:
UserTenantMapping lookup

---

## Step 3 — Identity Validation

Identity module validates:
- credentials
- tenant membership
- account status
- RBAC roles

---

## Step 4 — Session Creation

System creates:
- session identifier
- refresh token
- claims payload

Stored in:
- Redis
- secure session storage

---

## Step 5 — Secure Cookie Issuance

BFF issues:
- HttpOnly cookies
- Secure cookies
- SameSite configuration

Frontend does NOT directly receive access tokens.

---

# Session Architecture

## Session Store
Redis

Redis responsibilities:
- session storage
- token revocation
- distributed sessions
- session invalidation
- auth cache

---

# Authorization Model

## RBAC
Role-Based Access Control

Roles:
- Admin
- OperationsManager
- FinanceUser
- FieldWorker

---

# Claims-Based Authorization

Claims examples:
- TenantId
- Role
- Permissions
- ModuleAccess
- RegionScope

---

# Multi-Tenant Auth Rules

- every request must contain tenant context
- users cannot access cross-tenant data
- logs must include TenantId
- sessions must remain tenant-bound

---

# Backend Auth Components

Modules:
- Identity Module
- Tenant Resolver
- Session Manager
- Claims Manager
- RBAC Manager

Infrastructure:
- Redis
- Secure Cookies
- Middleware Pipeline

---

# Frontend Auth Responsibilities

Frontend responsibilities:
- login UI
- auth guards
- session-aware routing
- permission-aware rendering
- tenant-aware UI initialization

Frontend must NOT:
- store access tokens in localStorage
- manually manage JWT lifecycle
- directly handle refresh tokens

---

# Security Rules

## NEVER
- store tokens in localStorage
- expose refresh tokens to frontend
- bypass tenant resolution
- allow cross-tenant sessions

---

# ALWAYS
- use HttpOnly cookies
- validate tenant membership
- validate RBAC permissions
- log auth events
- support session invalidation

---

# Observability Requirements

All auth events must log:
- TenantId
- UserId
- CorrelationId
- RequestId
- IP Address
- SessionId

Events:
- login
- logout
- token refresh
- failed auth
- RBAC denial
- tenant mismatch

---

# Future Identity Provider Support

Future providers:
- Azure AD
- Auth0
- Okta
- OIDC providers
- SAML providers

See:
auth-future-update.md

---

# Future IdP Integration Rules

When implementing external IdP:
- preserve BFF architecture
- preserve session strategy
- preserve tenant-awareness
- preserve RBAC
- preserve audit logging

---

# AI Implementation Constraints

AI must:
- preserve HttpOnly cookie strategy
- preserve tenant resolution
- preserve Redis session architecture
- preserve BFF boundaries

AI must NOT:
- switch to localStorage auth
- bypass tenant middleware
- expose JWTs to frontend
- tightly couple auth providers

---

# Current Status

Completed:
- auth architecture finalized
- BFF strategy finalized
- tenant-aware auth finalized
- Redis session strategy finalized

Pending:
- implementation
- middleware pipeline
- cookie security config
- session invalidation
- external IdP abstraction