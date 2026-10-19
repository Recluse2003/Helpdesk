# Helpdesk — working project brief
Initial proposal - Everything here is open to change

## Problem
Organisations need a central place to manage support requests, assign responsibility and track issues through to completion.

## Users
- Customers submitting support requests.
- Support agents handling and resolving tickets.
- Administrators managing users and support operations.

## Core Workflow
A customer creates a ticket specifying the issue -> an agent takes ownership of the ticket -> the agent and customer communicate 
-> the agent may escalate the issue if they are unable to resolve it themselves -> the issue eventually gets resolved.

## Proposed Technology Stack
- HTML, CSS, TypeScript, and React for the frontend
- ASP.NET Core Web API and C# for the backend
- EF Core, and PostgreSQL or SQL Server for the database
- Automated tests and Git-based version control to ensure all work is backed up and working. 
