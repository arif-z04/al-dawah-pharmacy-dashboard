# Contributing to Al-Dawah Pharma

Thank you for your interest in contributing to **Al-Dawah Pharma**! We welcome bug reports, feature enhancements, documentation improvements, and architectural suggestions.

---

## Code of Conduct

We are committed to providing a welcoming, inclusive, and harassment-free environment for everyone. Please treat all contributors with respect and professionalism.

---

## How to Contribute

### 1. Reporting Bugs
- Search existing GitHub Issues before opening a new one.
- Use the **Bug Report** template.
- Include complete reproduction steps, database logs, and environment details.

### 2. Suggesting Features
- Open an issue using the **Feature Request** template.
- Describe the business value for pharmacy operators or healthcare clinics.

### 3. Submitting Code Changes (Pull Requests)
1. Fork the repository on GitHub.
2. Clone your fork locally:
   ```bash
   git clone https://github.com/your-username/AL-Dawah_Pharma.git
   cd AL-Dawah_Pharma
   ```
3. Create a descriptive feature branch:
   ```bash
   git checkout -b feature/new-inventory-alert
   ```
4. Follow the existing architecture:
   - **Database First**: The Oracle Database is the source of truth. Changes to business rules must be reflected in `sql/` scripts first.
   - **Clean Architecture**: Never couple domain models directly to database drivers or Web API controllers.
   - **No Hardcoded Business Data**: Do not use fake data in frontend code.
5. Verify your changes:
   ```bash
   dotnet test
   ```
6. Commit with clear, conventional messages:
   ```bash
   git commit -m "feat(inventory): add batch recall notification trigger"
   ```
7. Push to your fork and submit a Pull Request to `main`.
