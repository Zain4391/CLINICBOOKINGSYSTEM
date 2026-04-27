# Clinic Booking System

A RESTful API backend for managing clinic appointments, built with ASP.NET Core 7.0. The system supports three roles — **Patient**, **Doctor**, and **Admin** — with JWT-based authentication, Supabase (PostgreSQL) as the database, and email notifications via SMTP.

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Configuration](#configuration)
  - [Running Locally](#running-locally)
  - [Running with Docker](#running-with-docker)
- [API Endpoints](#api-endpoints)
  - [Auth](#auth)
  - [Patients](#patients)
  - [Doctors](#doctors)
  - [Availability Slots](#availability-slots)
  - [Appointments](#appointments)
  - [Specializations](#specializations)
  - [Admin](#admin)
- [Authentication](#authentication)

---

## Features

- User registration and login with role-based access (`patient`, `doctor`, `admin`)
- Password hashing with BCrypt
- JWT authentication with expiry
- Forgot/reset password flow via email
- Doctor profile registration with specialization
- Patient profile registration
- Doctor availability slot management
- Appointment booking with email confirmation
- Admin dashboard endpoints (stats, all doctors, patients, appointments)
- Swagger UI for interactive API documentation
- Docker support

---

## Tech Stack

| Layer          | Technology                          |
|----------------|-------------------------------------|
| Framework      | ASP.NET Core 7.0                    |
| Database       | Supabase (PostgreSQL)               |
| ORM / Client   | supabase-csharp (postgrest-csharp)  |
| Auth           | JWT Bearer tokens                   |
| Password Hash  | BCrypt.Net-Next                     |
| Email          | SMTP (Gmail)                        |
| API Docs       | Swagger / Swashbuckle               |
| Container      | Docker                              |

---

## Project Structure

```
ClinicBookingSystem/
├── Controllers/
│   ├── AuthController.cs          # Register, login, forgot/reset password
│   ├── PatientController.cs       # Patient profile management
│   ├── DoctorController.cs        # Doctor profile management
│   ├── AvailabilityController.cs  # Availability slot management
│   ├── AppointmentController.cs   # Appointment booking
│   ├── SpecializationController.cs# Specialization lookup
│   └── AdminController.cs         # Admin-only endpoints
├── DTOs/                          # Data Transfer Objects
├── Models/                        # Database models
├── Services/
│   ├── SupabaseService.cs         # Supabase client wrapper
│   └── EmailService.cs            # SMTP email sender
├── Program.cs                     # App entry point & service configuration
├── appsettings.json               # Configuration (no secrets committed)
├── dockerfile                     # Docker build & runtime stages
└── ClinicBookingSystem.csproj
```

---

## Getting Started

### Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- A [Supabase](https://supabase.com/) project with the required tables
- A Gmail account (or other SMTP provider) for email notifications
- Docker (optional, for containerized deployment)

### Configuration

Copy the values below into `appsettings.json` (or use environment variables / user secrets in development):

```json
{
  "Supabase": {
    "Url": "<your-supabase-project-url>",
    "Key": "<your-supabase-anon-key>"
  },
  "Jwt": {
    "Key": "<a-strong-secret-key>",
    "Issuer": "<your-issuer-string>",
    "Audience": "<your-audience-string>"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "<your-gmail-address>",
    "Password": "<your-gmail-app-password>"
  }
}
```

> **Never commit real secrets to source control.** Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables for local development.

### Running Locally

```bash
# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

The API starts on `http://localhost:80` by default (override with the `PORT` environment variable).  
Swagger UI is available at `http://localhost:80/swagger` when running in **Development** mode.

### Running with Docker

```bash
# Build the image
docker build -t clinic-booking-system .

# Run the container (pass config as environment variables)
docker run -p 8080:80 \
  -e Supabase__Url=<url> \
  -e Supabase__Key=<key> \
  -e Jwt__Key=<secret> \
  -e Jwt__Issuer=<issuer> \
  -e Smtp__Username=<email> \
  -e Smtp__Password=<password> \
  clinic-booking-system
```

---

## API Endpoints

All endpoints are prefixed with `/api`.

### Auth

| Method | Endpoint                  | Auth Required | Description                        |
|--------|---------------------------|---------------|------------------------------------|
| POST   | `/api/auth/register`      | No            | Register a new user                |
| POST   | `/api/auth/login`         | No            | Login and receive a JWT token      |
| POST   | `/api/auth/forgot-password` | No          | Request a password reset email     |
| POST   | `/api/auth/reset-password`  | No          | Reset password using a token       |
| GET    | `/api/auth/me`            | Yes           | Get the currently authenticated user |

### Patients

| Method | Endpoint               | Auth Required | Role    | Description               |
|--------|------------------------|---------------|---------|---------------------------|
| POST   | `/api/patients/register` | Yes         | patient | Create a patient profile  |
| GET    | `/api/patients/me`     | Yes           | Any     | Get current patient info  |

### Doctors

| Method | Endpoint               | Auth Required | Role   | Description              |
|--------|------------------------|---------------|--------|--------------------------|
| POST   | `/api/doctors/register` | Yes          | doctor | Create a doctor profile  |
| GET    | `/api/doctors/me`      | Yes           | Any    | Get current doctor info  |

### Availability Slots

| Method | Endpoint                   | Auth Required | Role   | Description                       |
|--------|----------------------------|---------------|--------|-----------------------------------|
| GET    | `/api/availability/all`    | No            | —      | List all available (unbooked) slots |
| POST   | `/api/availability`        | Yes           | doctor | Create a new availability slot    |
| GET    | `/api/availability/my`     | Yes           | doctor | Get the current doctor's slots    |

### Appointments

| Method | Endpoint                  | Auth Required | Role    | Description                       |
|--------|---------------------------|---------------|---------|-----------------------------------|
| POST   | `/api/appointments/book`  | Yes           | patient | Book an appointment in a slot     |
| GET    | `/api/appointments/my`    | Yes           | patient | List the current patient's appointments |

### Specializations

| Method | Endpoint                  | Auth Required | Description                  |
|--------|---------------------------|---------------|------------------------------|
| GET    | `/api/specializations`    | No            | List all specializations     |

### Admin

| Method | Endpoint                    | Auth Required | Role  | Description                |
|--------|-----------------------------|---------------|-------|----------------------------|
| GET    | `/api/admin/appointments`   | Yes           | admin | List all appointments      |
| GET    | `/api/admin/doctors`        | Yes           | admin | List all doctors           |
| GET    | `/api/admin/patients`       | Yes           | admin | List all patients          |
| GET    | `/api/admin/stats`          | Yes           | admin | Get system-wide statistics |

---

## Authentication

The API uses **JWT Bearer** tokens.

1. Register via `POST /api/auth/register` with `role` set to `patient`, `doctor`, or `admin`.
2. Login via `POST /api/auth/login` to receive a token.
3. Include the token in the `Authorization` header for protected endpoints:

```
Authorization: Bearer <your-token>
```

Tokens expire after **1 hour**.
