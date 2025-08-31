# Security Policy

## Supported Versions

We actively support the following versions of Noundry.Hydro with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | ✅ Yes             |
| < 1.0   | ❌ No              |

## Reporting a Vulnerability

We take security seriously. If you discover a security vulnerability in Noundry.Hydro, please report it responsibly.

### How to Report

**Please DO NOT report security vulnerabilities through public GitHub issues.**

Instead, please send an email to: **security@noundry.dev**

Include the following information:
- Type of issue (e.g., buffer overflow, SQL injection, cross-site scripting, etc.)
- Full paths of source file(s) related to the manifestation of the issue
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit the issue

### What to Expect

- **Acknowledgment**: We'll acknowledge receipt of your vulnerability report within 48 hours
- **Initial Assessment**: We'll provide an initial assessment within 5 business days
- **Updates**: We'll keep you informed of our progress throughout the investigation
- **Resolution**: We'll work to resolve confirmed vulnerabilities as quickly as possible
- **Credit**: We'll credit you in our security advisory (unless you prefer to remain anonymous)

### Security Best Practices

When using Noundry.Hydro in production:

1. **Keep Dependencies Updated**: Regularly update to the latest version
2. **Configure Authentication**: Use strong authentication and authorization policies
3. **Validate Input**: Always validate user input on both client and server
4. **Use HTTPS**: Ensure all communication is encrypted
5. **Review Logs**: Monitor application logs for suspicious activity
6. **Follow OWASP Guidelines**: Implement OWASP security recommendations

### Security Features

Noundry.Hydro includes several built-in security features:

- **CSRF Protection**: Anti-forgery token validation
- **Input Validation**: Client and server-side validation
- **Authorization**: Role-based and policy-based authorization
- **Secure Defaults**: Secure configuration out of the box
- **Audit Logging**: Comprehensive activity logging

Thank you for helping keep Noundry.Hydro and our community safe!