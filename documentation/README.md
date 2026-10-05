# Al-Dawah Pharma: Complete Architecture & Developer Documentation Suite

Welcome to the definitive, ground-up documentation for the **Al-Dawah Pharmacy Inventory & Sales Management System**.

This documentation suite is engineered for everyone: from an **absolute beginner** who has never built a web application and wants to understand how computers, browsers, servers, and databases communicate, to an **experienced software architect** looking for deep PL/SQL trigger implementations, concurrency handling, RBAC security, and .NET Web API design patterns.

---

## 📚 Curriculum & Table of Contents

| Chapter | Document Title | Description | Target Audience |
| :--- | :--- | :--- | :--- |
| **01** | [Absolute Beginner's Guide to Web Architecture](file:///home/noir/Work/AL-Dawah_Pharma/documentation/01_absolute_beginner_guide_to_web_and_software.md) | How the internet, browsers, servers, HTTP, JSON, and databases work from first principles. | Absolute Beginners, Junior Developers |
| **02** | [System Architecture & High-Level Design](file:///home/noir/Work/AL-Dawah_Pharma/documentation/02_system_architecture_and_design.md) | Multi-tier architecture, clean architecture, data flow diagrams, ACID transactions, and business rules. | All Developers, Architects |
| **03** | [Database Deep Dive & PL/SQL Reference](file:///home/noir/Work/AL-Dawah_Pharma/documentation/03_database_deep_dive_and_sql_reference.md) | Complete Oracle DB schema, 12 tables, 7 views, 4 stored procedures, 2 functions, 6 triggers, and security. | DBA, Backend Developers |
| **04** | [Backend C# .NET Web API Walkthrough](file:///home/noir/Work/AL-Dawah_Pharma/documentation/04_backend_csharp_dotnet_code_walkthrough.md) | Line-by-line explanation of ASP.NET Core, Dependency Injection, Repositories, DTOs, JWT Auth, and Controllers. | Backend Developers |
| **05** | [Frontend Architecture & UI Walkthrough](file:///home/noir/Work/AL-Dawah_Pharma/documentation/05_frontend_javascript_and_ui_walkthrough.md) | Vanilla JavaScript ES6+ SPA architecture, state management, POS billing, responsive UI, and print rendering. | Frontend Developers, Full-Stack |
| **06** | [Security, Authentication & Production Readiness](file:///home/noir/Work/AL-Dawah_Pharma/documentation/06_security_best_practices_and_production_readiness.md) | Cryptography, BCrypt hashing, JWT life-cycle, SQL injection prevention, RBAC, and data audit logs. | Security Engineers, DevOps, Leads |
| **07** | [Comprehensive Windows Setup Guide](file:///home/noir/Work/AL-Dawah_Pharma/documentation/07_setup_guide_windows.md) | Step-by-step setup on Windows 10/11 using Docker Desktop, WSL2, native .NET, PowerShell, and Oracle DB. | Windows Users, System Admins |
| **08** | [Troubleshooting, Error Reference & FAQ](file:///home/noir/Work/AL-Dawah_Pharma/documentation/08_troubleshooting_and_faq.md) | Exhaustive troubleshooting catalog for Oracle ORA errors, .NET exceptions, CORS, network ports, and FAQs. | Everyone |
| **09** | [Complete Annotated Source Code Reference](file:///home/noir/Work/AL-Dawah_Pharma/documentation/09_complete_source_code_annotated_reference.md) | Complete code listings with line-by-line educational commentary. | Code Reviewers, Students |

---

## 🌟 How to Read This Documentation

1. **If you are brand new to programming or web development**:
   - Start immediately with **Chapter 01: Absolute Beginner's Guide**.
   - Read every analogy carefully (the restaurant analogy for client/server, the notebook analogy for databases).
   - Follow with **Chapter 07: Windows Setup Guide** to get the software running on your own computer.
   - Then explore **Chapter 05 (Frontend)** and **Chapter 04 (Backend)** to see how code turns into real-world behavior.

2. **If you are a Computer Science student or bootcamp graduate**:
   - Skim **Chapter 01** to solidify your fundamentals.
   - Study **Chapter 02 (Architecture)** and **Chapter 03 (Database & PL/SQL)** to understand why real-world enterprise systems rely heavily on database constraints, triggers, and stored procedures rather than doing everything in application memory.
   - Study **Chapter 06 (Security)** to learn production-grade authentication and authorization patterns.

3. **If you are setting up or deploying the application**:
   - Jump directly to **Chapter 07: Comprehensive Windows Setup Guide** (or the Linux Docker quickstart in the project root [README.md](file:///home/noir/Work/AL-Dawah_Pharma/README.md)).
   - Keep **Chapter 08: Troubleshooting, Error Reference & FAQ** open in case of any port collisions, database listener errors, or permission questions.

---

## 🚀 Key Project Summary

- **Product Name**: Al-Dawah Pharma (Pharmacy Inventory & Sales Management System)
- **Target Domain**: Retail and wholesale pharmacies, hospitals, drugstores, and medical supply clinics.
- **Database Engine**: Oracle Database 23c / 26ai Free (Enterprise Relational Database).
- **Backend Framework**: ASP.NET Core 10.0 Web API (C#).
- **Frontend Technologies**: HTML5, Vanilla JavaScript (ES6+ Modules), Tailwind CSS, Lucide Icons.
- **Architectural Style**: Clean Multi-Tier Layered Architecture with decoupled Database, Backend REST API, and Single Page Application (SPA) Client.
