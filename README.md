# About this project

This project is me implementing my knowledge in Web API development with ASP.NET Core, EF Core, and SignalR in practical. My main objective building this project is to learn as I have not implemented any specific WEB API Design patterns. There is also a code inconsistency in few areas so if you are someone referencing my code please follow the general pattern.
For the realtime Client notification, Simple TypeScript application is used.


# Multi-Tenant Task Manager

A multi-tenant task management application with real time SignalR notification built with ASP.NET Core, Entity Framework, and TypeScript. 

📁 MultiTenantTaskManager - Server

📁 NotificationClient - Client for real time notification

## Features

- Multi-tenant architecture
- Role and policy based authorization
- Task and Project management
- Activity logging
- EF Core with soft delete


 ## Tech Stack
 
- ASP.NET Core 8
- Entity Framework Core
- JWT Authentication
- SQL Server
- FluentValidation
- SignalR
- TypeScript 


## Design Overview 

### User roles :
- SuperAdmin
- Admin
- Manager
- Member
- SpecialMember
  
### Permissions per role
#### 1. SuperAdmin
- lives outside tenant scope
- manages tenants(CREATE, DELETE, UPDATE Tenants)

#### 2. Admin
- lives inside Tenant
- manages User(UPDATE, DELETE Users from Tenant)
- manage Projects(CREATE, DELETE, UPDATE Projects)

#### 3. Manager
- lives inside Tenant
- manages Projects(CREATE, DELETE, UPDATE, Assign Manager, SpecialMember, and Member Users, Update Project status)
- manages Tasks(CREATE, DELETE, UPDATE, Assign User, Update Task status)

#### 4. SpecialMember
- lives inside Tenant
- manages Tasks(CREATE, DELETE, UPDATE, Assign Member User, Update Task status)

#### 5. Member
- lives inside Tenant
- view Tasks
